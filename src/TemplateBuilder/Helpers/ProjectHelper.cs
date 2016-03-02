namespace TemplateBuilder.Helpers
{
    using System.IO;
    using Microsoft.VisualStudio.Shell;

    internal static class ProjectHelper
    {
        public static void ReloadProject(global::EnvDTE.Project currentProject)
        {
            var dte2 = (global::EnvDTE80.DTE2)Package.GetGlobalService(typeof(global::EnvDTE.DTE));

            dte2.ExecuteCommand("File.SaveAll");

            string solutionName = Path.GetFileNameWithoutExtension(dte2.Solution.FullName);
            string projectName = currentProject.Name;

            dte2.Windows.Item(global::EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
            dte2.ToolWindows.SolutionExplorer
                .GetItem(solutionName + @"\" + projectName)
                .Select(global::EnvDTE.vsUISelectionType.vsUISelectionTypeSelect);

            dte2.ExecuteCommand("Project.UnloadProject");
            System.Threading.Thread.Sleep(500);
            dte2.ExecuteCommand("Project.ReloadProject");
        }
    }
}
