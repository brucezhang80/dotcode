using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Web;

namespace dotcode.Models
{
    /// <summary>
    /// Contains methods to assist in generating base URLs for
    /// the site
    /// </summary>
    public static class UrlMap
    {
        public const string CodeUnitUrlPrefix = "p";
        public const string UserUrlPrefix = "u";
        public const string AboutUrlPrefix = "about";
        public const string HelpUrlPrefix = "help";
        public const string CloneUrlPrefix = "clone";
        public const string RunUiUrlPrefix = "runui";
        public const string RunUrlPrefix = "r";
        public const string NewUrlPrefix = "new";
        public const string PasswordResetUrlPrefix = "passwordreset";

        public static string GetLoginUrl(string domain, string path)
        {
            domain = Regex.Replace(domain, "^http:", "https:");
            domain = Regex.Replace(domain, "49280", "44300");
            return domain + path;
        }

        public static string GetLogoutUrl()
        {
            return "/api/account/logout";
        }

        public static string ViewCodeUnit(long autoid, long version)
        {
            return String.Format("/{0}/{1}{2}", CodeUnitUrlPrefix, autoid, version == 0 ? "" : "/" + version + "/");
        }

        public static string ViewUser(string username)
        {
            return String.Format("/{0}/{1}", UserUrlPrefix, username);
        }
    }
}