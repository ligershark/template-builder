namespace FileReplacer {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class RobustReplacer {

        public void ReplaceInFiles(string rootDir, string include,string exclude,IDictionary<string,string>replacements,StringBuilder logger = null) {
            if (string.IsNullOrEmpty(rootDir)) { throw new ArgumentNullException("rootDir"); }
            if (!Directory.Exists(rootDir)) { throw new ArgumentException(string.Format("rootDir doesn't exist at [{0}]", rootDir)); }

            string rootDirFullPath = Path.GetFullPath(rootDir);

            // search for all include files
            List<string> pathsToInclude = new List<string>();
            List<string> pathsToExclude = new List<string>();

            if (!string.IsNullOrEmpty(include)) {
                string[] includeParts = include.Split(';');
                foreach (string includeStr in includeParts) {
                    var results = RobustReplacer.Search(rootDirFullPath, includeStr);
                    foreach (var result in results) {
                        if (!pathsToInclude.Contains(result)) {
                            pathsToInclude.Add(result);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(exclude)) {
                string[] excludeParts = exclude.Split(';');
                foreach (string excludeStr in excludeParts) {
                    var results = RobustReplacer.Search(rootDirFullPath, excludeStr);
                    foreach (var result in results) {
                        if (!pathsToExclude.Contains(result)) {
                            pathsToExclude.Add(result);
                        }
                    }
                }
            }

            int numFilesExcluded = pathsToInclude.RemoveAll(p => pathsToExclude.Contains(p));
            LogMessageLine(logger,"Number of files excluded based on pattern: [{0}]", numFilesExcluded);

            foreach (string file in pathsToInclude) {
                string fileFullPath = Path.GetFullPath(file);
                bool modified = false;

                using (var fileStream = File.Open(fileFullPath, FileMode.Open, FileAccess.ReadWrite)) {
                    using (var replacer = new TokenReplacer(fileStream)) {
                        foreach (string key in replacements.Keys) {
                            modified |= replacer.Replace(key, replacements[key]);
                        }
                    }

                    fileStream.Flush();
                }

                if (modified) {
                    LogMessageLine(logger,"Updating text after replacements in file [{0}]", fileFullPath);
                }
                else {
                    LogMessageLine(logger,"Not writing out file because no replacments detected [{0}]", fileFullPath);
                }
            }
        }

        protected void LogMessageLine(StringBuilder strBuilder, string message, params object[] args) {
            if (strBuilder != null) {
                strBuilder.AppendLine(string.Format(message,args));
            }
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
                catch(Exception) { } // swallow

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
                catch(Exception) { } // swallow

                if (paths != null && paths.Length > 0) {
                    foreach (string subDir in paths) {
                        dirs.Enqueue(subDir);
                    }
                }
            }
        }
    }
}
