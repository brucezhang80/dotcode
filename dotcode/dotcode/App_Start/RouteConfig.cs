using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MvcDomainRouting.Code;
using dotcode.Models;

namespace dotcode
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.Add("ShowConsoleRoute", new DomainRoute(
                "run.jitjot.net",     // Domain with parameters
                "home/showconsole",    // URL with parameters
                new { controller = "home", action = "showconsole", id = UrlParameter.Optional }  // Parameter defaults
            ));

            routes.Add("RunRoute", new DomainRoute(
                "run.jitjot.net",     // Domain with parameters
                "r/{codeunitid}/{method}",    // URL with parameters
                new { controller = "sandbox", action = "run", id = UrlParameter.Optional }  // Parameter defaults
            ));

            routes.Add("BlockRoute", new DomainRoute(
                @"\w+.jitjot.net",     // Domain with parameters
                "",    // URL with parameters
                new { controller = "", action = "", id = UrlParameter.Optional },  // Parameter defaults
                new StopRoutingHandler()
            ));

            routes.MapRoute(
                "LoadAndRunUI",
                UrlMap.RunUiUrlPrefix + "/{method}",
                defaults: new
                {
                    controller = "Home",
                    action = "RunConsole",
                    jsonArgs = UrlParameter.Optional
                });

            routes.MapRoute("passwordReset", UrlMap.PasswordResetUrlPrefix + "/{token}", new {controller = "Home", action = "ResetPassword", token = UrlParameter.Optional});
            routes.MapRoute("new", UrlMap.NewUrlPrefix, defaults: new { controller = "Home", action = "New", id = UrlParameter.Optional });
            routes.MapRoute("about", UrlMap.AboutUrlPrefix, defaults: new { controller = "Home", action = "About", id = UrlParameter.Optional });
            routes.MapRoute("help", UrlMap.HelpUrlPrefix, defaults: new { controller = "Home", action = "Help", id = UrlParameter.Optional });
            routes.MapRoute("clone", UrlMap.CloneUrlPrefix, defaults: new { controller = "Home", action = "Clone", id = UrlParameter.Optional });
            routes.MapRoute("user", UrlMap.UserUrlPrefix + "/{username}", defaults: new { controller = "Home", action = "UserDetails", id = UrlParameter.Optional });
            routes.MapRoute("home", UrlMap.CodeUnitUrlPrefix + "/{id}/{version}", defaults: new { controller = "Home", action = "Index", version = UrlParameter.Optional });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

        }
    }
}