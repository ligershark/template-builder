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
        Text
    }
    public class GenerateTemplatePackReport : Task {
        public GenerateTemplatePackReport() {
            this.ReportType = Tasks.ReportType.Xml.ToString();
        }

        [Required]
        public ITaskItem[] TemplateFiles { get; set; }
        public ITaskItem[] SnippetFiles { get; set; }

        [Required]
        public ITaskItem OutputFile { get; set; }

        private ReportType _reportType;
        public string ReportType {
            get { return _reportType.ToString(); }
            set {
                ReportType result;
                if (Enum.TryParse<ReportType>(value, out result)) {
                    _reportType = result;
                }
                else {
                    Log.LogWarning("Unknown value for ReportType: [{0}]", value);
                }
            }
        }

        public override bool Execute() {
            Log.LogMessage("Generating template pack report using format [{0}] to file: [{1}]",ReportType, OutputFile.GetFullPath());

            if (SnippetFiles == null) { SnippetFiles = new ITaskItem[] { }; }

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

            IList<string> snippetFilePaths = new List<string>();
            SnippetFiles.ToList().ForEach(snippetItem => {
                snippetFilePaths.Add(snippetItem.GetFullPath());
            });

            var snippets = GetAllSnippetInfo(snippetFilePaths);

            Log.LogMessage("info.count [{0}]", allResults.Count());
            ITemplatePackReportWriter reportWriter = null;
            if (_reportType == Tasks.ReportType.Text) {
                reportWriter = new TextTemplatePackReportWriter();
            }
            else if (_reportType == Tasks.ReportType.Xml) {
                reportWriter = new XmlTemplatePackReportWriter();
            }
            else {
                Log.LogError("Unknown value for ReportType [{0}]", ReportType);
                return false;
            }

            reportWriter.WriteReport(OutputFile.GetFullPath(), allResults, snippets);

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

        /// <summary>
        /// This will read the .snippet files and create a SnippetInfo file from it
        /// </summary>
        /// <param name="snippetFilePaths"></param>
        /// <returns></returns>
        protected List<SnippetInfo> GetAllSnippetInfo(IList<string>snippetFilePaths) {
            List<SnippetInfo> siList = new List<SnippetInfo>();

            if (snippetFilePaths != null) {
                snippetFilePaths.ToList().ForEach(snippetFilePath => {
                    siList.Add(SnippetInfo.BuildFromSnippetFile(snippetFilePath));
                });
            }

            return siList;
        }
    }
}
