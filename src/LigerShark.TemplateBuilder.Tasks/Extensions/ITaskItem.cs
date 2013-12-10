using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LigerShark.TemplateBuilder.Tasks.Extensions {
    public static class ITaskItemExtension {
        public static string GetFullPath(this ITaskItem item) {
            return item.GetMetadata("FullPath");
        }
    }
}
