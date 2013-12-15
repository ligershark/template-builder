using System;
using System.Collections.Generic;
namespace LigerShark.TemplateBuilder.Tasks {
    public interface ITemplatePackReportWriter {
        void WriteReport(string filePath, IEnumerable<TemplatePackReportModel> reportItems, IEnumerable<SnippetInfo>snippetItems);
    }
}
