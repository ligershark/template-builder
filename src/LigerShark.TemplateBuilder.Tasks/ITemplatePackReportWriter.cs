using System;
namespace LigerShark.TemplateBuilder.Tasks {
    public interface ITemplatePackReportWriter {
        void WriteReport(System.Collections.Generic.IEnumerable<TemplatePackReportModel> reportItems, string filePath);
    }
}
