using Sitecore.Data.Items;
using Sitecore.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.CODG.Services
{
    public class PresentationService
    {
        public List<PlaceholderDefinition> GetPlaceholders(Item item, string deviceId)
        {
            var layoutField = item.Fields[FieldIDs.LayoutField];
            var finalLayoutField = item.Fields[FieldIDs.FinalLayoutField];
            if (layoutField == null && finalLayoutField == null)
                return new List<PlaceholderDefinition>();

            var finalLayout = LayoutDefinition.Parse(finalLayoutField.Value);
            var deviceItem = finalLayout.GetDevice(deviceId);
            if (deviceItem != null && deviceItem.Renderings != null && deviceItem.Renderings.Count > 0)
                return deviceItem.Placeholders?.Cast<PlaceholderDefinition>().ToList() ?? new List<PlaceholderDefinition>();

            var layout = LayoutDefinition.Parse(layoutField.Value);
            deviceItem = layout.GetDevice(deviceId);

            return deviceItem.Placeholders?.Cast<PlaceholderDefinition>().ToList() ?? new List<PlaceholderDefinition>();
        }
    }
}