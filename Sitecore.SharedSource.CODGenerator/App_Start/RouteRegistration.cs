using System;
using System.Collections.Generic;
using System.Web.Routing;
using System.Linq;
using Sitecore.Pipelines;
using System.Web.Mvc;
using Sitecore.SharedSource.CODG.Controllers;

namespace Sitecore.SharedSource.CODG.App_Start
{
    public class CODGRouteRegistration
    {
        public virtual void Process(PipelineArgs args)
        {
            RouteTable.Routes.MapRoute(
                name: "CustomRoute",
                url: "CODG/{controller}/{action}/{id}",
                defaults: new { controller = "CODG", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { typeof(CODGController).Namespace }
            );
        }
    }
}