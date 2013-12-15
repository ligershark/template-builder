using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LigerShark.TemplateBuilder.Tasks {
    public class TextTemplatePackReportWriter : LigerShark.TemplateBuilder.Tasks.ITemplatePackReportWriter {
        public void WriteReport(string filePath, IEnumerable<TemplatePackReportModel> reportItems, IEnumerable<SnippetInfo> snippetItems) {
            if (reportItems == null) { throw new ArgumentNullException("reportItems"); }
            if (string.IsNullOrEmpty(filePath)) { throw new ArgumentNullException("filePath"); }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Template type\tType\tSubType\tName\tDescription");
            // get the report text now
            reportItems.ToList().ForEach(infoItem => {
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

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
