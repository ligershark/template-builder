namespace LigerShark.TemplateBuilder.Tasks {
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class ReplaceInFiles : Task {
        [Required]
        public string RootDirectory { get; set; }
        [Required]
        public ITaskItem TemplateInfoFile { get; set; }

        public override bool Execute() {
            try {
                if (string.IsNullOrEmpty(RootDirectory)) {
                    Log.LogError("RootDirectory cannot be empty");
                    return false;
                }
                if (!Directory.Exists(this.RootDirectory)) {
                    Log.LogError("RootDirectory not found at [{0}]", this.RootDirectory);
                    return false;
                }

                string rootDirFullPath = Path.GetFullPath(this.RootDirectory);
                // parse the XML file
                TemplateInfo templateInfo = TemplateInfo.BuildTemplateInfoFrom(this.TemplateInfoFile.GetMetadata("FullPath"));

                Replacer replacer = new Replacer();
                StringBuilder logger = new StringBuilder();
                replacer.ReplaceInFiles(rootDirFullPath, templateInfo.Include, templateInfo.Exclude, templateInfo.Replacements, logger);
                Log.LogMessage(logger.ToString());

                // search for all include files
                //List<string> pathsToInclude = new List<string>();
                //List<string> pathsToExclude = new List<string>();

                //if (!string.IsNullOrEmpty(templateInfo.Include)) {
                //    string[] includeParts = templateInfo.Include.Split(';');
                //    foreach (string include in includeParts) {
                //        var results = ReplaceInFiles.Search(rootDirFullPath, include);
                //        foreach (var result in results) {
                //            if (!pathsToInclude.Contains(result)) {
                //                pathsToInclude.Add(result);
                //            }
                //        }
                //    }
                //}

                //if (!string.IsNullOrEmpty(templateInfo.Exclude)) {
                //    string[] excludeParts = templateInfo.Exclude.Split(';');
                //    foreach (string exclude in excludeParts) {
                //        var results = ReplaceInFiles.Search(rootDirFullPath, exclude);
                //        foreach (var result in results) {
                //            if (!pathsToExclude.Contains(result)) {
                //                pathsToExclude.Add(result);
                //            }
                //        }
                //    }
                //}

                //int numFilesExcluded = pathsToInclude.RemoveAll(p => pathsToExclude.Contains(p));
                //Log.LogMessage("Number of files excluded based on pattern: [{0}]", numFilesExcluded);

                //foreach (string file in pathsToInclude) {
                //    string fileFullPath = Path.GetFullPath(file);

                //    string originalFileText = File.ReadAllText(fileFullPath);
                //    string replacedText = originalFileText;

                //    foreach (string key in templateInfo.Replacements.Keys) {
                //        replacedText = Regex.Replace(replacedText, key, templateInfo.Replacements[key]);
                //    }

                //    if (!originalFileText.Equals(replacedText)) {
                //        Log.LogMessage("Updating text after replacements in file [{0}]", fileFullPath);
                //        File.WriteAllText(fileFullPath, replacedText);
                //    }
                //    else {
                //        Log.LogMessage("Not writing out file because no replacments detected [{0}]", fileFullPath);
                //    }
                //}
            }
            catch (Exception ex) {
                Log.LogError(ex.ToString());
                return false;
            }
            return true;
        }

        static IEnumerable<string> Search(string root, string searchPattern) {
            // taken from: http://stackoverflow.com/a/438316/105999
            Queue<string> dirs = new Queue<string>();
            dirs.Enqueue(root);
            while (dirs.Count > 0) {
                string dir = dirs.Dequeue();

                // files
                string[] paths = null;
                try {
                    paths = Directory.GetFiles(dir, searchPattern);
                }
                catch { } // swallow

                if (paths != null && paths.Length > 0) {
                    foreach (string file in paths) {
                        yield return file;
                    }
                }

                // sub-directories
                paths = null;
                try {
                    paths = Directory.GetDirectories(dir);
                }
                catch { } // swallow

                if (paths != null && paths.Length > 0) {
                    foreach (string subDir in paths) {
                        dirs.Enqueue(subDir);
                    }
                }
            }
        }
    }
}
