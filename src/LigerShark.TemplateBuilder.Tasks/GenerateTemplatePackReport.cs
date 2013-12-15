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
    public enum ReportType {
        Xml,
        Csv
    }
    public class GenerateTemplatePackReport : Task {
        public GenerateTemplatePackReport() {
            this.ReportType = Tasks.ReportType.Xml;
        }

        [Required]
        public ITaskItem[] TemplateFiles { get; set; }

        [Required]
        public ITaskItem OutputFile { get; set; }

        public ReportType ReportType { get; set; }

        public override bool Execute() {
            Log.LogMessage("Generating template pack report using format [{0}] to file: [{1}]",ReportType, OutputFile.GetFullPath());

            // the info that we want to show includes the following data
            // Name, Description, ProjetType, ProjectSubType?
            XNamespace ns = "http://schemas.microsoft.com/developer/vstemplate/2005";

            var allResults = from d in this.GetTemplateFilesAsDocs()
                             from r in d.Document.Root.Descendants(ns + "TemplateData")
                             orderby r.ElementSafeValue(ns + "Name")
                             orderby r.ElementSafeValue(ns + "ProjectSubType")
                             orderby r.ElementSafeValue(ns + "ProjectType")
                             orderby d.TemplateType
                             select new TemplatePackReportModel {
                                 TemplatePath = d.TemplatePath,
                                 TemplateType = d.TemplateType,
                                 Name = r.ElementSafeValue(ns + "Name"),
                                 Description = r.ElementSafeValue(ns + "Description"),
                                 ProjectType = r.ElementSafeValue(ns + "ProjectType"),
                                 ProjectSubType = r.ElementSafeValue(ns + "ProjectSubType")
                             };

            Log.LogMessage("info.count [{0}]", allResults.Count());
            ITemplatePackReportWriter reportWriter = null;
            if (ReportType == Tasks.ReportType.Csv) {
                reportWriter = new CsvTemplatePackReportWriter();
            }
            else if (ReportType == Tasks.ReportType.Xml) {
                reportWriter = new XmlTemplatePackReportWriter();
            }
            else {
                Log.LogError("Unknown value for ReportType [{0}]", ReportType);
                return false;
            }

            reportWriter.WriteReport(allResults, OutputFile.GetFullPath());

            return true;
        }

        protected List<TemplateDocument> GetTemplateFilesAsDocs() {
            List<TemplateDocument> docs = new List<TemplateDocument>();

            this.TemplateFiles.ToList().ForEach(template => {
                docs.Add(new TemplateDocument {
                    TemplateType = template.GetMetadata("TemplateType"),
                    TemplatePath = template.GetFullPath()
                });
            });

            return docs;
        }

        protected class TemplateDocument {            
            public string TemplatePath { get; set; }
            public string TemplateType { get; set; }
            private XDocument document;
            public XDocument Document {
                get {
                    if (this.document == null && TemplatePath != null) {
                        this.document = XDocument.Load(this.TemplatePath);
                    }

                    return this.document;
                }
            }
        }
    }
}
