using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shared;

namespace Sandbox.NET
{
    public class Sandbox
    {
        public event Func<string> Stdout;
        public event Func<string> Stderr; 

        private SandboxConfig _sandboxConfig;
        private static string _sandboxHostExe = "sandboxhost.exe";
        private static string[] _sandboxHostDependencies = new[]
        {
            "shared.dll", "sandbox.net.dll", "managedjob.dll", "Newtonsoft.Json.dll", "BandwidthThrottle.dll",
            "EasyHook.dll", "EasyHook32.dll", "EasyHook64.dll"
        };
        
        private bool _stdOutTruncated = false;
        private bool _stdErrTruncated = false;
        private StringBuilder _stdOut;
        private StringBuilder _stdErr;

        public Sandbox(SandboxConfig sandboxConfig)
        {
            _sandboxConfig = sandboxConfig;
        }

        public SandboxOutput Run()
        {
            return Run(_sandboxConfig);
        }

        private SandboxOutput Run(SandboxConfig sandboxConfig)
        {
            try
            {
                _stdOut = new StringBuilder();
                _stdErr = new StringBuilder();

                var sandboxHostDestFile = InitSandbox(sandboxConfig);
                var clientUri = ServiceHelper.GetServiceUri(Path.Combine("client", sandboxConfig.Uid));

                var sandboxHostProcess = CreateSandboxHostProcess(sandboxHostDestFile, clientUri);

                // Start the local service to transfer data between host and client process.
                var clientService = new SandboxClientService(sandboxConfig, sandboxHostProcess);
                using (ServiceHelper.StartService(clientUri, clientService))
                {
                    try
                    {
                        // Capture stdout and stderr from process and redirect to stringbuilder
                        sandboxHostProcess.OutputDataReceived += SandboxHostProcessOnOutputDataReceived;
                        sandboxHostProcess.ErrorDataReceived += SandboxHostProcessOnErrorDataReceived;

                        // Start external sandbox hosting process
                        sandboxHostProcess.Start();
                        sandboxHostProcess.BeginOutputReadLine();
                        sandboxHostProcess.BeginErrorReadLine();
                        sandboxHostProcess.WaitForExit((int) sandboxConfig.MaxRunTime.TotalMilliseconds);
                    }

                    catch (Exception e)
                    {
                        _stdErr.AppendLine(e.Message);
                    }

                    finally
                    {
                        sandboxHostProcess.OutputDataReceived -= SandboxHostProcessOnOutputDataReceived;
                        sandboxHostProcess.ErrorDataReceived -= SandboxHostProcessOnErrorDataReceived;
                    }
                }

                if (!sandboxHostProcess.HasExited)
                {
                    sandboxHostProcess.Kill();
                    _stdErr.Insert(0, "Process killed");
                }

                var sandboxOutput = clientService.SandboxOutput;

                if (_stdOutTruncated)
                    _stdOut.AppendLine("--- output truncated ---");
                if (_stdErrTruncated)
                    _stdErr.AppendLine("--- output truncated ---");

                sandboxOutput.Stdout = _stdOut.ToString().Trim();
                sandboxOutput.Stderr = _stdErr.ToString().Trim();

                return sandboxOutput;

            }
            finally
            {
                DisposeSandbox(sandboxConfig);
            }
        }

        private void SandboxHostProcessOnErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            var pendingAppend = args.Data ?? String.Empty;
            if (_stdErr.Length + pendingAppend.Length <= _sandboxConfig.ConsoleBufferMax)
                _stdErr.AppendLine(args.Data);
            else
                _stdErrTruncated = true;
        }

        private void SandboxHostProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (_stdOutTruncated) return;

            var pendingAppend = args.Data ?? String.Empty;
            if (_stdOut.Length + pendingAppend.Length <= _sandboxConfig.ConsoleBufferMax)
                _stdOut.AppendLine(pendingAppend);
            else
                _stdOutTruncated = true;
        }

        private Process CreateSandboxHostProcess(string sandboxHostDestFile, Uri clientUri)
        {
            var psi = new ProcessStartInfo(sandboxHostDestFile)
                {
                    Arguments = clientUri.ToString(), // Pass in the URI for the callback service
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = _sandboxConfig.ApplicationBinDirectory,
                };

            var process = new Process {StartInfo = psi};

            return process;
        }

        private void DisposeSandbox(SandboxConfig sandboxConfig, int attempts = 0)
        {
            if (attempts < 0 || attempts > 3) return;

            var retry = false;

            try
            {
                if (Directory.Exists(sandboxConfig.ApplicationBaseDirectory))
                {
                    Directory.Delete(sandboxConfig.ApplicationBaseDirectory, true);
                }   
            }

            catch (Exception)
            {
                retry = true;
            }

            if (!retry) return;

            Thread.Sleep(1000 * (attempts + 1));
            DisposeSandbox(sandboxConfig, attempts + 1);
        }

        private string InitSandbox(SandboxConfig sandboxConfig)
        {
            // Create the directories for the sandbox
            Directory.CreateDirectory(sandboxConfig.ApplicationBinDirectory);
            Directory.CreateDirectory(sandboxConfig.ApplicationDataDirectory);

            // Copy sandboxhost.exe and dependencies to folder
            foreach (var sourceFile in _sandboxHostDependencies.Concat(new[] { _sandboxHostExe }))
            {
                var sourceFull = Path.Combine(sandboxConfig.SandboxGlobalReferenceLocation, sourceFile);
                var sandboxHostDestFile = Path.Combine(sandboxConfig.ApplicationBinDirectory, Path.GetFileName(sourceFile));

                File.Copy(sourceFull, sandboxHostDestFile, true);
            }

            // Copy additional references to bin directory
            if (sandboxConfig.AdditionalReferences != null)
            {
                foreach (var additionalReference in sandboxConfig.AdditionalReferences)
                {
                    var binFilePath = Path.Combine(sandboxConfig.ApplicationBinDirectory,
                                                    additionalReference.AssemblyName);
                    File.WriteAllBytes(binFilePath, additionalReference.AssemblyBytes);
                }
            }

            File.WriteAllBytes(
                Path.Combine(sandboxConfig.ApplicationBinDirectory, sandboxConfig.SandboxedAssembly.AssemblyName),
                sandboxConfig.SandboxedAssembly.AssemblyBytes);

            // Return the path to the sandboxhost executable in the sandbox folder
            return Path.Combine(sandboxConfig.ApplicationBinDirectory, _sandboxHostExe);
        }
    }
}
