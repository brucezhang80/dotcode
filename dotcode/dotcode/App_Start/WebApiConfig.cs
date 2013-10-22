using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using dotcode.Models;

namespace dotcode
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                "Activation", 
                "activate/{username}/{token}",
                new {
                        controller = "account",
                        action = "ActivateAccount"
                    });

            config.Routes.MapHttpRoute(
                name: "ControllerAndAction",
                routeTemplate: "api/{controller}/{action}");

            config.Routes.MapHttpRoute(
                name: "ControllerAndActionAndId",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional });

            config.Routes.MapHttpRoute(
                name: "LoadAndRun",
                routeTemplate:  UrlMap.RunUrlPrefix + "/{codeunitid}/{version}/{method}",
                defaults: new
                {
                    controller = "sandbox",
                    action = "run",
                    jsonArgs = RouteParameter.Optional,
                    retattr = RouteParameter.Optional
                });
        }
    }
}
