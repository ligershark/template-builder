using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LigerShark.TemplateBuilder.Tasks {
    public class SnippetInfo {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public string Shortcut { get; set; }
        private static XNamespace ns = @"http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet";
        public static SnippetInfo BuildFromSnippetFile(string snippetFilePath) {
            if(!File.Exists(snippetFilePath)){
                throw new FileNotFoundException(".snippet file not found",snippetFilePath);
            }

            XDocument doc = XDocument.Load(snippetFilePath);
            // XNamespace ns = @"http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet";
            var result = (from e in doc.Root.Descendants(ns + "CodeSnippet")                         
                          select new SnippetInfo {
                              Path = snippetFilePath,
                              Description = GetDescriptionFromCodeSnippetElement(e),
                              Shortcut = GetShortcutFromCodeSnippetElement(e),
                              Title = GetTitleFromCodeSnippetElement(e)
                          }).SingleOrDefault();

            return result;
        }

        private static string GetDescriptionFromCodeSnippetElement(XElement codeSnippetElement) {
            if (codeSnippetElement == null) { throw new ArgumentNullException("codeSnippetElement"); }

            string result = string.Empty;

            try {
                result = codeSnippetElement.Element(ns + "Header").Element(ns + "Description").Value;
            }
            catch (Exception) {
                // TODO: log this error
                result = string.Empty;
            }                  

            return result;
        }
        private static string GetTitleFromCodeSnippetElement(XElement codeSnippetElement) {
            if (codeSnippetElement == null) { throw new ArgumentNullException("codeSnippetElement"); }

            string result = string.Empty;

            try {
                result = codeSnippetElement.Element(ns + "Header").Element(ns + "Title").Value;
            }
            catch (Exception) {
                // TODO: log this error
                result = string.Empty;
            }                  

            return result;
        }
        private static string GetShortcutFromCodeSnippetElement(XElement codeSnippetElement) {
            if (codeSnippetElement == null) { throw new ArgumentNullException("codeSnippetElement"); }

            string result = string.Empty;

            try {
                result = codeSnippetElement.Element(ns + "Header").Element(ns + "Shortcut").Value;
            }
            catch (Exception) {
                // TODO: log this error
                result = string.Empty;
            }

            return result;
        }
    }
}
