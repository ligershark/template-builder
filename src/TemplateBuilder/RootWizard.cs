using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;
using EnvDTE100;
using EnvDTE80;
using Microsoft.VisualStudio.TemplateWizard;

namespace TemplateBuilder
{
    public class RootWizard : IWizard
    {
        // Core Solution object
        private Solution4 _solution;

        private IList<Project> _existingProjects;

        // Use to communicate $saferootprojectname$ to ChildWizard     
        public static Dictionary<string, string> GlobalDictionary = new Dictionary<string, string>();

        // Add global replacement parameters     
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            // Place "$saferootprojectname$ in the global dictionary. 
            // Copy from $safeprojectname$ passed in my root vstemplate  
            GlobalDictionary["$saferootprojectname$"] = replacementsDictionary["$safeprojectname$"];

            //Check if WizardRunKind equals AsMultiProject. 
            if (runKind != WizardRunKind.AsMultiProject) return;
            
            var dte2 = automationObject as DTE2;
            if (dte2 != null) _solution = (Solution4)dte2.Solution;

            //Get existing projects.
            _existingProjects = GetProjects() ?? new Project[0];
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        public void RunFinished()
        {
            if (_solution == null)
            {
                throw new Exception("No solution found.");
            }

            //Get all projects in solution
            var projects = GetProjects().Except(_existingProjects).ToList();
            if (projects == null || !projects.Any()) throw new Exception("No projects found.");

            //Get the projects directory from first project.
            var projectsDir = Path.GetDirectoryName(Path.GetDirectoryName(projects.First().FullName));
            if (projectsDir == null) return;

            //Change the projects location.
            var solutionStructure = projects.Select(AdjustProjectLocation).ToList();

            //Remove the projects from the solution
            foreach (var project in projects)
            {
                _solution.Remove(project);
            }

            //Restructure solution
            foreach (var keyValuePair in solutionStructure.Where(keyValuePair => !string.IsNullOrWhiteSpace(keyValuePair.Value)))
            {
                if (keyValuePair.Key != null)
                {
                    //If Key is not null, The project needs to be added to a SolutionFolder
                    var slnDirectory = keyValuePair.Key.Object as SolutionFolder;
                    if (slnDirectory != null)
                    {
                        slnDirectory.AddFromFile(keyValuePair.Value);
                    }
                }
                else
                {
                    //Key is null, add project to Solution root.
                    _solution.AddFromFile(keyValuePair.Value);
                }
            }

            ThreadPool.QueueUserWorkItem(state =>
            {
                //Wait for *.sln file and obsolete folder to be created.
                System.Threading.Thread.Sleep(4000);

                //Delete the old directory
                DeleteDirectory(projectsDir);
            });
        }

        public void BeforeOpeningFile(ProjectItem projectItem)
        {

        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {

        }

        public void ProjectFinishedGenerating(Project project)
        {
            
        }

        /// <summary>
        /// Copies the directory.
        /// </summary>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <param name="destDirectory">The destination directory.</param>
        private static void CopyDirectory(string sourceDirectory, string destDirectory)
        {
            if (!Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            var files = Directory.GetFiles(sourceDirectory);
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (name == null) continue;

                var dest = Path.Combine(destDirectory, name);
                File.Copy(file, dest);
            }

            var folders = Directory.GetDirectories(sourceDirectory);
            foreach (var folder in folders)
            {
                var name = Path.GetFileName(folder);
                if (name == null) continue;

                var dest = Path.Combine(destDirectory, name);
                CopyDirectory(folder, dest);
            }
        }

        /// <summary>
        /// Deletes the directory.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="inUseRetryCount">The in use retry count.</param>
        private static void DeleteDirectory(string directory, int inUseRetryCount = 3)
        {
            var counter = 0;
            while (counter < inUseRetryCount)
            {
                try
                {
                    if (!Directory.Exists(directory))
                    {
                        return;
                    }

                    var files = Directory.GetFiles(directory);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }

                    var folders = Directory.GetDirectories(directory);
                    foreach (var folder in folders)
                    {
                        var name = Path.GetFileName(folder);
                        if (name == null) continue;

                        DeleteDirectory(folder);
                    }

                    Directory.Delete(directory);
                    break;
                }
                catch (Exception)
                {
                    System.Threading.Thread.Sleep(2000);
                    counter++;
                }
            }
        }

        /// <summary>
        /// Adjusts the project location.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>The solution structure (key: optional SolutionFolder, value: *.csproj path</returns>
        private KeyValuePair<Project, string> AdjustProjectLocation(Project project)
        {
            var projectFullName = project.FullName;
            var projectName = Path.GetFileName(projectFullName);
            if (projectName == null) return new KeyValuePair<Project, string>();

            //Get project parent (this is either a SolutionFolder or solution root (null))
            var parent = project.ParentProjectItem.ContainingProject.Kind == ProjectKinds.vsProjectKindSolutionFolder ? project.ParentProjectItem.ContainingProject : null;

            //Get the old project directory
            var oldProjectDir = Path.GetDirectoryName(projectFullName);

            //Go to parent directory
            var solutionDir = Path.GetDirectoryName(Path.GetDirectoryName(oldProjectDir));
            if (solutionDir == null) return new KeyValuePair<Project, string>();

            //Create new project directory
            var newProjectDir = Path.Combine(solutionDir, Path.GetFileNameWithoutExtension(projectName));
            Directory.CreateDirectory(newProjectDir);

            //Move the project content to new directory
            CopyDirectory(oldProjectDir, newProjectDir);

            return new KeyValuePair<Project, string>(parent, Path.Combine(newProjectDir, projectName));
        }

        /// <summary>
        /// Gets the projects in a solution recursively.
        /// </summary>
        /// <returns></returns>
        private IList<Project> GetProjects()
        {
            var projects = _solution.Projects;
            var list = new List<Project>();
            var item = projects.GetEnumerator();

            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }
            return list;
        }

        /// <summary>
        /// Gets the solution folder projects.
        /// </summary>
        /// <param name="solutionFolder">The solution folder.</param>
        /// <returns></returns>
        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            var list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }
            return list;
        }
    }
}
