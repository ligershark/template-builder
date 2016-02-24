namespace TemplateBuilder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Construction;
    using Microsoft.Build.Evaluation;
    using Microsoft.VisualStudio.TemplateWizard;

    public class FixArtifactsPathWizard : IWizard
    {
        #region Public Methods

        public void BeforeOpeningFile(global::EnvDTE.ProjectItem projectItem)
        {
        }
        
        public void ProjectFinishedGenerating(global::EnvDTE.Project project)
        {
            var projectFilePath = project.FileName;
            var solutionFilePath = project.DTE.Solution.FileName;

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

            if (!string.Equals(Path.GetExtension(projectFilePath), ".xproj"))
            {
                return hasChanged;
            }

            var projectDirectoryPath = Path.GetDirectoryName(projectFilePath);
            var solutionDirectoryPath = string.IsNullOrEmpty(solutionFilePath) ? projectDirectoryPath : Path.GetDirectoryName(solutionFilePath);

            var artifactsObjDirectoryPath = Path.Combine(solutionDirectoryPath, @"artifacts\obj\$(MSBuildProjectName)");
            var artifactsBinDirectoryPath = Path.Combine(solutionDirectoryPath, @"artifacts\bin\$(MSBuildProjectName)");

            var relativeObjPackagesDirectoryPath = PathHelper.GetRelativePath(
                projectDirectoryPath,
                artifactsObjDirectoryPath)
                .TrimEnd('\\');
            var relativeBinPackagesDirectoryPath = PathHelper.GetRelativePath(
                projectDirectoryPath,
                artifactsBinDirectoryPath);

            //<PropertyGroup Label="Globals">
            //  <ProjectGuid>6e0ef33d-3c19-4ea2-8ca9-c7bf19bdd947</ProjectGuid>
            //  <RootNamespace>MvcBoilerplate</RootNamespace>
            //  <BaseIntermediateOutputPath Condition="'$(BaseIntermediateOutputPath)'=='' ">..\..\artifacts\obj\$(MSBuildProjectName)</BaseIntermediateOutputPath>
            //  <OutputPath Condition="'$(OutputPath)'=='' ">..\..\artifacts\bin\$(MSBuildProjectName)\</OutputPath>
            //</PropertyGroup>

            Project buildProject = new Project(projectFilePath);

            var baseIntermediateOutputPathElement = buildProject
                .Xml
                .PropertyGroups
                .FirstOrDefault(x => string.Equals(x.Label, "Globals"))?
                .Children
                .OfType<ProjectPropertyElement>()
                .FirstOrDefault(x => string.Equals(x.Name, "BaseIntermediateOutputPath"));
            if (baseIntermediateOutputPathElement != null &&
                !string.Equals(baseIntermediateOutputPathElement.Value, relativeObjPackagesDirectoryPath, StringComparison.Ordinal))
            {
                baseIntermediateOutputPathElement.Value = relativeObjPackagesDirectoryPath;
                hasChanged = true;
            }

            var outputPathElement = buildProject
                .Xml
                .PropertyGroups
                .FirstOrDefault(x => string.Equals(x.Label, "Globals"))?
                .Children
                .OfType<ProjectPropertyElement>()
                .FirstOrDefault(x => string.Equals(x.Name, "OutputPath"));
            if (outputPathElement != null &&
                !string.Equals(outputPathElement.Value, relativeBinPackagesDirectoryPath, StringComparison.Ordinal))
            {
                outputPathElement.Value = relativeBinPackagesDirectoryPath;
                hasChanged = true;
            }

            return hasChanged;
        }

        #endregion
    }
}
