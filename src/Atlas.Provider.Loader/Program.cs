using System.Text.Json;
using Atlas.Provider.Tools;

namespace Atlas.Provider.Loader
{
    static class Program
    {
        /// <summary>
        /// This class used to load the current context into Core Commands
        /// </summary>
        /// <param name="project">Relative path to the project folder of the target project. Default value is the current folder.</param>
        /// <param name="startupProject">Relative path to the project folder of the startup project. Default value is the current folder.</param>
        /// <param name="framework">The Target Framework Moniker for the target framework. Use when the project file specifies multiple target frameworks, and you want to select one of them.</param>
        /// <param name="context">The DbContext class to use. Class name only or fully qualified with namespaces. 
        /// If this option is omitted, EF Core will find the context class. If there are multiple context classes, this option is required.</param>
        /// <param name="noBuild">Don't build the project. Intended to be used when the build is up-to-date.</param>
        /// <param name="args"></param>
        static void Main(
          string project,
          string startupProject,
          string framework,
          string context,
          bool noBuild = false,
          string[]? args = null
        )
        {
            if (string.IsNullOrEmpty(startupProject))
            {
                startupProject = project;
            }

            var (projectFile, startupProjectFile) = ResolveProjects(project, startupProject);

            var _project = Project.FromFile(projectFile);
            var _startupProject = Project.FromFile(startupProjectFile);

            if (!noBuild)
            {
                _startupProject.Build();
            }

            var targetDir = Path.GetFullPath(Path.Combine(_startupProject.ProjectDir!, _startupProject.OutputPath!));
            var targetPath = Path.Combine(targetDir, _project.TargetFileName!);
            var startupTargetPath = Path.Combine(targetDir, _startupProject.TargetFileName!);
            var depsFile = Path.Combine(
                targetDir,
                _startupProject.AssemblyName + ".deps.json");
            var runtimeConfig = Path.Combine(
                targetDir,
                _startupProject.AssemblyName + ".runtimeconfig.json");
            var projectAssetsFile = _startupProject.ProjectAssetsFile;
            var libDepsFile = Path.Combine(
               Path.GetDirectoryName(typeof(Program).Assembly.Location)!,
                "Atlas.Provider.Core.deps.json");

            string executable = "dotnet";
            var opts = new List<string>
            {
                "exec",
                "--depsfile",
                depsFile,
                "--additional-deps",
                libDepsFile
            };

            if (!string.IsNullOrEmpty(projectAssetsFile))
            {
                using var file = File.OpenRead(projectAssetsFile);
                using var reader = JsonDocument.Parse(file);
                var projectAssets = reader.RootElement;
                var packageFolders = projectAssets.GetProperty("packageFolders").EnumerateObject().Select(p => p.Name);

                foreach (var packageFolder in packageFolders)
                {
                    opts.Add("--additionalprobingpath");
                    opts.Add(packageFolder.TrimEnd(Path.DirectorySeparatorChar));
                }
            }

            if (File.Exists(runtimeConfig))
            {
                opts.Add("--runtimeconfig");
                opts.Add(runtimeConfig);
            }
            else if (_startupProject.RuntimeFrameworkVersion!.Length != 0)
            {
                opts.Add("--fx-version");
                opts.Add(_startupProject.RuntimeFrameworkVersion);
            }

            opts.Add(Path.GetDirectoryName(typeof(Program).Assembly.Location) + "/Atlas.Provider.Core.dll");

            if (args != null)
            {
                opts.AddRange(args);
            }
            
            opts.Add("--assembly");
            opts.Add(targetPath);
            opts.Add("--project");
            opts.Add(projectFile);
            opts.Add("--startup-assembly");
            opts.Add(startupTargetPath);
            opts.Add("--startup-project");
            opts.Add(startupProjectFile);
            opts.Add("--project-dir");
            opts.Add(_project.ProjectDir!);
            opts.Add("--root-namespace");
            opts.Add(_project.RootNamespace!);
            opts.Add("--language");
            opts.Add(_project.Language!);
            opts.Add("--framework");
            opts.Add(_startupProject.TargetFramework!);

            if (string.Equals(_project.Nullable, "enable", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_project.Nullable, "annotations", StringComparison.OrdinalIgnoreCase))
            {
                opts.Add("--nullable");
            }

            opts.Add("--working-dir");
            opts.Add(Directory.GetCurrentDirectory());

            Exe.Run(executable, opts, _startupProject.ProjectDir);
        }

        private static List<string> ResolveProjects(string? path)
        {
            if (path == null)
            {
                path = Directory.GetCurrentDirectory();
            }
            else
            {
                path = Path.GetFullPath(path);

                if (!Directory.Exists(path)) // It's not a directory
                {
                    return new List<string> { path };
                }
            }

            var projectFiles = Directory.EnumerateFiles(path, "*.*proj", SearchOption.TopDirectoryOnly)
                .Where(f => !string.Equals(Path.GetExtension(f), ".xproj", StringComparison.OrdinalIgnoreCase))
                .Take(2).ToList();

            return projectFiles;
        }

        private static (string, string) ResolveProjects(
            string? projectPath,
            string? startupProjectPath)
        {
            var projects = ResolveProjects(projectPath);
            var startupProjects = ResolveProjects(startupProjectPath);

            if (projects.Count > 1)
            {
                throw new InvalidOperationException("Multiple projects in directory");
            }

            if (startupProjects.Count > 1)
            {
                throw new InvalidOperationException("Multiple projects in directory");
            }

            if (projectPath != null
                && projects.Count == 0)
            {
                throw new InvalidOperationException("No project in directory");
            }

            if (startupProjectPath != null
                && startupProjects.Count == 0)
            {
                throw new InvalidOperationException("No project in directory");
            }

            if (projectPath == null
                && startupProjectPath == null)
            {
                return projects.Count == 0
                    ? throw new InvalidOperationException("No project")
                    : (projects[0], startupProjects[0]);
            }

            if (projects.Count == 0)
            {
                return (startupProjects[0], startupProjects[0]);
            }

            if (startupProjects.Count == 0)
            {
                return (projects[0], projects[0]);
            }

            return (projects[0], startupProjects[0]);
        }
    }
}
