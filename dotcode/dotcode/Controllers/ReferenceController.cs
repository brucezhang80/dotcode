using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using JitJotData;
using dotcode.Models;

namespace dotcode.Controllers
{
    public class ReferenceController : Controller
    {
        public ActionResult Main(Guid id)
        {
            return View("Main", id);
        }

        [System.Web.Http.HttpPost]
        public ActionResult EditReferences([FromBody] ClientModel clientModel)
        {
            return View("Popup", clientModel);
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult ListReferences([FromBody] ClientModel clientModel)
        {
            if (clientModel == null) return null;
            var dotCodeDb = new dotcodedbEntities();

            IEnumerable<Binary> codeUnitReferences;

            // Conditionally load based on this flag.  Either load saved references or
            // load references based on the Guid[] in this class.
            if (clientModel.LoadSavedReferences)
            {
                codeUnitReferences = from cur in dotCodeDb.CodeUnitDllReferences.Include("Binary")
                                     where !cur.IsTemporaryReference && cur.CodeUnitId == clientModel.Id
                                     select cur.Binary;
            }

            else
            {
                codeUnitReferences = from cur in dotCodeDb.Binaries
                                     where clientModel.References.Contains(cur.Id)
                                     select cur;
            }

            var binaries = codeUnitReferences
                .OrderBy(d => d.Type)
                .ThenBy(d => d.FileName)
                .ToArray()
                .Select(b => new Binary {Id=b.Id, FileName = b.FileName, Description = b.Description, RuntimeVersion = b.RuntimeVersion});

            return View("List", new Tuple<Guid, IEnumerable<Binary>>(clientModel.Id, binaries));
        }

        [System.Web.Http.HttpPost]
        public ActionResult ListCustomReferencesPartial([FromBody] ClientModel clientModel)
        {
            return View("_CustomReferencePartial", clientModel);
        }

        [System.Web.Mvc.HttpGet]
        public IEnumerable<Binary> GetProjectUploadedReferences(ClientModel clientModel)
        {
            return ReferenceModel.GetProjectCustomReferences(clientModel);
        }

        [System.Web.Mvc.HttpPost]
        public string UploadCustomProjectReference(ClientModel clientModel, IEnumerable<HttpPostedFileBase> files)
        {
            if (clientModel.Id == Guid.Empty)
                return String.Empty;

            var ids = ReferenceModel.UploadCustomProjectReference(clientModel, files);
            return new JavaScriptSerializer().Serialize(ids);
        }

        [System.Web.Http.HttpGet]
        public void DeleteUploadedReference(Guid projectid, Guid dllReferenceId)
        {
            ReferenceModel.DeleteCustomProjectReference(projectid, dllReferenceId);
        }
    }
}
