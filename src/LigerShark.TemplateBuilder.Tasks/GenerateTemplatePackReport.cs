using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using LigerShark.TemplateBuilder.Tasks.Extensions;
using System.IO;

namespace LigerShark.TemplateBuilder.Tasks {
    public class GenerateTemplatePackReport : Task {

        [Required]
        public ITaskItem[] TemplateFiles { get; set; }

        [Required]
        public ITaskItem OutputFile { get; set; }

        public override bool Execute() {
            Log.LogMessage("Generating template pack report to: [{0}]", OutputFile.GetFullPath());

            // the info that we want to show includes the following data
            // Name, Description, ProjetType, ProjectSubType?
            XNamespace ns = "http://schemas.microsoft.com/developer/vstemplate/2005";

            var allResults = from d in this.GetTemplateFilesAsDocs2()
                             from r in d.Document.Root.Descendants(ns + "TemplateData")
                             orderby r.ElementSafeValue(ns + "Name")
                             orderby r.ElementSafeValue(ns + "ProjectSubType")
                             orderby r.ElementSafeValue(ns + "ProjectType")
                             orderby d.TemplateType
                             select new {
                                 TemplateType = d.TemplateType,
                                 Name = r.ElementSafeValue(ns + "Name"),
                                 Description = r.ElementSafeValue(ns + "Description"),
                                 ProjectType = r.ElementSafeValue(ns + "ProjectType"),
                                 ProjectSubType = r.ElementSafeValue(ns + "ProjectSubType")
                             };


            Log.LogMessage("info.count [{0}]", allResults.Count());
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Template type\tType\tSubType\tName\tDescription");
            // get the report text now
            allResults.ToList().ForEach(infoItem => {
                sb.AppendFormat(
                    string.Format(
                        "{0}\t{1}\t{2}\t{3}\t{4}{5}",
                        infoItem.TemplateType,
                        infoItem.ProjectType,
                        infoItem.ProjectSubType,
                        infoItem.Name,
                        infoItem.Description,
                        Environment.NewLine,
                        string.Empty
                        ));
            });

            File.WriteAllText(OutputFile.GetFullPath(), sb.ToString());

            return true;
        }

        protected List<XDocument> GetTemplateFilesAsDocs() {
            List<XDocument> docs = new List<XDocument>();

            this.TemplateFiles.ToList().ForEach(template => {

                try {
                    var doc = XDocument.Load(template.GetFullPath());

                    docs.Add(doc);
                }
                catch (Exception ex) {
                    Log.LogWarning("Unable to read template file [{0}]", template.GetFullPath());
                    Log.LogWarning(ex.ToString());
                }
            });

            return docs;
        }

        protected List<TemplateDocument> GetTemplateFilesAsDocs2() {
            List<TemplateDocument> docs = new List<TemplateDocument>();

            this.TemplateFiles.ToList().ForEach(template => {
                docs.Add(new TemplateDocument(template.GetMetadata("TemplateType"), template.GetFullPath()));
            });

            return docs;
        }

        protected class TemplateDocument {
            public TemplateDocument(string templateType, string templatePath) {
                TemplateType = templateType;
                this.templatePath = templatePath;
            }

            private string templatePath { get; set; }
            public string TemplateType { get; set; }
            private XDocument document;
            public XDocument Document {
                get {
                    if (this.document == null && templatePath != null) {
                        this.document = XDocument.Load(this.templatePath);
                    }

                    return this.document;
                }
            }
        }
    }
}
