namespace LigerShark.TemplateBuilder.Tasks {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System.Net;

    public class GetItemTemplateNameFromVSTemplatePath : Microsoft.Build.Utilities.Task {

        public GetItemTemplateNameFromVSTemplatePath() {
            this.Success = true;
        }
        public bool Success { get; private set; }

        # region Inputs
        [Required]
        public string VstemplateFilePath { get; set; }
        [Required]
        public string ItemTemplateRoot { get; set; }

        public string ItemTemplateZipRootFolder { get; set; }

        public string CustomTemplatesFolder { get; set; }
        #endregion

        #region Outputs
        [Output]
        public string ItemTemplateName { get; set; }

        [Output]
        public string ItemTemplateFolder { get; set; }

        [Output]
        public string ZipfileName { get; set; }

        [Output]
        public string OutputPathFolder { get; set; }

        [Output]
        public string OutputPathWithFileName { get; set; }
        #endregion

        public override bool Execute() {
                Log.LogMessage("GetItemTemplateNameFromVSTemplatePath Starting");
                System.IO.FileInfo fi = new System.IO.FileInfo(VstemplateFilePath);
                System.IO.DirectoryInfo di = fi.Directory;

                var ItemTemplateFolderInfo = new System.IO.FileInfo(di.Parent.FullName);
                ItemTemplateFolder = ItemTemplateFolderInfo.FullName + @"\";

                // we need to get the name of the first folder under 'ItemTemplates' (ItemTemplateRoot)
                var itemTemplateRootUri = new Uri(ItemTemplateRoot);
                var relFolder = itemTemplateRootUri.MakeRelativeUri(new Uri(VstemplateFilePath)).ToString();
                var templateRelPath = relFolder.Substring(0, relFolder.IndexOf('/'));

                if (ItemTemplateZipRootFolder == null) {
                    ItemTemplateZipRootFolder = string.Empty;
                }

                string itRootFileName = di.Parent.Name;
                string subFolder = this.CustomTemplatesFolder;

                ZipfileName = string.Format(
                  "{0}{1}.zip",
                  ItemTemplateName,
                  fi.Name);
                // set OutputFolder
                // if the name is 
                //  'CSharp.vstemplate' -> CSharp\
                //  'Web.CSharp.vstemplate' -> CSharp\Web\
                //  'VB.vstemplate' -> VisualBasic\
                //  'Web.VB.vstemplate' -> VisualBasic\Web\
                //  'fsharp.vstemplate' -> FSharp\
                if (string.Compare(@"CSharp.vstemplate", fi.Name, StringComparison.OrdinalIgnoreCase) == 0) {
                    ItemTemplateName = string.Format("{0}.csharp", itRootFileName);
                    OutputPathFolder = string.Format(@"{0}CSharp\{1}\{2}", ItemTemplateZipRootFolder, templateRelPath, subFolder);
                }
                else if (string.Compare(@"Web.CSharp.vstemplate", fi.Name, StringComparison.OrdinalIgnoreCase) == 0) {
                    ItemTemplateName = string.Format("{0}.web.csharp", itRootFileName);

                    // web site templates do not support any nesting
                    OutputPathFolder = string.Format(@"{0}CSharp\Web\{1}", ItemTemplateZipRootFolder, subFolder);
                }
                else if (string.Compare(@"VB.vstemplate", fi.Name, StringComparison.OrdinalIgnoreCase) == 0) {
                    ItemTemplateName = string.Format("{0}.VB", itRootFileName);
                    OutputPathFolder = string.Format(@"{0}VisualBasic\{1}\{2}", ItemTemplateZipRootFolder, templateRelPath, subFolder);
                }
                else if (string.Compare(@"Web.VB.vstemplate", fi.Name, StringComparison.OrdinalIgnoreCase) == 0) {
                    ItemTemplateName = string.Format("{0}.web.VB", itRootFileName);

                    // web site templates do not support any nesting
                    OutputPathFolder = string.Format(@"{0}VisualBasic\Web\{1}", ItemTemplateZipRootFolder, subFolder);
                }
                else if (string.Compare(@"fsharp.vstemplate", fi.Name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ItemTemplateName = string.Format("{0}.fsharp", itRootFileName);
                    OutputPathFolder = string.Format(@"{0}FSharp\{1}\{2}", ItemTemplateZipRootFolder, templateRelPath, subFolder);
                }
                else {
                    Log.LogError("Unknown value for ItemTemplateName: [{0}]. Supported values include 'CSharp.vstemplate','Web.CSharp.vstemplate','VB.vstemplate' and 'Web.VB.vstemplate' ", fi.Name);
                    return false;
                }

                OutputPathWithFileName = string.Format(@"{0}{1}", OutputPathFolder, itRootFileName);                 

            return Success;
        }
    }
}