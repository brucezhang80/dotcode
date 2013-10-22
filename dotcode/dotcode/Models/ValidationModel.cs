using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using WebMatrix.WebData;

namespace dotcode.Models
{
    public class ValidationModel
    {
        public static bool IsNewPasswordValid(string password)
        {
            return password != null && password.Length >= 8;
        }

        public static bool CanUserSetProjectLanguage(Guid projectid)
        {
            // Only allow setting language if project does not exist in database,
            // therefore being a new, unsaved project.
            var codeUnitExists = DbModel.CodeUnitExistsById(projectid);
            return !codeUnitExists;
        }

        public static bool CanUserCloneProject(Guid projectid)
        {
            return WebSecurity.IsAuthenticated && DbModel.CodeUnitExistsById(projectid);
        }

        public static bool CanUserBuildOrRunCode(Guid projectid)
        {
            return DbModel.CodeUnitExistsById(projectid);
        }

        public static bool CanUserEditOrSaveProject(Guid projectid)
        {
            if (!DbModel.CodeUnitExistsById(projectid)) return false;
            var codeUnit = DbModel.GetCodeUnitById(projectid);
            return codeUnit.UserId == WebSecurity.CurrentUserId;
        }

        public static bool CanUserSetProjectReferences(Guid id)
        {
            var codeUnit = DbModel.GetCodeUnitById(id);
            return codeUnit != null && codeUnit.UserId == WebSecurity.CurrentUserId;
        }

        public static bool CanUseCreateNewProject()
        {
            return WebSecurity.IsAuthenticated;
        }
    }
}