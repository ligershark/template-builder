using System;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

namespace TemplateBuilder {
    public class RootWizard : IWizard {
        // Use to communicate $saferootprojectname$ to ChildWizard     
        public static Dictionary<string, string> GlobalDictionary = new Dictionary<string, string>();

        // Add global replacement parameters     
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams) {
            // Place "$saferootprojectname$ in the global dictionary. 
            // Copy from $safeprojectname$ passed in my root vstemplate         
            GlobalDictionary["$saferootprojectname$"] = replacementsDictionary["$safeprojectname$"];
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
