namespace FileReplacer {
    using System;
    using System.Collections.Generic;
    using System.Text;

    public interface IReplacer {
        void ReplaceInFiles(string rootDir, string include, string exclude, IDictionary<string, string> replacements, StringBuilder logger = null);
    }
}
