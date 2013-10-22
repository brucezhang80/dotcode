using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using ManagedJob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sandbox.NET;
using Shared;

namespace SandboxHost
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SandboxHostService : ISandboxHost
    {
        private readonly SandboxConfig _config;
        private Job _job;

        public SandboxHostService(){}

        public SandboxHostService(SandboxConfig config)
        {
            _config = config;
        }

        private Job SetJobObject()
        {
            GC.Collect();
            var currentMemoryUsed = Process.GetCurrentProcess().WorkingSet64;
            ulong newMemoryLimit = (ulong?) _config.MemoryLimit ?? ulong.MaxValue;

            if (newMemoryLimit != ulong.MaxValue)
            {
                newMemoryLimit += (ulong) currentMemoryUsed;
            }

            var jobObject = new Job(true, newMemoryLimit);

            jobObject.AssignCurrentProcess();

            return jobObject;
        }

        public SandboxOutput Execute(SandboxConfig sandboxConfig)
        {
            Environment.CurrentDirectory = Path.Combine(sandboxConfig.ApplicationBinDirectory);

            var sandboxOutput = new SandboxOutput();

            try
            {
                var trustedLibraries = new[] {"BandwidthThrottle.dll"};
                var appDomain = SandboxHelper.CreateAppDomain(sandboxConfig, trustedLibraries);
                _job = SetJobObject();

                var type = typeof (Host);
                var className = sandboxConfig.SandboxedAssembly.EntryPointClass;
                var methodName = sandboxConfig.SandboxedAssembly.EntryPointMethod;
                var assemblyBytes = sandboxConfig.SandboxedAssembly.AssemblyBytes;

                var handle = Activator.CreateInstanceFrom(appDomain, type.Assembly.ManifestModule.FullyQualifiedName,
                                                          type.FullName);

                var loader = (Host)handle.Unwrap();

                var resMon = new ResourceMonitor(_config);
                resMon.StartThrottle();

                var beforeLoadMemoryUsage = Process.GetCurrentProcess().WorkingSet64;
                sandboxOutput = loader.Run(sandboxOutput, className, methodName, assemblyBytes, sandboxConfig.SandboxedAssembly.JsonArgs, sandboxConfig.AdditionalReferences.Select(c => c.AssemblyBytes).ToList());
                var currentMemoryUsage = Process.GetCurrentProcess().WorkingSet64;

                sandboxOutput.PeakMemoryUsage = currentMemoryUsage - beforeLoadMemoryUsage;

                SetSandboxOutputTrafficHistory(sandboxOutput, resMon);

                resMon.StopThrottle();
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error executing file:\r\n{0}\r\n{1}", ex.Message, ex.StackTrace);
            }

            return sandboxOutput;
        }

        private static void SetSandboxOutputTrafficHistory(SandboxOutput sandboxOutput, ResourceMonitor resourceMonitor)
        {
            var downloadQuota = resourceMonitor.NetThrottler.GetInputQuota();
            var uploadQuota = resourceMonitor.NetThrottler.GetOutputQuota();

            sandboxOutput.BytesDownloaded = downloadQuota.TotalBytesTransferred;
            sandboxOutput.BytesUploaded = uploadQuota.TotalBytesTransferred;

            var readQuota = resourceMonitor.FileThrottler.GetInputQuota();
            var writeQuota = resourceMonitor.FileThrottler.GetOutputQuota();

            sandboxOutput.BytesRead = readQuota.TotalBytesTransferred;
            sandboxOutput.BytesWritten = writeQuota.TotalBytesTransferred;
        }

        public void Die()
        {
            if(_job != null)
                _job.Dispose();
        }

        private sealed class Host : MarshalByRefObject
        {
            public SandboxOutput Run(SandboxOutput sandboxOutput, string className, string methodName, byte[] compiledAssembly, string jsonArgs, List<byte[]> assemblies)
            {
                if (sandboxOutput == null)
                    throw new ArgumentNullException();

                if (className == null)
                    throw new ArgumentNullException("className");

                if (methodName == null)
                    throw new ArgumentNullException("methodName");

                if (compiledAssembly == null)
                    throw new ArgumentNullException("compiledAssembly");

                object returnValue = null;

                var assembly = Assembly.Load(compiledAssembly);
                var targetType = assembly.GetType(className, true);
                var target = targetType.GetMethod(methodName);

                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    var args = GetJsonArgsAsObjectArray(target, jsonArgs);
                    sandboxOutput.StartTime = DateTime.UtcNow;
                    returnValue = target.Invoke(null, args);
                }

                catch (Exception exc)
                {
                    Console.Error.WriteLine(exc.InnerException.Message);
                }

                finally
                {
                    sw.Stop();
                    SetReturnValueJson(sandboxOutput, returnValue);
                    sandboxOutput.EndTime = DateTime.UtcNow;
                    sandboxOutput.RuntimeMilliseconds = sw.ElapsedMilliseconds;
                }

                return sandboxOutput;
            }

            private static object[] GetJsonArgsAsObjectArray(MethodInfo methodInfo, string jsonArgs)
            {
                if (jsonArgs == null) return null;
                try
                {
                    var jArray = JArray.Parse(jsonArgs);
                    
                    var parameters = methodInfo.GetParameters();
                    var parameterCasts = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];

                        if (i >= jArray.Count)
                        {
                            parameterCasts[i] = GetDefaultValue(parameter.ParameterType);
                            continue;
                        }

                        try
                        {
                            var jParam = jArray[i];
                            var parameterType = parameter.ParameterType;

                            object obj;
                            switch (jParam.Type)
                            {
                                case JTokenType.String:
                                    obj = jParam.Value<string>();
                                    break;
                                default:
                                        obj = JsonConvert.DeserializeObject(jParam.ToString(), parameterType, new JsonSerializerSettings() { });
                                    break;
                            }

                            parameterCasts[i] = obj;
                        }

                        catch
                        {
                            // Intentionally ignore any errors trying to parse types.
                        }

                        finally
                        {
                            if (parameterCasts[i] == null)
                            {
                                parameterCasts[i] = GetDefaultValue(parameter.ParameterType);
                            }
                        }
                    }

                    return parameterCasts.Length == 0 ? null : parameterCasts.ToArray();

                }
                catch (Exception exception)
                {    
                    throw new Exception("Error parsing json arguments. " + exception.Message);
                }
            }

            static object GetDefaultValue(Type t)
            {
                if (t.IsValueType)
                {
                    return Activator.CreateInstance(t);
                }
                else
                {
                    return null;
                }
            }

            private static void SetReturnValueJson(SandboxOutput output, object retval)
            {
                try
                {
                    output.ReturnType = (retval == null ? typeof (void) : retval.GetType()).ToString();
                    // If it is a simple type such as string or integer, we just assign it verbatim.
                    // Otherwise, we serialize as JSON.

                    string json = String.Empty;
                    if (retval != null)
                    {
                        if (retval.GetType().IsPrimitive || retval is string)
                        {
                            json = retval.ToString();
                        }

                        else
                        {
                            json = JsonConvert.SerializeObject(retval, Formatting.Indented, 
                                new JsonSerializerSettings
                                    { 
                                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                });
                        }
                    }

                    output.RetVal = json;   
                }

                catch (Exception exception)
                {
                    output.Stderr = "Error serializing return value: " + exception.Message;
                }
            }
        }
    }
}