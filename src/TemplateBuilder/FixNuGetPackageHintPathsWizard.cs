namespace TemplateBuilder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Evaluation;
    using Microsoft.VisualStudio.TemplateWizard;
    using NuGet;
    using TemplateBuilder.Helpers;

    public class FixNuGetPackageHintPathsWizard : IWizard
    {
        private const string PackagesSlash = @"packages\";
        private const string SlashPackagesSlash = @"\packages\";
        private const string Reference = "Reference";
        private const string HintPath = "HintPath";

        #region Public Methods

        public void BeforeOpeningFile(global::EnvDTE.ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(global::EnvDTE.Project project)
        {
            string projectFilePath = project.FileName;
            string solutionFilePath = project.CodeModel.DTE.Solution.FileName;

            if (Execute(solutionFilePath, projectFilePath))
            {
                project.Save();
            }
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

        #region Private Static Methods

        private static bool Execute(string solutionFilePath, string projectFilePath)
        {
            bool hasChanged = false;

            string projectDirectoryPath = Path.GetDirectoryName(projectFilePath);
            string solutionDirectoryPath = string.IsNullOrEmpty(solutionFilePath) ? projectDirectoryPath : Path.GetDirectoryName(solutionFilePath);
            string customPackagesDirectoryPath = GetCustomPackagesDirectoryPath(solutionDirectoryPath);

            string relativePackagesDirectoryPath = GetRelativePackagesDirectoryPath(
                projectDirectoryPath,
                solutionDirectoryPath,
                customPackagesDirectoryPath);
            
            Project buildProject = new Project(projectFilePath);

            foreach (ProjectMetadata metadata in buildProject.Items
                .Where(x => string.Equals(x.ItemType, Reference, StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.Metadata)
                .Where(x => string.Equals(x.Name, HintPath, StringComparison.OrdinalIgnoreCase) &&
                    (x.UnevaluatedValue.StartsWith(PackagesSlash) || x.UnevaluatedValue.Contains(SlashPackagesSlash))))
            {
                int startIndex;
                if (customPackagesDirectoryPath == null)
                {
                    startIndex = metadata.UnevaluatedValue.IndexOf(PackagesSlash);
                }
                else
                {
                    startIndex = metadata.UnevaluatedValue.IndexOf(PackagesSlash) + PackagesSlash.Length;
                }

                if (startIndex != -1)
                {
                    string newUnevaluatedValue = relativePackagesDirectoryPath + metadata.UnevaluatedValue.Substring(startIndex);
                    if (!string.Equals(metadata.UnevaluatedValue, newUnevaluatedValue, StringComparison.OrdinalIgnoreCase))
                    {
                        hasChanged = true;
                        metadata.UnevaluatedValue = newUnevaluatedValue;
                    }
                }
                else
                {
                    // If the project template author has used a nuget.config of their own, we are screwed. We don't 
                    // know which reference is a NuGet package reference as the folder could be named anything.
                    // So for safety we do nothing.
                }
            }
            
            return hasChanged;
        }

        private static string GetRelativePackagesDirectoryPath(
            string projectDirectoryPath, 
            string solutionDirectoryPath, 
            string customPackagesDirectoryPath)
        {
            string relativePackagesDirectoryPath;
            if (customPackagesDirectoryPath == null)
            {
                relativePackagesDirectoryPath = PathHelper.GetRelativePath(
                    projectDirectoryPath,
                    solutionDirectoryPath);
            }
            else
            {
                // Absolute Custom Packages Path
                if (Path.IsPathRooted(customPackagesDirectoryPath))
                {
                    return PathHelper.GetRelativePath(projectDirectoryPath, customPackagesDirectoryPath);
                }

                // Relative Custom Packages Path
                string path = Path.Combine(solutionDirectoryPath, customPackagesDirectoryPath);
                return PathHelper.GetRelativePath(projectDirectoryPath, path);
            }

            return relativePackagesDirectoryPath;
        }

        private static string GetCustomPackagesDirectoryPath(string projectDirectoryPath)
        {
            // Read the nuget.config file and use the repository path there instead.
            // This is actually very complicated. See https://docs.nuget.org/consume/nuget-config-file
            // <?xml version="1.0" encoding="utf-8"?>
            // <configuration>
            //   <config>
            //     <add key="repositorypath" value="c:\blah" />
            //   </config>
            // </configuration>

            string rootPath = Path.GetPathRoot(projectDirectoryPath);
            IFileSystem fileSystem = new PhysicalFileSystem(rootPath);
            var settings = Settings.LoadMachineWideSettings(fileSystem, projectDirectoryPath);
            return settings.Select(x => x.GetRepositoryPath()).Where(x => x != null).FirstOrDefault();
        }

        #endregion
    }
}
