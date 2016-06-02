namespace TemplateBuilder
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TemplateWizard;
    using TemplateBuilder.Helpers;

    public class ReloadProjectWizard : IWizard
    {
        #region Public Methods

        public void BeforeOpeningFile(global::EnvDTE.ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(global::EnvDTE.Project project)
        {
            ProjectHelper.ReloadProject(project);
        }

        public void ProjectItemFinishedGenerating(global::EnvDTE.ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
        }

        public void RunStarted(
            object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind,
            object[] customParams)
        {
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        #endregion
    }
}