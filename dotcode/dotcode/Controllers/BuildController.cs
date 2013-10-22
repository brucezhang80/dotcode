using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using JitJotData;
using dotcode.Models;
using dotcode.lib.common.Compiler;
using Language = dotcode.lib.common.Language;

namespace dotcode.Controllers
{
    public class BuildController : ApiController
    {
        [HttpPost]
        public ClientModel MethodReflect(ClientModel clientModel)
        {
            var reflection = BuildModel.GetReflection(clientModel.Source, clientModel.References, Language.CSharp5).ToList();
            var methods = clientModel.Reflection;
            if (methods != null)
            {
                foreach (var method in reflection)
                {
                    var oldMethod = methods.FirstOrDefault(m => m.MethodName == method.MethodName);
                    if (oldMethod == null) continue;

                    foreach (var parameter in method.Parameters)
                    {
                        var oldParameter =
                            oldMethod.Parameters.SingleOrDefault(p => p.ParameterName == parameter.ParameterName);
                        if (oldParameter == null) continue;

                        parameter.Value = oldParameter.Value;
                    }
               }
            }

            return new ClientModel { Reflection = reflection, MethodIndex = clientModel.MethodIndex };
        }

        public ClientModel SaveCodeUnit([FromBody] ClientModel clientModel)
        {
            var dbCodeUnit = CreateOrUpdateCodeUnit(clientModel.Id,
                clientModel.Source,
                clientModel.Title,
                clientModel.Description,
                clientModel.IsPublic,
                clientModel.References);

            return new ClientModel()
            {
                Id = dbCodeUnit.Id,
                AutoId = dbCodeUnit.AutoId,
                Reflection = clientModel.Reflection,
                VersionId = dbCodeUnit.VersionId,
                CompilerOutput = null,
                MethodIndex = clientModel.MethodIndex,
                Description = dbCodeUnit.Description,
                IsPublic = dbCodeUnit.IsPublic,
                LanguageId = Language.GetLanguageById(dbCodeUnit.LanguageId ?? 1).Id,
                References = clientModel.References,
                RuntimeOutput = null,
                Source = dbCodeUnit.Source,
                Title = dbCodeUnit.Title
            };
        }


        private CodeUnit CreateOrUpdateCodeUnit(Guid id, string source, string title, string description, bool ispublic, IEnumerable<Guid> references)
        {
            var codeUnitExists = DbModel.CodeUnitExistsById(id);
            var dbCodeUnit = !codeUnitExists
                                 ? DbModel.CreateCodeUnit(source, title, description, references)
                                 : DbModel.UpdateCodeUnit(id, source, title, description, ispublic, references);

            var compilerOutput = BuildModel.CompileInternal(source,
                                                ReferenceModel.GetProjectReferences(id),
                                                Language.GetLanguageById(dbCodeUnit.LanguageId));

            BuildModel.SaveCompilerOutputToDb(new dotcodedbEntities(), id, compilerOutput);
            return dbCodeUnit;
        }


        [HttpPost]
        public ClientModel Compile([FromBody] ClientModel clientModel)
        {
            try
            {
                if (clientModel == null) return new ClientModel() { };
                var compilerOutput = BuildModel.CompileInternal(clientModel.Source, clientModel.References, Language.CSharp5);
                return new ClientModel
                {
                    CompilerOutput = new CompilerOutputSummary(compilerOutput)
                };

            }
            catch (Exception ex)
            {
                return new ClientModel(){CompilerOutput = new CompilerOutputSummary(){HasErrors = true, TimeStamp = DateTime.UtcNow}};
            }
        }
    }
}