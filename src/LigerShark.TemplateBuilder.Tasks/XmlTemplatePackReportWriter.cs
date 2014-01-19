using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LigerShark.TemplateBuilder.Tasks {
    public class XmlTemplatePackReportWriter : ITemplatePackReportWriter {
        public void WriteReport(string filePath, IEnumerable<TemplatePackReportModel> reportItems, IEnumerable<SnippetInfo> snippetItems) {
            if (reportItems == null) { throw new ArgumentNullException("reportItems"); }
            if (string.IsNullOrEmpty(filePath)) { throw new ArgumentNullException("filePath"); }
        //<?xml version="1.0" encoding="utf-8"?>
        //<TemplateReport>
        //    <ItemTemplates>
        //        <ItemTemplate Path="\rel\path" Name="name" Description="description" ProjectType="project type" ProjectSubType="project sub type"/>
        //        <ItemTemplate Path="\rel\path" Name="name" Description="description" ProjectType="project type" ProjectSubType="project sub type"/>
        //    </ItemTemplates>

        //    <ProjectTemplates>
        //        <ProjectTemplate Path="\rel\path" Name="name" Description="description" ProjectType="project type" ProjectSubType="project sub type"/>
        //        <ProjectTemplate Path="\rel\path" Name="name" Description="description" ProjectType="project type" ProjectSubType="project sub type"/>
        //    </ProjectTemplates>

        //    <Snippets>
        //        <Snippet Title="title here" Description="description" Shortcut="shortcut" Path="\rel\path"/>
        //        <Snippet Title="title here" Description="description" Shortcut="shortcut" Path="\rel\path"/>
        //        <Snippet Title="title here" Description="description" Shortcut="shortcut" Path="\rel\path"/>
        //    </Snippets>
        //</TemplateReport>
            try {
                var result =
                    new XElement("TemplateReport",
                        new XElement("ItemTemplates",
                            from it in reportItems
                            where string.Compare("ItemTemplate", it.TemplateType, StringComparison.OrdinalIgnoreCase) == 0
                            select new XElement("ItemTemplate",
                                new XAttribute("Path", GetRelativePathForTemplatePath(filePath,it.TemplatePath)),
                                new XAttribute("Name", it.Name ?? string.Empty),
                                new XAttribute("Description", it.Description ?? string.Empty),
                                new XAttribute("ProjectType", it.ProjectType ?? string.Empty),
                                new XAttribute("ProjectSubType", it.ProjectSubType ?? string.Empty))
                            ),
                        new XElement("ProjectTemplates",
                            from pt in reportItems
                            where string.Compare("ProjectTemplate", pt.TemplateType, StringComparison.OrdinalIgnoreCase) == 0
                            select new XElement("ProjectTemplate",
                                new XAttribute("Path", GetRelativePathForTemplatePath(filePath,pt.TemplatePath)),
                                new XAttribute("Name", pt.Name ?? string.Empty),
                                new XAttribute("Description", pt.Description ?? string.Empty),
                                new XAttribute("ProjectType", pt.ProjectType ?? string.Empty),
                                new XAttribute("ProjectSubType", pt.ProjectSubType ?? string.Empty))
                            ),
                        new XElement("Snippets",
                            from sn in snippetItems
                            select new XElement("Snippet",
                                new XAttribute("Title",sn.Title??string.Empty),
                                new XAttribute("Description",sn.Description??string.Empty),
                                new XAttribute("Shortcut",sn.Shortcut??string.Empty),
                                new XAttribute("Path",GetRelativePathForTemplatePath(filePath,sn.Path))))
                                );

                result.Save(filePath);
            }
            catch (Exception ex) {
                string msg = ex.ToString();
                throw;
            }
        }

        private string GetRelativePathForTemplatePath(string reportPath, string templatePath) {
            if (string.IsNullOrEmpty(reportPath)) { throw new ArgumentNullException("reportPath"); }
            
            // since this will be called from a linq expression we will avoid returning null
            // and instead return string.Empty
            if (string.IsNullOrEmpty(templatePath)) {
                return string.Empty;
            }

            FileInfo reportPathFileInfo = new FileInfo(reportPath);

            // compute the relative path
            // http://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
            Uri reportFolderUri = new Uri(reportPathFileInfo.Directory.FullName + @"/");
            Uri templatePathUri = new Uri(templatePath);

            string relPath = Uri.UnescapeDataString(
                reportFolderUri.MakeRelativeUri(templatePathUri)
                .ToString()
                .Replace('/', Path.DirectorySeparatorChar)
                );

            return relPath;
        }
    }
}
