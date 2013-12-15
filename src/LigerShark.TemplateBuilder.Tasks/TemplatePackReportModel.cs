using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LigerShark.TemplateBuilder.Tasks {
    public class TemplatePackReportModel {
        public string TemplatePath { get; set; }
        public string TemplateType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProjectType { get; set; }
        public string ProjectSubType { get; set; }

        //TemplateType = d.TemplateType,
        //                         Name = r.ElementSafeValue(ns + "Name"),
        //                         Description = r.ElementSafeValue(ns + "Description"),
        //                         ProjectType = r.ElementSafeValue(ns + "ProjectType"),
        //                         ProjectSubType = r.ElementSafeValue(ns + "ProjectSubType")
    }
}
