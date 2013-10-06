namespace LigerShark.TemplateBuilder.Tasks {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System.IO;
    using System.Net;
    using Microsoft.Build.Utilities;
    using Microsoft.Build.Framework;
    using System.Xml.Linq;

    //<?xml version="1.0" encoding="utf-8" ?>
    //<TemplateInfo>
    //  <Replacements Include="**\*" Exclude="**\*.png;**\*.jpg;">
    //    <add key="SideWaffleProjectName" value="$safeprojectname$"/>
    //    <add key="CustomName" value="SomeOtherString"/>
    //  </Replacements>
    //</TemplateInfo>

    public class ParseTemplateInfoFile : Task {
        public bool Success { get; set; }

        [Required]
        public string FilePath { get; set; }
        public string ReplacementsItemSpec { get; set; }

        [Output]
        public string Include { get; set; }
        [Output]
        public string Exclude { get; set; }
        [Output]
        public ITaskItem[] Replacements { get; set; }

        public override bool Execute() {
            Log.LogMessage("ParseTemplateInfoFile called on file [{0}]", FilePath);

            if (!File.Exists(FilePath)) {
                Log.LogError("templateinfo.xml file not found at [{0}]",FilePath);
                return false;
            }
            if(string.IsNullOrEmpty(ReplacementsItemSpec)){
                ReplacementsItemSpec = "Replacements";
            }

            try {
                XDocument doc = XDocument.Load(FilePath);
                var result = (from r in doc.Root.Elements("Replacements")
                             select new {
                                 Include = r.Attribute("Include").Value,
                                 Exclude = r.Attribute("Exclude").Value,
                                 Replacements = (
                                     from a in r.Elements("add")
                                     select new {
                                         Key = a.Attribute("key").Value,
                                         Value = a.Attribute("value").Value
                                     }
                                 )
                             }).Single();

                // we have to prefix include and exclude with $(_TemplateDir)               
                //if (result.Include != null) {
                //    string[] includeParts = result.Include.Split(';');
                //    StringBuilder includeBuilder = new StringBuilder();
                //    foreach (var str in includeParts) {
                //        includeBuilder.AppendFormat("{0}{1};", @"$(_TemplateDir)", str);
                //    }
                //    this.Include = includeBuilder.ToString();
                //}

                //if (result.Exclude != null) {
                //    string[] excludeParts = result.Exclude.Split(';');
                //    StringBuilder excludeBuilder = new StringBuilder();
                //    foreach (var str in excludeParts) {
                //        excludeBuilder.AppendFormat("{0}{1};", @"$(_TemplateDir)", str);
                //    }
                //    this.Exclude = excludeBuilder.ToString();
                //}

                this.Include = result.Include;
                this.Exclude = result.Exclude;

                List<ITaskItem> repList = new List<ITaskItem>();
                
                foreach(var item in result.Replacements){
                    ITaskItem rep = new TaskItem(ReplacementsItemSpec);
                    rep.SetMetadata(item.Key, item.Value);
                    repList.Add(rep);
                }

                Replacements = repList.ToArray();

            }
            catch (Exception ex) {
                Log.LogError("Unable to parse template info file [{0}]. Details=[{1}]",FilePath,ex.ToString());
                return false;
            }
            return true;
            return Success;
        }
    }
}
