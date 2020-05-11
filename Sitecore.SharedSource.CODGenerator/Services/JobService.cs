using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.CODG.Services
{
    public class JobService
    {
        public void SetJobMessage(string message)
        {
            if (Context.Job != null)
                Context.Job.Status.Messages.Add(message);
        }
    }
}