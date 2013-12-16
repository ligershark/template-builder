using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace LigerShark.TemplateBuilder.Tasks {
    public class CreateTemplateTask : Task {
        [Required]
        public string ProjectFile { get; set; }

        [Required]
        public string VsTemplateShell { get; set; }

        [Required]
        public string DestinationTemplateLocation { get; set; }

        public List<string> _filesToCopy { get; set; }

        [Output]
        public ITaskItem[] FilesToCopy { get; set; }

        private const string MSBuildSchema = "http://schemas.microsoft.com/developer/vstemplate/2005";

        public void RecurseItems(XElement projectItemContainer, string sourcePrefix, string targetPrefix, HashSet<string> takenSourceFileNames, HashSet<string> takenTargetFileNames) {
            foreach (var projectItem in projectItemContainer.Elements(XName.Get("ProjectItem", MSBuildSchema))) {
                takenSourceFileNames.Add(projectItem.Value.ToLower());
                takenTargetFileNames.Add(projectItem.Attribute(XName.Get("TargetFileName")).Value.ToLower());
            }

            foreach (var folder in projectItemContainer.Elements(XName.Get("Folder", MSBuildSchema))) {
                var sourceFolderName = folder.Attribute(XName.Get("Name")).Value.ToLower();
                var targetFolderName = folder.Attribute(XName.Get("TargetFolderName")).Value.ToLower();

                var sourceFolder = !string.IsNullOrWhiteSpace(sourcePrefix) ? Path.Combine(sourcePrefix, sourceFolderName) : sourceFolderName;
                var targetFolder = !string.IsNullOrWhiteSpace(targetPrefix) ? Path.Combine(targetPrefix, targetFolderName) : targetFolderName;

                RecurseItems(folder, sourceFolder, targetFolder, takenSourceFileNames, takenTargetFileNames);
            }
        }

        public void RecurseItems(XElement projectItemContainer, HashSet<string> takenSourceFileNames, HashSet<string> takenTargetFileNames) {
            RecurseItems(projectItemContainer, null, null, takenSourceFileNames, takenTargetFileNames);
        }

        public override bool Execute() {
            var vstemplate = XDocument.Load(VsTemplateShell);
            var workingTemplate = XDocument.Parse(@"<VSTemplate Version=""3.0.0"" xmlns=""http://schemas.microsoft.com/developer/vstemplate/2005"" Type=""Project"" />");

            if (vstemplate.Root == null || workingTemplate.Root == null) {
                return false;
            }

            var templateData = new XElement(XName.Get("TemplateData", MSBuildSchema));
            workingTemplate.Root.Add(templateData);
            MergeTemplateData(templateData, vstemplate.Root.Element(XName.Get("TemplateData", MSBuildSchema)));

            var project = new ProjectInstance(ProjectFile);
            var realProjectFile = Path.GetFileName(project.FullPath);

            if (realProjectFile == null) {
                return false;
            }

            var projectExtension = Path.GetExtension(project.FullPath);

            if (projectExtension == null) {
                return false;
            }

            var templateContentElement = vstemplate.Root.Element(XName.Get("TemplateContent", MSBuildSchema));
            XElement projectElement = null;

            if (templateContentElement != null) {
                var element = templateContentElement.Element(XName.Get("Project", MSBuildSchema));

                if (element != null) {
                    projectElement = XElement.Parse(element.ToString());
                }
            }

            templateContentElement = new XElement(XName.Get("TemplateContent", MSBuildSchema));
            templateContentElement.Add(projectElement);

            if (projectElement == null) {
                projectElement = new XElement(XName.Get("Project", MSBuildSchema));
                templateContentElement.Add(projectElement);
            }
            else {
                projectElement.RemoveAttributes();
            }

            projectElement.Add(new XAttribute(XName.Get("TargetFileName"), "$projectname$" + projectExtension));
            projectElement.Add(new XAttribute(XName.Get("File"), realProjectFile));
            projectElement.Add(new XAttribute(XName.Get("ReplaceParameters"), true));

            workingTemplate.Root.Add(templateContentElement);
            var sourceFileNames = new HashSet<string>();
            var targetFileNames = new HashSet<string>();

            RecurseItems(projectElement, sourceFileNames, targetFileNames);
            _filesToCopy = new List<string>();
            var itemsToMerge = new List<string>();

            foreach (var item in project.Items) {
                if (!IsPotentialSourceFile(item)) {
                    continue;
                }

                var name = item.EvaluatedInclude;
                var lowerName = name.ToLower();

                if (targetFileNames.Contains(lowerName)) {
                    continue;
                }

                if(item.ItemType != "Folder") {
                    _filesToCopy.Add(name);
                }

                if (!sourceFileNames.Contains(lowerName)) {
                    itemsToMerge.Add(name);
                }
            }

            Merge(projectElement, itemsToMerge);
            workingTemplate.Save(DestinationTemplateLocation);

            FilesToCopy = new ITaskItem[_filesToCopy.Count];
            for (int i = 0; i < FilesToCopy.Length; i++) {
                FilesToCopy[i] = new TaskItem(_filesToCopy[i]);
            }

            return true;
        }

        private static void MergeTemplateData(XElement target, XElement source, string childName, object defaultValue) {
            var element = source.Element(XName.Get(childName, MSBuildSchema));
            var value = defaultValue;

            if (element != null) {
                value = element.Value;
            }

            target.Add(new XElement(XName.Get(childName, MSBuildSchema), value));
        }

        private static void MergeTemplateData(XElement workingTemplate, XElement source) {
            source = source ?? new XElement("Foo");

            MergeTemplateData(workingTemplate, source, "Name", "My Project Template Name");
            MergeTemplateData(workingTemplate, source, "Description", "A description for this template");
            MergeTemplateData(workingTemplate, source, "DefaultName", "Project");
            MergeTemplateData(workingTemplate, source, "ProjectType", "CSharp");
            MergeTemplateData(workingTemplate, source, "SortOrder", 1000);
            MergeTemplateData(workingTemplate, source, "CreateNewFolder", true);
            MergeTemplateData(workingTemplate, source, "ProvideDefaultName", true);
            MergeTemplateData(workingTemplate, source, "LocationField", "Enabled");
            MergeTemplateData(workingTemplate, source, "EnableLocationBrowseButton", true);
            MergeTemplateData(workingTemplate, source, "Icon", "logo.png");
            MergeTemplateData(workingTemplate, source, "NumberOfParentCategoriesToRollUp", 1);
        }

        private static bool IsPotentialSourceFile(ProjectItemInstance item) {
            var nonFileTypes = new List<string>
            {
                "Reference",
                "AppConfigFileDestination",
                "IntermediateAssembly",
                "ApplicationManifest",
                "DeployManifest",
                "BuiltProjectOutputGroupKeyOutput",
                "DebugSymbolsProjectOutputGroupOutput",
                "AvailableItemName",
                "Clean",
                "XamlBuildTaskTypeGenerationExtensionName",
                "WebPublishExtnsionsToExcludeItem"
            };

            // if it ends with a / or \ assuem it points to a folder
            string include = item.EvaluatedInclude ?? string.Empty;

            return !item.ItemType.StartsWith("_")
                        && !nonFileTypes.Contains(item.ItemType)
                        && !include.EndsWith(@"/")
                        && !include.EndsWith(@"\");
        }

        private static void Merge(XElement rootElement, IEnumerable<string> filesToMerge) {
            var splitFiles = filesToMerge.OrderBy(x => x).Select(x => x.Split('\\'));

            foreach (var fileParts in splitFiles) {
                var workingElement = rootElement;

                for (var i = 0; i < fileParts.Length - 1; ++i) {
                    var newWorkingElement = workingElement.Elements(XName.Get("Folder", MSBuildSchema)).FirstOrDefault(x => string.Equals(x.Attribute(XName.Get("TargetFolderName")).Value, fileParts[i], StringComparison.OrdinalIgnoreCase));

                    if (newWorkingElement == null) {
                        newWorkingElement = new XElement(XName.Get("Folder", MSBuildSchema));
                        newWorkingElement.Add(new XAttribute(XName.Get("Name"), fileParts[i]));
                        newWorkingElement.Add(new XAttribute(XName.Get("TargetFolderName"), fileParts[i]));
                        workingElement.Add(newWorkingElement);
                        workingElement = newWorkingElement;
                    }
                    else {
                        workingElement = newWorkingElement;
                    }
                }

                if (!string.IsNullOrWhiteSpace(fileParts[fileParts.Length - 1])) {
                    var fileElement = new XElement(XName.Get("ProjectItem", MSBuildSchema), fileParts[fileParts.Length - 1]);
                    fileElement.Add(new XAttribute(XName.Get("ReplaceParameters"), true));
                    fileElement.Add(new XAttribute(XName.Get("TargetFileName"), fileParts[fileParts.Length - 1]));
                    workingElement.Add(fileElement);
                }
            }
        }
    }
}
