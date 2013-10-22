using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JitJotData;
using Mirrors;
using WebMatrix.WebData;
using dotcode.Models;

namespace dotcode.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    public class AdminDashboardController : Controller
    {
        //
        // GET: /AdminDashboard/
        public ActionResult Index()
        {
            WebSecurity.RequireRoles("admin");

            return View("~/Views/Admin/Dashboard.cshtml");
        }

        public ActionResult SetDefaultProject(int autoid, int languageid)
        {
            var codeUnitExists = DbModel.CodeUnitExistsByAutoId(autoid);
            if (codeUnitExists)
            {
                var dotCodeDb = new dotcodedbEntities();
                var codeUnit = DbModel.GetCodeUnitByAutoId(autoid, 0);
                var adminSettings = dotCodeDb.AdminSettings.FirstOrDefault();
                if (adminSettings == null)
                {
                    adminSettings = new AdminSetting();
                    dotCodeDb.AdminSettings.Add(adminSettings);
                }

                adminSettings.DefaultCSharpProjectId = codeUnit.Id;

                dotCodeDb.SaveChanges();
            }

            return Index();
        }

        [HttpPost]
        public ActionResult UploadCommonReference(IEnumerable<HttpPostedFileBase> files)
        {
            var dotcodeDb = new dotcodedbEntities();
            foreach (var file in files)
            {
                if (file.ContentLength <= 0) continue;

                Mirror mirror;
                var bin = new Binary();
                var fileBytes = StreamToByteArray(file.InputStream);
                
                try
                {
                    mirror = new Mirror(fileBytes);
                }
                catch
                {
                    // We are intentionally ignoring invalid assemblies.
                    continue;
                }
                

                bin.Id = Guid.NewGuid();
                bin.RawAssembly = fileBytes;
                bin.FileName = file.FileName;
                bin.Type = 2;
                bin.RuntimeVersion = mirror.GetImageRuntimeVersion();
                bin.Description = mirror.GetAssemblyFileVersionInfo(bin.RawAssembly).Comments;

                dotcodeDb.Binaries.Add(bin);
            }

            dotcodeDb.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult DeleteCommonReference(Guid? id = null)
        {
            return RedirectToAction("Index");
        }

        public static byte[] StreamToByteArray(Stream stream)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
