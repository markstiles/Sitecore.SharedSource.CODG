using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Jobs;
using Sitecore.SharedSource.CODG.Areas.CODG.Models;

namespace Sitecore.SharedSource.CODG.Services
{
    public class CODService
    {
        #region Constructor

        public string RenderingParameterField = "Parameters Template";
        public string DatasourceTemplateField = "Datasource Template";

        protected List<string> EnumFieldTypes = new List<string> {
            "Checklist", "Droplist", "Grouped Droplink", "Grouped Droplist", "Multilist", "Multilist with Search",
            "Treelist", "TreelistEx", "Droplink", "Droptree", "tree", "tree list", "Treelist with Search"
        };

        protected ID DeviceId = new ID("{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}");
        protected ID PlaceholdeSettingId = new ID("{5C547D4E-7111-4995-95B0-6B561751BF2E}");

        protected HSSFWorkbook Workbook { get; set; }

        protected IFont BoldFont { get; set; }
        protected IFont DefaultFont { get; set; }

        protected ICellStyle HeadStyle { get; set; }
        protected ICellStyle GreenStyle { get; set; }
        protected ICellStyle DefaultStyle { get; set; }

        protected PresentationService PresentationService { get; set; }
        protected JobService JobService { get; set; }

        public CODService()
        {
            Workbook = new HSSFWorkbook();

            BoldFont = Workbook.CreateFont();
            BoldFont.IsBold = true;
            BoldFont.FontName = "Calibri";
            BoldFont.FontHeightInPoints = 11;

            DefaultFont = Workbook.CreateFont();
            DefaultFont.FontName = "Calibri";
            DefaultFont.FontHeightInPoints = 11;

            HeadStyle = Workbook.CreateCellStyle();
            HeadStyle.SetFont(BoldFont);

            GreenStyle = Workbook.CreateCellStyle();
            GreenStyle.SetFont(DefaultFont);
            GreenStyle.FillForegroundColor = HSSFColor.LightGreen.Index;
            GreenStyle.FillPattern = FillPattern.BigSpots;
            GreenStyle.FillBackgroundColor = HSSFColor.LightGreen.Index;

            DefaultStyle = Workbook.CreateCellStyle();
            DefaultStyle.SetFont(DefaultFont);

            PresentationService = new PresentationService();
            JobService = new JobService();
        }

        #endregion

        #region Root

        public void Generate()
        {
            //Configuration - site configuration
            //Global Layouts - header / footer
            //Pages - list page types and base templates
            //Components - crawl renderings / sublayouts
            //Base Templates - templates with links to other templates but no links to components, pages or individual instances
            //Layout Containers - not sure a good way to determine 
            //Related Items - taxonomy, navigation
            //Enumerations - lists

            if (Context.Job != null)
            { 
                Context.Job.Options.Priority = ThreadPriority.Highest;
                Context.Job.Status.Total = 10;
            }

            var db = Configuration.Factory.GetDatabase("master");

            var model = BuildModel(db);
            if (Context.Job != null)
                Context.Job.Status.Processed = 1;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            string filePath = $"{HttpRuntime.AppDomainAppPath}Areas\\CODG\\Files\\COD.{DateTime.Now.ToString("yyyy.MM.dd.H.mm.ss")}.xls";
            
            var dsi = PropertySetFactory.CreateDocumentSummaryInformation();
            dsi.Company = "Velir";
            Workbook.DocumentSummaryInformation = dsi;
            
            var baseTemplateSheet = Workbook.CreateSheet("Base Templates");
            FillBaseTemplateSheet(Workbook, baseTemplateSheet, model, db);
            if (Context.Job != null)
                Context.Job.Status.Processed = 2;

            Workbook.CreateSheet("Configuration");
            if (Context.Job != null)
                Context.Job.Status.Processed = 3;

            Workbook.CreateSheet("Global Layouts");
            if (Context.Job != null)
                Context.Job.Status.Processed = 4;

            var containerSheet = Workbook.CreateSheet("Layout Containers");
            FillContainerSheet(Workbook, containerSheet, model, db);
            if (Context.Job != null)
                Context.Job.Status.Processed = 5;

            var pageSheet = Workbook.CreateSheet("Pages");
            FillPageSheet(Workbook, pageSheet, model, db);
            if (Context.Job != null)
                Context.Job.Status.Processed = 6;

            var componentSheet = Workbook.CreateSheet("Components");
            FillComponentSheet(Workbook, componentSheet, model, db);
            if (Context.Job != null)
                Context.Job.Status.Processed = 7;

            Workbook.CreateSheet("Related Items");
            if (Context.Job != null)
                Context.Job.Status.Processed = 8;

            var enumSheet = Workbook.CreateSheet("Enumerations");
            FillEnumerationSheet(Workbook, enumSheet, model, db);
            if (Context.Job != null)
                Context.Job.Status.Processed = 9;

            Workbook.CreateSheet("Field Type Legend");
            if (Context.Job != null)
                Context.Job.Status.Processed = 10;

            if (Context.Job != null)
                Context.Job.Status.State = JobState.Finished;

            FileStream file = new FileStream(filePath, FileMode.Create);
            Workbook.Write(file);
            file.Close();
        }
        
        public CODModel BuildModel(Database db)
        {
            var model = new CODModel();

            var layoutItems = db.SelectItems("/sitecore/layout/Layouts//*[@@templatename='Layout']");
            var line = 1;
            var totalLines = layoutItems.Length;
            foreach (Item i in layoutItems)
            {
                model.Layouts.Add(i.ID.Guid, i);

                JobService.SetJobMessage($"Processed layout item {line} of {totalLines}");
                line++;
            }
               
            var renderingItems = db.SelectItems("/sitecore/layout/Renderings//*[@@templatename='View rendering' or @@templatename='Controller rendering']");
            line = 1;
            totalLines = renderingItems.Length;
            foreach (Item i in renderingItems)
            {
                if (i.Name.ToLower().Contains("container"))
                    model.Containers.Add(i.ID.Guid, i);
                else
                    model.Renderings.Add(i.ID.Guid, i);

                JobService.SetJobMessage($"Processed rendering item {line} of {totalLines}");
                line++;
            }
                
            var sublayoutItems = db.SelectItems("/sitecore/templates//*[@@templatename='Sublayout']");
            line = 1;
            totalLines = sublayoutItems.Length;
            foreach (Item i in layoutItems)
            {
                if (i.Name.ToLower().Contains("container"))
                    model.Containers.Add(i.ID.Guid, i);
                else
                    model.Sublayouts.Add(i.ID.Guid, i);


                JobService.SetJobMessage($"Processed sublayout item {line} of {totalLines}");
                line++;
            }
                
            var templateItems = db.SelectItems("/sitecore/templates//*[@@templatename='Template']");
            line = 1;
            totalLines = templateItems.Length;
            foreach (Item itm in templateItems)
            {
                JobService.SetJobMessage($"Processed page/enum item {line} of {totalLines}");
                line++;

                var temp = db.Templates[itm.ID];

                if (temp.StandardValues != null && HasLayout(temp.StandardValues))
                {
                    model.Pages.Add(itm.ID.Guid, itm);
                    continue;
                }

                var templateFields = itm.Axes.GetDescendants().Where(a 
                    => a.TemplateName.Equals("Template field")
                    && EnumFieldTypes.Contains(a.Fields["Type"].Value)
                    && !string.IsNullOrWhiteSpace(a.Fields["Source"].Value));
                foreach(var f in templateFields)
                    AddSourceEnumTemplates(model, f);                
            }

            line = 1;
            foreach (Item itm in templateItems)
            {
                JobService.SetJobMessage($"Processed base template item {line} of {totalLines}");
                line++;

                var isNotDatasource = !model.Datasources.ContainsKey(itm.ID.Guid);
                var isNotRenderingParam = !model.RenderingParameters.ContainsKey(itm.ID.Guid);
                var isNotEnumeration = !model.Enumerations.ContainsKey(itm.ID.Guid);
                var itemLinks = Globals.LinkDatabase.GetReferrers(itm);
                var hasLinks = itemLinks.Length > 0;
                var allLinksAreTemplates = itemLinks.All(a => a?.GetSourceItem()?.Paths.FullPath.StartsWith("/sitecore/templates") ?? false);
                if (isNotDatasource && isNotRenderingParam && isNotEnumeration 
                    && hasLinks && allLinksAreTemplates)
                {
                    model.BaseTemplates.Add(itm.ID.Guid, itm);
                    continue;
                }
            }
            
            return model;
        }
        
        #endregion

        #region Fill Sheets

        protected void FillBaseTemplateSheet(HSSFWorkbook workbook, ISheet sheet, CODModel model, Database db)
        {
            BuildRow(sheet, HeadStyle, GetHeadValues());

            sheet.CreateFreezePane(0, 1, 0, 1);

            long line = 0;
            var totalLines = model.BaseTemplates.Count;

            foreach (KeyValuePair<Guid, Item> itm in model.BaseTemplates)
            {
                line++;

                BuildRow(sheet, GreenStyle, GetTemplateValues(itm.Value.Name, "Base Template"));

                EnumerateTemplateFields(workbook, sheet, db, itm.Value, "");

                JobService.SetJobMessage($"Processed base template item {line} of {totalLines}");
            }
        }

        protected void FillContainerSheet(HSSFWorkbook workbook, ISheet sheet, CODModel model, Database db)
        {
            BuildRow(sheet, HeadStyle, GetHeadValues());

            sheet.CreateFreezePane(0, 1, 0, 1);

            long line = 0;
            var totalLines = model.Containers.Count;

            foreach (KeyValuePair<Guid, Item> itm in model.Containers)
            {
                line++;

                BuildRow(sheet, GreenStyle, GetTemplateValues(itm.Value.Name, "Container"));

                EnumerateTemplateFields(workbook, sheet, db, itm.Value, "");

                JobService.SetJobMessage($"Processed container item {line} of {totalLines}");
            }
        }
        
        protected void FillPageSheet(HSSFWorkbook workbook, ISheet sheet, CODModel model, Database db)
        {
            BuildRow(sheet, HeadStyle, GetPageValues());

            sheet.CreateFreezePane(0, 1, 0, 1);

            long line = 0;
            var totalLines = model.Pages.Count;

            foreach (KeyValuePair<Guid, Item> itm in model.Pages)
            {
                line++;

                var baseField = (DelimitedField)itm.Value.Fields[FieldIDs.BaseTemplate];
                var baseList = baseField?.Items.Select(a => itm.Value.Database.GetItem(a)).Where(b => b != null).Select(c => c.Name);
                var baseValue = baseList.Any() ? string.Join(", ", baseList) : "";

                var device = (DeviceItem)itm.Value.Database.GetItem(DeviceId);
                var textInfo = new CultureInfo("en-US", false).TextInfo;
                var placeholders = PresentationService.GetPlaceholders(itm.Value, DeviceId.ToString());
                var placeholderKeys = new List<string>();
                if (placeholders.Any())
                    placeholderKeys = placeholders.Select(a => textInfo.ToTitleCase(a.Key.Replace("-", " "))).ToList();
                var rendValue = string.Join(", ", placeholderKeys);
                
                BuildRow(sheet, GreenStyle, GetPageTemplateValues(itm.Value.Name, "Page", baseValue, rendValue));

                EnumerateTemplateFields(workbook, sheet, db, itm.Value, "");

                JobService.SetJobMessage($"Processed page item {line} of {totalLines}");
            }
        }

        protected void FillEnumerationSheet(HSSFWorkbook workbook, ISheet sheet, CODModel model, Database db)
        {
            BuildRow(sheet, HeadStyle, GetHeadValues());

            sheet.CreateFreezePane(0, 1, 0, 1);

            long line = 0;
            var totalLines = model.Enumerations.Count;

            foreach (KeyValuePair<Guid, Item> itm in model.Enumerations)
            {
                line++;

                BuildRow(sheet, GreenStyle, GetTemplateValues(itm.Value.Name, "Enumeration"));

                EnumerateTemplateFields(workbook, sheet, db, itm.Value, "");

                JobService.SetJobMessage($"Processed enumeration item {line} of {totalLines}");
            }
        }

        protected void FillComponentSheet(HSSFWorkbook workbook, ISheet sheet, CODModel model, Database db)
        {
            BuildRow(sheet, HeadStyle, GetHeadValues());

            sheet.CreateFreezePane(0, 1, 0, 1);

            long line = 0;
            var totalLines = model.Renderings.Count;

            var d = new Dictionary<Guid, Item>();
            foreach(var a in model.Renderings)
                d.Add(a.Key, a.Value);
            foreach (var b in model.Sublayouts)
                d.Add(b.Key, b.Value);
            foreach (KeyValuePair<Guid, Item> itm in d)
            {
                line++;

                BuildRow(sheet, GreenStyle, GetTemplateValues($"{itm.Value.Name} ({itm.Value.TemplateName})", "Component"));

                var rpItem = GetItemByFieldId(db, itm.Value, RenderingParameterField);
                var dsItem = GetItemByFieldPath(db, itm.Value, DatasourceTemplateField);
                if (dsItem == null && rpItem == null)
                {
                    BuildRow(sheet, DefaultStyle, GetEmptyValues());
                    continue;
                }

                if (dsItem != null) 
                    EnumerateTemplateFields(workbook, sheet, db, dsItem, "Datasource");
                if (rpItem != null)
                    EnumerateTemplateFields(workbook, sheet, db, rpItem, "Rendering Parameters");

                JobService.SetJobMessage($"Processed component item {line} of {totalLines}");
            }
        }

        #endregion
        
        #region Get Values

        protected List<string> GetHeadValues()
        {
            var headValues = new List<string> { "Template Name", "Section", "Field", "Field Type", "Required?", "Default", "Type", "Inherits", "Notes", "Help Text" };
            return headValues;
        }

        protected List<string> GetPageValues()
        {
            var headValues = new List<string> { "Template Name", "Section", "Field", "Field Type", "Required?", "Default", "Type", "Inherits", "Placeholder Settings", "Notes", "Help Text" };
            return headValues;
        }

        protected List<string> GetPageTemplateValues(string templateName, string type, string inherits, string placeholderSettings)
        {
            var values = new List<string> { templateName, "", "", "", "", "", type, inherits, placeholderSettings, "", "" };
            return values;
        }

        protected List<string> GetTemplateValues(string templateName, string type)
        {
            var values = new List<string> { templateName, "", "", "", "", "", type, "", "", "" };
            return values;
        }

        protected List<string> GetTemplateNameValues(string templateName)
        {
            var values = new List<string> { templateName, "", "", "", "", "", "", "", "", "" };
            return values;
        }

        protected List<string> GetEmptyValues()
        {
            var emptyValues = new List<string> { "", "[no unique fields]", "", "", "", "", "", "", "", "" };
            return emptyValues;
        }

        protected List<string> GetSectionValues(string sectionName)
        {
            var sectionValues = new List<string> { "", sectionName, "", "", "", "", "", "", "", "" };
            return sectionValues;
        }

        protected List<string> GetFieldValues(string fieldName, string fieldType, bool isRequired, string defaultValue, string helpText)
        {
            var fieldValues = new List<string> { "", "", fieldName, fieldType, isRequired ? "Y" : "N", defaultValue, "", "", "", helpText };
            return fieldValues;
        }

        #endregion

        #region Helpers
        
        protected void AddSourceEnumTemplates(CODModel model, Item fieldItem)
        {
            var source = fieldItem.Fields["Source"].Value;
            
            // query:fast://*[@@templateid='{8288D4EA-6576-486F-AAEC-4680329A2D5C}' and @@key!='__standard values']
            if (source.ToLower().Contains("query"))
            {
                var queryItems = fieldItem.Database.SelectItems(source.Replace("query:", "")).ToList();
                foreach (var i in queryItems)
                {
                    if (!i.Paths.FullPath.StartsWith("/sitecore/content/"))
                        continue;

                    if (model.Enumerations.ContainsKey(i.TemplateID.Guid))
                        continue;

                    model.Enumerations.Add(i.TemplateID.Guid, i);
                }
            }

            // databasename=legacymaster&datasource=/sitecore/system/Languages
            // Datasource=/sitecore/content/home/Products/&IncludeTemplatesForSelection=Product Category
            else if(source.ToLower().Contains("datasource"))
            {
                var querystring = HttpUtility.ParseQueryString(source);
                var dictionary = querystring.Keys.Cast<string>().ToDictionary(k => k.ToLower(), v => querystring[v]);
                if (dictionary.ContainsKey("includetemplatesforselection"))
                {
                    var tempNames = dictionary["includetemplatesforselection"].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var name in tempNames)
                    {
                        var temp = fieldItem.Database.Templates[name];
                        if (temp != null && !model.Enumerations.ContainsKey(temp.ID.Guid))
                            model.Enumerations.Add(temp.ID.Guid, temp);
                    }
                }
                else if (dictionary.ContainsKey("datasource"))
                {
                    var path = dictionary["datasource"];
                    if (!path.StartsWith("/sitecore/content/"))
                        return;
                    
                    var folder = fieldItem.Database.GetItem(path);
                    if (folder == null || !folder.HasChildren)
                        return;
                    
                    var childrenIds = folder.GetChildren().Select(b => b.TemplateID.Guid).Distinct();
                    foreach (var c in childrenIds)
                    {
                        if (model.Enumerations.ContainsKey(c))
                            continue;

                        var tempId = new ID(c);
                        var tempItem = fieldItem.Database.GetItem(tempId);
                        model.Enumerations.Add(c, tempItem);
                    }
                }
            }

            // /sitecore/system/Settings/Rules/Segment Builder
            // {C0CAF698-8A42-4B66-9EAF-7D442B46F722}
            else
            {
                var folder = fieldItem.Database.GetItem(source);
                if (folder == null || !folder.Paths.FullPath.StartsWith("/sitecore/content/"))
                    return;

                if (!folder.HasChildren)
                    return;
                
                var childrenIds = folder.GetChildren().Select(b => b.TemplateID.Guid).Distinct();
                foreach (var c in childrenIds)
                {
                    if (model.Enumerations.ContainsKey(c))
                        continue;

                    var tempId = new ID(c);
                    var tempItem = fieldItem.Database.GetItem(tempId);
                    model.Enumerations.Add(c, tempItem);
                }
            }
        }

        protected void EnumerateTemplateFields(HSSFWorkbook workbook, ISheet sheet, Database db, Item item, string type)
        {
            if (!item.HasChildren)
                return;

            var tNameValues = GetTemplateNameValues(type);
            if (!string.IsNullOrWhiteSpace(type))
                BuildRow(sheet, DefaultStyle, tNameValues);

            var standardValues = db.Templates[item.ID]?.StandardValues;
            foreach (Item section in item.GetChildren())
            {
                if (section.Name.Equals("__Standard Values"))
                {
                    var templateNameValues = GetTemplateNameValues("__Standard Values");
                    BuildRow(sheet, DefaultStyle, templateNameValues);
                    continue;
                }

                var sectionValues = GetSectionValues(section.Name);
                BuildRow(sheet, DefaultStyle, sectionValues);

                foreach (Item field in section.GetChildren())
                {
                    var helpText = field?.Fields["__Long description"]?.Value ?? field?.Fields["__Short description"]?.Value;
                    var fieldValues = GetFieldValues(field.Name, field["Type"], IsRequired(field), standardValues?.Fields[field.Name]?.Value, helpText);
                    BuildRow(sheet, DefaultStyle, fieldValues);
                }
            }
        }

        protected int pos = 300;
        protected IRow BuildRow(ISheet sheet, ICellStyle style, List<string> list)
        {
            var row = sheet.CreateRow(sheet.PhysicalNumberOfRows);
            for (int i = 0; i < list.Count; i++)
            {
                var cell = row.CreateCell(i);
                if (style != null)
                    cell.CellStyle = style;
                cell.SetCellValue(list[i]);
            }

            return row;
        }

        protected Item GetItemByFieldId(Database db, Item item, string fieldName)
        {
            var rpField = item.Fields[fieldName];
            if (rpField == null || !ID.IsID(rpField.Value))
                return null;

            var rpID = new ID(rpField.Value);
            var rpItem = db.GetItem(rpID);

            return rpItem;
        }

        protected Item GetItemByFieldPath(Database db, Item item, string fieldName)
        {
            var dsField = item.Fields[fieldName];
            if (dsField == null)
                return null;

            var dsItem = db.GetItem(dsField.Value);

            return dsItem;
        }

        protected bool HasLayout(Item item)
        {
            return item.Fields[FieldIDs.LayoutField] != null && !string.IsNullOrEmpty(item.Fields[FieldIDs.LayoutField].Value);
        }

        protected bool IsRequired(Item item)
        {
            var requiredID = "{59D4EE10-627C-4FD3-A964-61A88B092CBC}";
            var qab = item.Fields["Quick Action Bar"];
            if (qab != null && qab.Value.Contains(requiredID))
                return true;

            var vb = item.Fields["Validate Button"];
            if (vb != null && vb.Value.Contains(requiredID))
                return true;

            var vb2 = item.Fields["Validator Bar"];
            if (vb2 != null && vb2.Value.Contains(requiredID))
                return true;

            var wf = item.Fields["Workflow"];
            if (wf != null && wf.Value.Contains(requiredID))
                return true;

            return false;
        }

        #endregion
    }
}