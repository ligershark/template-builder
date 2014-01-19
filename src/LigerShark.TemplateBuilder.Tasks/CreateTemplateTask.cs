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
        
        public string ProjectFile { get; set; }

        [Required]
        public string VsTemplateShell { get; set; }

        [Required]
        public string DestinationTemplateLocation { get; set; }

        public ITaskItem[] FilesExclude { get; set; }

        public List<string> _filesToCopy { get; set; }

        public ITaskItem[] NonFileTypes { get; set; }

        public bool UpdateProjectElement { get; set; }
        [Output]
        public ITaskItem[] FilesToCopy { get; set; }

        private const string VsTemplateSchema = "http://schemas.microsoft.com/developer/vstemplate/2005";

        private List<string> DefaultNonFileTypesList;
        public CreateTemplateTask() {
            this.DefaultNonFileTypesList = new List<string> {
                "Reference",
                "ProjectReference",
                "AppConfigFileDestination",
                "IntermediateAssembly",
                "ApplicationManifest",
                "DeployManifest",
                "BuiltProjectOutputGroupKeyOutput",
                "DebugSymbolsProjectOutputGroupOutput",
                "AvailableItemName",
                "Clean",
                "XamlBuildTaskTypeGenerationExtensionName",
                "WebPublishExtnsionsToExcludeItem",
                "COMReference",
                "DocumentationProjectOutputGroupOutput"
            };

            UpdateProjectElement = true;
        }

        public void RecurseItems(XElement projectItemContainer, string sourcePrefix, string targetPrefix, HashSet<string> takenSourceFileNames, HashSet<string> takenTargetFileNames) {
            foreach (var projectItem in projectItemContainer.Elements(XName.Get("ProjectItem", VsTemplateSchema))) {
                takenSourceFileNames.Add(projectItem.Value.ToLower());
                takenTargetFileNames.Add(projectItem.Attribute(XName.Get("TargetFileName")).Value.ToLower());
            }

            foreach (var folder in projectItemContainer.Elements(XName.Get("Folder", VsTemplateSchema))) {
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

        private string GetProjectFile(XDocument vstemplate) {
            string result = ProjectFile;

            if (string.IsNullOrEmpty(result) && vstemplate != null) {
                XNamespace ns = @"http://schemas.microsoft.com/developer/vstemplate/2005";
                string projfilename = vstemplate.Root.Element(ns + "TemplateContent").Element(ns + "Project").Attribute("File").Value;
                // assume that this is a relative path to the .vstemplate file passed in

                FileInfo vsTemplateFi = new FileInfo(VsTemplateShell);
                result = System.IO.Path.Combine(vsTemplateFi.Directory.FullName, projfilename);
            }

            return result;            
        }
        public override bool Execute() {
            var vstemplate = XDocument.Load(VsTemplateShell);
            var workingTemplate = XDocument.Parse(@"<VSTemplate Version=""3.0.0"" xmlns=""http://schemas.microsoft.com/developer/vstemplate/2005"" Type=""Project"" />");

            if (vstemplate.Root == null || workingTemplate.Root == null) {
                return false;
            }

            var templateData = new XElement(XName.Get("TemplateData", VsTemplateSchema));
            workingTemplate.Root.Add(templateData);
            MergeTemplateData(templateData, vstemplate.Root.Element(XName.Get("TemplateData", VsTemplateSchema)));

            var project = new ProjectInstance(GetProjectFile(vstemplate));
            var realProjectFile = Path.GetFileName(project.FullPath);

            if (realProjectFile == null) {
                return false;
            }

            var projectExtension = Path.GetExtension(project.FullPath);

            if (projectExtension == null) {
                return false;
            }

            var templateContentElement = vstemplate.Root.Element(XName.Get("TemplateContent", VsTemplateSchema));
            XElement projectElement = null;

            if (templateContentElement != null) {
                var element = templateContentElement.Element(XName.Get("Project", VsTemplateSchema));

                if (element != null) {
                    projectElement = XElement.Parse(element.ToString());
                }
            }

            templateContentElement = new XElement(XName.Get("TemplateContent", VsTemplateSchema));
            templateContentElement.Add(projectElement);

            if (projectElement == null) {
                projectElement = new XElement(XName.Get("Project", VsTemplateSchema));
                templateContentElement.Add(projectElement);
            }
            //else {
            //    projectElement.RemoveAttributes();
            //}
            // instead of calling RemoveAttribute and then Add we can just call SetAttributeValue below so that we don't
            // remove any attributes we are not aware of


            if (UpdateProjectElement) {                
                projectElement.SetAttributeValue(XName.Get("TargetFileName"), "$safeprojectname$" + projectExtension);
                projectElement.SetAttributeValue(XName.Get("File"), realProjectFile);
                projectElement.SetAttributeValue(XName.Get("ReplaceParameters"), true);
            }

            // projectElement.Add(new XAttribute(XName.Get("TargetFileName"), "$safeprojectname$" + projectExtension));
            // projectElement.Add(new XAttribute(XName.Get("File"), realProjectFile));
            // projectElement.Add(new XAttribute(XName.Get("ReplaceParameters"), true));

            workingTemplate.Root.Add(templateContentElement);
            var sourceFileNames = new HashSet<string>();
            var targetFileNames = new HashSet<string>();

            List<string> filesToExclude = GetFilesToExlucudeAsList();
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

                if (filesToExclude.Contains(lowerName)) {
                    continue;
                }

                if(item.ItemType != "Folder") {
                    _filesToCopy.Add(name);
                }

                if (!sourceFileNames.Contains(lowerName)) {
                    itemsToMerge.Add(name);
                }

            }

            //Copy all non-mutated sections
            var mutatedTemplateSections = new[] {"TemplateContent", "TemplateData"};
            var elementsToCopyDirectly = vstemplate.Root.Elements().Where(x => !mutatedTemplateSections.Contains(x.Name.LocalName));

            foreach (var element in elementsToCopyDirectly) {
                var clonedElement = XElement.Parse(element.ToString());
                workingTemplate.Root.Add(clonedElement);
                // workingTemplate.Add(clonedElement);
            }

            Merge(projectElement, itemsToMerge);
            workingTemplate.Save(DestinationTemplateLocation);

            FilesToCopy = new ITaskItem[_filesToCopy.Count];
            for (int i = 0; i < FilesToCopy.Length; i++) {
                FilesToCopy[i] = new TaskItem(_filesToCopy[i]);
            }

            return true;
        }
        private List<string> GetFilesToExlucudeAsList() {
            List<string> filesToExclude = new List<string>();

            if (FilesExclude != null) {
                foreach (var item in FilesExclude) {
                    if (item == null || string.IsNullOrEmpty(item.ItemSpec)) {
                        continue;
                    }
                    filesToExclude.Add(item.ItemSpec.ToLower());
                }
            }

            return filesToExclude;
        }
        private static void MergeTemplateData(XElement target, XElement source, string childName, object defaultValue) {
            var element = source.Element(XName.Get(childName, VsTemplateSchema));
            var value = defaultValue;

            if (element != null) {
                value = element.Value;
            }

            target.Add(new XElement(XName.Get(childName, VsTemplateSchema), value));
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

        private bool IsPotentialSourceFile(ProjectItemInstance item) {
            var nonFileTypes = GetNonFileTypesList();

            // TODO: we need to change the logic here. By using project.Items we are
            //  getting a lot more than just source files. For example in some cases ls-BuildAssemblies comes thru

            // if it ends with a / or \ assume it points to a folder
            string include = item.EvaluatedInclude ?? string.Empty;

            return !item.ItemType.StartsWith("_")
                        && !item.ItemType.StartsWith("ls-")
                        && !nonFileTypes.Contains(item.ItemType)
                        && !include.EndsWith(@"/")
                        && !include.EndsWith(@"\");
        }

        private static void Merge(XElement rootElement, IEnumerable<string> filesToMerge) {
            var splitFiles = filesToMerge.OrderBy(x => x).Select(x => x.Split('\\'));

            foreach (var fileParts in splitFiles) {
                var workingElement = rootElement;

                for (var i = 0; i < fileParts.Length - 1; ++i) {
                    var newWorkingElement = workingElement.Elements(XName.Get("Folder", VsTemplateSchema)).FirstOrDefault(x => string.Equals(x.Attribute(XName.Get("TargetFolderName")).Value, fileParts[i], StringComparison.OrdinalIgnoreCase));

                    if (newWorkingElement == null) {
                        newWorkingElement = new XElement(XName.Get("Folder", VsTemplateSchema));
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
                    var fileElement = new XElement(XName.Get("ProjectItem", VsTemplateSchema), fileParts[fileParts.Length - 1]);
                    fileElement.Add(new XAttribute(XName.Get("ReplaceParameters"), true));
                    fileElement.Add(new XAttribute(XName.Get("TargetFileName"), fileParts[fileParts.Length - 1]));
                    workingElement.Add(fileElement);
                }
            }
        }

        private IList<string> GetNonFileTypesList() {
            List<string> nonFileTypesList = new List<string>();

            if (this.NonFileTypes != null) {
                this.NonFileTypes.ToList().ForEach(item => {
                    string spec = item.ItemSpec;
                    if (!string.IsNullOrEmpty(spec)) {
                        string cleanedUpSpec = spec.Trim().TrimStart(';').TrimEnd(';');
                        nonFileTypesList.Add(cleanedUpSpec);
                    }
                });
            }
            else {
                nonFileTypesList.AddRange(DefaultNonFileTypesList);
            }

            return nonFileTypesList;
        }
    }
}
