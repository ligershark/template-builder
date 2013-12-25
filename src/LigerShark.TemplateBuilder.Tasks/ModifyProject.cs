using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LigerShark.TemplateBuilder.Tasks {
    public class ModifyProject :Task {
        [Required]
        public ITaskItem[] ItemsToRemove { get; set; }
        [Required]
        public string SourceProjectFilePath { get; set; }
        [Required]
        public string DestProjectFilePath { get; set; }
        public override bool Execute() {

            if (ItemsToRemove == null || ItemsToRemove.Length <= 0) {
                Log.LogMessage("ModifyProject called but ItemsToRemove is empty. Project file: [{0}]", SourceProjectFilePath);
                return true;
            }

            List<string> pathsLower = new List<string>(ItemsToRemove.Length);
            foreach (var item in ItemsToRemove) {
                pathsLower.Add(item.ItemSpec.ToLower());
            }
            List<ProjectItemElement> itemsToRemove = new List<ProjectItemElement>();
            // load up the project and then remove the specificed items
            var projFile = ProjectRootElement.Open(SourceProjectFilePath);
            // see if we can find an item in the list which matches what we have in our list
            foreach (var item in projFile.Items) {
                if (item == null || string.IsNullOrEmpty(item.Include)) {
                    continue;
                }

                if (pathsLower.Contains(item.Include.ToLower())) {
                    itemsToRemove.Add(item);
                }
            }

            foreach (var itemToRemove in itemsToRemove) {
                    if (itemToRemove != null) {
                        Log.LogMessage(
                            MessageImportance.Low, 
                            "Removing item [{0}] from project file [{1}], dest project [{2}]", 
                            itemToRemove, 
                            SourceProjectFilePath, 
                            DestProjectFilePath);

                        itemToRemove.Parent.RemoveChild(itemToRemove);
                    }
            }

            projFile.Save(DestProjectFilePath);

            return true;
        }
    }
}
