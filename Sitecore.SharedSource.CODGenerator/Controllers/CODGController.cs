using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sitecore.Data;
using Sitecore.Jobs;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using System.IO;
using System.Configuration;
using System.Reflection;
using Sitecore.SharedSource.CODG.Models;
using Sitecore.SharedSource.CODG.Services;

namespace Sitecore.SharedSource.CODG.Controllers
{
    public class CODGController : Controller
    {
        public ActionResult Index(string id, string language, string version, string db)
        {
            return View("GenerateForm");
        }

        public ActionResult GenerateForm()
        {
            var handleName = $"CODG-{DateTime.UtcNow:yyyy/MM/dd-hh:mm}";
            var codService = new CODService();

            var jobOptions = new JobOptions(
                handleName,
                "COD Generation",
                Context.Site.Name,
                codService,
                "Generate",
                new object[] { });

            JobManager.Start(jobOptions);

            return Json(new
            {
                Failed = false,
                HandleName = handleName,
                Error = ""
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetJobStatus(string handleName)
        {
            var j = JobManager.GetJob(handleName);

            var message = j?.Status?.Messages?.Count > 0
                ? j.Status.Messages[j.Status.Messages.Count - 1]
                : "";

            var file = !string.IsNullOrWhiteSpace(message) && (j?.IsDone ?? false)
                ? j.Status.Messages[j.Status.Messages.Count - 2]
                : "";
            
            return Json(new {
                Current = j?.Status.Processed ?? 0,
                Total = j?.Status.Total ?? 0,
                Completed = j?.IsDone ?? true,
                Message = message,
                File = file
            });
        }
    }
}