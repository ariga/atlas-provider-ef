using System.Text.Json;

namespace Atlas.Provider.Loader
{
  static class Program
  {
    private const string AssemblyName = "Atlas.Provider.Core";

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
      try
      {
        var (projectFile, startupProjectFile) = Project.ResolveProjects(project, startupProject);
        var _project = Project.FromFile(projectFile, null, framework);
        var _startupProject = Project.FromFile(startupProjectFile, null, framework);
        if (!noBuild)
        {
          _startupProject.Build();
        }
        var targetDir = Path.GetFullPath(Path.Combine(_startupProject.ProjectDir!, _startupProject.OutputPath!));
        var loaderDirPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        var runtimeOpts = new List<string>
            {
                "--depsfile", Path.Combine(targetDir, _startupProject.AssemblyName + ".deps.json"),
                // Load the deps for the atlas provider core
                "--additional-deps", Path.Combine(loaderDirPath!, AssemblyName + ".deps.json"),
            };
        var projectAssetsFile = _startupProject.ProjectAssetsFile;
        if (!string.IsNullOrEmpty(projectAssetsFile))
        {
          using var file = File.OpenRead(projectAssetsFile);
          using var reader = JsonDocument.Parse(file);
          var folders = reader.RootElement
              .GetProperty("packageFolders")
              .EnumerateObject()
              .Select(p => p.Name);
          foreach (var folder in folders)
          {
            runtimeOpts.Add("--additionalprobingpath");
            runtimeOpts.Add(folder.TrimEnd(Path.DirectorySeparatorChar));
          }
        }

        var runtimeConfig = Path.Combine(targetDir, _startupProject.AssemblyName + ".runtimeconfig.json");
        if (File.Exists(runtimeConfig))
        {
          runtimeOpts.Add("--runtimeconfig");
          runtimeOpts.Add(runtimeConfig);
        }
        else if (_startupProject.RuntimeFrameworkVersion!.Length != 0)
        {
          runtimeOpts.Add("--fx-version");
          runtimeOpts.Add(_startupProject.RuntimeFrameworkVersion);
        }

        var arguments = new List<string>();
        if (args != null)
        {
          arguments.AddRange(args);
        }
        arguments.AddRange([
            "--root-namespace", _project.RootNamespace!,
                "--language", _project.Language!,
                "--project-dir", _project.ProjectDir!,
                "--working-dir", Directory.GetCurrentDirectory(),
                "--assembly", Path.Combine(targetDir, _project.TargetFileName!),
                "--project", projectFile,
                "--framework", _startupProject.TargetFramework!,
                "--startup-assembly", Path.Combine(targetDir, _startupProject.TargetFileName!),
                "--startup-project", startupProjectFile,
            ]);
        if (string.Equals(_project.Nullable, "enable", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_project.Nullable, "annotations", StringComparison.OrdinalIgnoreCase))
        {
          arguments.Add("--nullable");
        }
        // dotnet exec [runtime-options] [path-to-application] [arguments]
        Exe.Run("dotnet", [
            "exec",
                .. runtimeOpts,
                Path.Combine(loaderDirPath!, AssemblyName + ".dll"),
                .. arguments,
            ], _startupProject.ProjectDir);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex.Message);
      }
    }
  }
}
