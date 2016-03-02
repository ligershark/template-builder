namespace TemplateBuilder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Construction;
    using Microsoft.Build.Evaluation;
    using Microsoft.VisualStudio.TemplateWizard;
    using TemplateBuilder.Helpers;

    public class FixArtifactsPathWizard : IWizard
    {
        private const string BaseIntermediateOutputPathPropertyName = "BaseIntermediateOutputPath";
        private const string OutputPathPropertyName = "OutputPath";

        #region Public Methods

        public void BeforeOpeningFile(global::EnvDTE.ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(global::EnvDTE.Project project)
        {
            var projectFilePath = project.FileName;
            var solutionFilePath = project.DTE.Solution.FileName;

            if (!string.Equals(Path.GetExtension(projectFilePath), ".xproj"))
            {
                return;
            }

            var projectDirectoryPath = Path.GetDirectoryName(projectFilePath);
            var solutionDirectoryPath = string.IsNullOrEmpty(solutionFilePath) ? projectDirectoryPath : Path.GetDirectoryName(solutionFilePath);

            var artifactsObjDirectoryPath = Path.Combine(solutionDirectoryPath, @"artifacts\obj\$(MSBuildProjectName)");
            var artifactsBinDirectoryPath = Path.Combine(solutionDirectoryPath, @"artifacts\bin\$(MSBuildProjectName)");

            var relativeObjPackagesDirectoryPath = PathHelper.GetRelativePath(
                projectDirectoryPath,
                artifactsObjDirectoryPath);
            var relativeBinPackagesDirectoryPath = PathHelper.GetRelativePath(
                projectDirectoryPath,
                artifactsBinDirectoryPath);

            //<PropertyGroup Label="Globals">
            //  <ProjectGuid>6e0ef33d-3c19-4ea2-8ca9-c7bf19bdd947</ProjectGuid>
            //  <RootNamespace>MvcBoilerplate</RootNamespace>
            //  <BaseIntermediateOutputPath Condition="'$(BaseIntermediateOutputPath)'=='' ">..\..\artifacts\obj\$(MSBuildProjectName)</BaseIntermediateOutputPath>
            //  <OutputPath Condition="'$(OutputPath)'=='' ">..\..\artifacts\bin\$(MSBuildProjectName)\</OutputPath>
            //</PropertyGroup>

            bool hasChanged = false;
            Project buildProject = new Project(projectFilePath);

            var baseIntermediateOutputPathElement = buildProject
                .Xml
                .PropertyGroups
                .FirstOrDefault(x => string.Equals(x.Label, "Globals"))?
                .Children
                .OfType<ProjectPropertyElement>()
                .FirstOrDefault(x => string.Equals(x.Name, BaseIntermediateOutputPathPropertyName));
            if (baseIntermediateOutputPathElement != null &&
                !string.Equals(baseIntermediateOutputPathElement.Value, relativeObjPackagesDirectoryPath.TrimEnd('\\'), StringComparison.Ordinal))
            {
                baseIntermediateOutputPathElement.Value = relativeObjPackagesDirectoryPath.TrimEnd('\\');
                hasChanged = true;
            }

            var outputPathElement = buildProject
                .Xml
                .PropertyGroups
                .FirstOrDefault(x => string.Equals(x.Label, "Globals"))?
                .Children
                .OfType<ProjectPropertyElement>()
                .FirstOrDefault(x => string.Equals(x.Name, OutputPathPropertyName));
            if (outputPathElement != null &&
                !string.Equals(outputPathElement.Value, relativeBinPackagesDirectoryPath, StringComparison.Ordinal))
            {
                outputPathElement.Value = relativeBinPackagesDirectoryPath;
                hasChanged = true;
            }

            if (hasChanged)
            {
                buildProject.Save();
                ProjectHelper.ReloadProject(project);
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
    }
}