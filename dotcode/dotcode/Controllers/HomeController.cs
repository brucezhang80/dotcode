using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using System.Web.Mvc;
using JitJotData;
using Mirrors;
using Shared;
using WebMatrix.WebData;
using dotcode.Extensions;
using dotcode.Models;
using System.Linq;
using dotcode.lib.common.Compiler;
using Language = dotcode.lib.common.Language;

namespace dotcode.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(int id = -1, int version = -1)
        {
            CodeUnit codeUnit = null;

            if (version < 0) 
                version = 0;

            if (id < 0)
            {
                codeUnit = DbModel.GetDefaultProjectByLanguage(Language.CSharp5);
            }

            else
            {
                // We need to have an admin page for default code blocks to show per language.
                codeUnit = DbModel.GetCodeUnitByAutoId(id, version) ?? new CodeUnit();
                var canUserViewCode = (codeUnit.IsPublic || codeUnit.UserId == null || WebSecurity.CurrentUserId == codeUnit.UserId);

                if (codeUnit.Id == Guid.Empty || !canUserViewCode)
                {
                    Response.Redirect("/", true);
                }    
            }

            var db = new dotcodedbEntities();
            var compilerOut = db.CompilerOutputs.Include("Binary").SingleOrDefault(c => c.CodeUnitId == codeUnit.Id);

            var references = (ReferenceModel.GetProjectReferences(codeUnit.Id) ?? Enumerable.Empty<Guid>()).ToList();
            var clientModel = new ClientModel
            {
                AutoId = codeUnit.AutoId,
                Description = codeUnit.Description,
                Id = codeUnit.Id,
                IsPublic = codeUnit.IsPublic,
                LanguageId = (codeUnit.LanguageId ?? 0),
                Source = codeUnit.Source,
                Title = codeUnit.Title,
                VersionId = codeUnit.VersionId,
                References = references,
                ShowRunUrl = compilerOut != null,
                Reflection = compilerOut == null ? new List<MethodDefinition>() : BuildModel.GetReflection(compilerOut.Binary.RawAssembly)
            };

            if (!String.IsNullOrWhiteSpace(codeUnit.Source))
            {
                clientModel.Reflection =
                    BuildModel.GetReflection(codeUnit.Source, references, Language.CSharp5).ToList();
            }

            return View(clientModel);
        }

        public ActionResult ResetPassword(string token = null)
        {
            token = (token ?? String.Empty).Trim();
            var userid = String.IsNullOrWhiteSpace(token) ? -1 : WebSecurity.GetUserIdFromPasswordResetToken(token);
            if (userid == -1)
            {
                return View("ResetPassword");
            }

            else
            {
                return View("ChangePassword", (object) token);
            }
        }

        public ActionResult Login()
        {
            return View("Login");
        }

        public ActionResult About()
        {
            return View("About");
        }

        public ActionResult Help()
        {
            return View("Help");
        }

        public void New()
        {
            if (!ValidationModel.CanUseCreateNewProject()) return;
            
            var dotcodedb = new dotcodedbEntities();
            var codeUnit = new CodeUnit() {
                UserId = WebSecurity.CurrentUserId, 
                Id = Guid.NewGuid(),
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                IsPublic = true,
                LanguageId = 1,
                Source = "",
                VersionId = 0
            };

            var autoid = new CodeUnitAutoId();
            dotcodedb.CodeUnitAutoIds.Add(autoid);
            dotcodedb.SaveChanges();

            codeUnit.AutoId = autoid.Id;
            dotcodedb.CodeUnits.Add(codeUnit);
            dotcodedb.SaveChanges();

            Response.Redirect(Url.Action("Index", new{id = codeUnit.AutoId}), true);
        }

        public ClientModel EditProjectInfo(ClientModel clientModel)
        {
            if (!ValidationModel.CanUserEditOrSaveProject(clientModel.Id)) return clientModel;

            var dotCodeDb = new dotcodedbEntities();
            var codeUnit = dotCodeDb.CodeUnits.SingleOrDefault(c => c.Id == clientModel.Id);
            if (codeUnit == null) return clientModel;
            codeUnit.IsPublic = clientModel.IsPublic;

            dotCodeDb.CodeUnits.Attach(codeUnit);
            dotCodeDb.Entry(codeUnit).State = EntityState.Modified;
            dotCodeDb.SaveChanges();

            return clientModel;
        }

        [System.Web.Http.HttpPost]
        public ActionResult FormatCompilerOutput([FromBody] CompilerOutputSummary compilerOutputSummary)
        {
            return View("~/Views/Partial/_CompilerPartial.cshtml", compilerOutputSummary);
        }

        [System.Web.Http.HttpPost, ValidateInput(false)]
        public string RunConsole([FromBody] ClientModel clientModel, [FromUri] string method, [FromUri] string jsonArgs = null)
        {
            var dotcodedb = new dotcodedbEntities();
            var log = new RunLog();
            var ip = Request.ServerVariables["REMOTE_ADDR"];
            string output = null;

            log.IPAddress = ip;
            log.Input = method + ", " + jsonArgs;
            log.UserId = WebSecurity.CurrentUserId;

            try
            {
                SandboxOutput sandboxOutput = null;
                if (clientModel != null && !String.IsNullOrWhiteSpace(method))
                {
                    var host = Request.Headers["HOST"];
                    var compilerOut = BuildModel.CompileSource(clientModel.Source, clientModel.References,
                                                               lib.common.Language.GetLanguageById(clientModel.LanguageId));

                    sandboxOutput = compilerOut.HasErrors ?
                        new SandboxOutput() { Stderr = "Build failed.  Check compiler errors for more details." }
                        : SandboxModel.RunInternal(host, compilerOut.CompiledAssembly, clientModel.References, method, jsonArgs);
                }

                output = sandboxOutput.ToJsonString();
            }

            finally
            {
                log.Output = output;
                dotcodedb.RunLogs.Add(log);
                dotcodedb.SaveChanges();
            }

            return output;
        }

        public ActionResult ShowConsole()
        {
            return View("~/Views/Editor/_Console.cshtml", null);
        }

        public ActionResult ProjectInfo(Guid id)
        {
            return PartialView("~/Views/Editor/_ProjectInfo.cshtml", id);
        }

        public PartialViewResult RenderNavBar(Guid? id)
        {
            ClientModel model = null;
            if (id != null && id != Guid.Empty)
                model = new ClientModel { Id = (Guid) id };
            return PartialView("~/Views/Shared/_NavigationBar.cshtml", model);
        }

        public ActionResult UserDetails(string username)
        {
            var dotCodeDb = new dotcodedbEntities();
            var user = dotCodeDb.Users.SingleOrDefault(u => u.Username.ToLower() == username.ToLower());
            return View("~/Views/Account/UserDetails.cshtml", user);
        }

        [System.Web.Http.HttpPost]
        public long Clone(ClientModel clientModel)
        {
            var clonedCodeUnit = DbModel.CloneCodeUnit(clientModel.Id, clientModel.Source, clientModel.References);
            return clonedCodeUnit.AutoId;
        }
    }
}
