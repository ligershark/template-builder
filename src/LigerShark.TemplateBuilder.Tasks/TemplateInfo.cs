namespace LigerShark.TemplateBuilder.Tasks {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

    public class TemplateInfo {
        public TemplateInfo() {
            this.Replacements = new Dictionary<string, string>();
        }
        public string OverridePath { get; set; }
        public string Include { get; set; }
        public string Exclude { get; set; }
        public IDictionary<string,string> Replacements { get; set; }

        
        public static string SafeGetAttributeValue(XElement element, XName attributeName) {
            if (element == null || attributeName == null) {
                return string.Empty;
            }

            var attrib = element.Attribute(attributeName);

            return attrib != null ? attrib.Value : string.Empty;
        }
        public static TemplateInfo BuildTemplateInfoFrom(string filePath){            
            if (string.IsNullOrEmpty(filePath)) { throw new ArgumentNullException("filePath"); }
            if (!File.Exists(filePath)) { throw new FileNotFoundException("Template info file not found.", filePath); }

            XDocument doc = XDocument.Load(filePath);
            var result = (from r in doc.Root.Elements("Replacements")
                          select new {
                              // OverridePath = SafeGetAttributeValue(t,"Path"),
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
            
            // there is probably a better way to parse this file in one pass but this is OK for now
            var templateInfo = (from t in doc.Root.Elements("TemplateInfo")
                                select new {
                                    OverridePath = SafeGetAttributeValue(t, "Path")
                                }).SingleOrDefault();

            var tempInfo = new TemplateInfo {
                OverridePath = (templateInfo != null ? templateInfo.OverridePath : string.Empty),
                Include = result.Include,
                Exclude = result.Exclude
            };

            foreach (var r in result.Replacements) {
                tempInfo.Replacements[r.Key] = r.Value;
            }

            return tempInfo;
        }
    }
}
