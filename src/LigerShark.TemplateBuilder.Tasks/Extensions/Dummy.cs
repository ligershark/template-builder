using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LigerShark.TemplateBuilder.Tasks.Extensions {
    // this is here just to ensure that the xdt assemblies are copied to the bin folder
    public class Dummy {
        public void DontRemove() {
            var result = Microsoft.Web.XmlTransform.TransformFlags.ApplyTransformToAllTargetNodes;

            throw new ApplicationException(string.Format("Don't call this method: [{0}]", result));
        }
    }
}
