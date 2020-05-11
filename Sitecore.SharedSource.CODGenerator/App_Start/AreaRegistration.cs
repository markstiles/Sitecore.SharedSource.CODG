using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sitecore.SharedSource.CODG.App_Start
{
    public class CODGAreaRegistration : AreaRegistration
    {
        public override string AreaName => "CODG";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(AreaName, "CODG/{controller}/{action}", new
            {
                area = AreaName
            });
        }
    }
}