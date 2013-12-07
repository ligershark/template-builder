using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

namespace TemplateBuilder {
    public class ChildWizard : IWizard {

        // Add global replacement parameters     
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams) {
            // Add custom parameters.         
            replacementsDictionary.Add("$saferootprojectname$", RootWizard.GlobalDictionary["$saferootprojectname$"]);
        }


        public bool ShouldAddProjectItem(string filePath) {
            return true;
        }

        public void RunFinished() {
        }

        public void BeforeOpeningFile(ProjectItem projectItem) {
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem) {

        }

        public void ProjectFinishedGenerating(Project project) {

        }
    }
}
