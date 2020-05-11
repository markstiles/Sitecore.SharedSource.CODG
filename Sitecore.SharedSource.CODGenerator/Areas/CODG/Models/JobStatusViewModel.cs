using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.CODG.Areas.CODG.Models
{
    public class JobStatusViewModel
    {
        public long Current { get; set; }
        public long Total { get; set; }
        public bool Completed { get; set; }
        public string Message { get; set; }
    }
}