namespace InlineTaskHelper {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System.IO;
    using System.Net;

    public class ReplaceInFiles : Microsoft.Build.Utilities.Task {
        public bool Success { get; set; }

        public Microsoft.Build.Framework.ITaskItem[] FilesToReplace { get; set; }
        public Microsoft.Build.Framework.ITaskItem[] Replacements { get; set; }

        [Microsoft.Build.Framework.Output]
        public Microsoft.Build.Framework.ITaskItem[] UpdatedFiles { get; set; }
        public override bool Execute() {

            if (FilesToReplace == null || FilesToReplace.Length <= 0 || 
                Replacements == null || Replacements.Length <=0 ) {
                return Success;
            }           

            List<Microsoft.Build.Framework.ITaskItem> updatedFileList = new List<Microsoft.Build.Framework.ITaskItem>();
            // build up a dictionary of the replacements to minimize traversing
            Dictionary<string, string> replacements = new Dictionary<string, string>();            
            foreach (var replacement in Replacements) {
                IDictionary customMetadata = replacement.CloneCustomMetadata();
                foreach(var key in customMetadata.Keys){
                    replacements[key as string] = customMetadata[key] as string;
                }
            }

            foreach (var item in FilesToReplace) {             
                string filePath = item.GetMetadata("FullPath");
                string originalFileText = File.ReadAllText(filePath);
                string replacedText = originalFileText;

                foreach (string key in replacements.Keys) {
                    replacedText = System.Text.RegularExpressions.Regex.Replace(replacedText, key, replacements[key]);
                }

                if (!originalFileText.Equals(replacedText)) {
                    Log.LogMessage("Updating text after replacements in file [{0}]", filePath);
                    File.WriteAllText(filePath, replacedText);
                }
                else {
                    Log.LogMessage("Not writing out file because no replacments detected [{0}]", filePath);
                }
            }


            this.UpdatedFiles = updatedFileList.ToArray();
            return Success;
        }
    }
}
