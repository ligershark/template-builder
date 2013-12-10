using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LigerShark.TemplateBuilder.Tasks.Extensions {
    public static class XElementExtensions {
        public static string ElementSafeValue(this XElement element, XName name) {
            var e = element.Element(name);

            return e != null ? e.Value : null;
        }
    }
}
