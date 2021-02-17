using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.CODG.Models
{
    public class CODModel
    {
        public CODModel()
        {
            Layouts = new Dictionary<Guid, Item>();
            Containers = new Dictionary<Guid, Item>();
            Renderings = new Dictionary<Guid, Item>();
            Sublayouts = new Dictionary<Guid, Item>();
            Pages = new Dictionary<Guid, Item>();
            Datasources = new Dictionary<Guid, Item>();
            RenderingParameters = new Dictionary<Guid, Item>();
            BaseTemplates = new Dictionary<Guid, Item>();
            Enumerations = new Dictionary<Guid, Item>();
        }
        
        public Dictionary<Guid, Item> Layouts { get; set; }
        public Dictionary<Guid, Item> Containers { get; set; }
        public Dictionary<Guid, Item> Renderings { get; set; }
        public Dictionary<Guid, Item> Sublayouts { get; set; }
        public Dictionary<Guid, Item> Pages { get; set; }
        public Dictionary<Guid, Item> Datasources { get; set; }
        public Dictionary<Guid, Item> RenderingParameters { get; set; }
        public Dictionary<Guid, Item> BaseTemplates { get; set; }
        public Dictionary<Guid, Item> Enumerations { get; set; }
    }
}