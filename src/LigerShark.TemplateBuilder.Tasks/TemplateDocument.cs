using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LigerShark.TemplateBuilder.Tasks {
    public class TemplateDocument {
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
