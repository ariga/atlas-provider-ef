using System.Text.Json;

namespace Atlas.Provider.Loader
{
  static class Program
  {
    private const string AssemblyName = "Atlas.Provider.Core";

    static int Main(string[] args)
    {
      string? project = null;
      string? startupProject = null;
      string? framework = null;
      string? context = null;
      bool noBuild = false;
      var positionalArgs = new List<string>();

      for (int i = 0; i < args.Length; i++)
      {
        switch (args[i])
        {
          case "--project":
            if (i + 1 < args.Length)
            {
              project = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --project option requires a value.");
            }
            break;
          case "--startup-project":
            if (i + 1 < args.Length)
            {
              startupProject = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --startup-project option requires a value.");
            }
            break;
          case "--framework":
            if (i + 1 < args.Length)
            {
              framework = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --framework option requires a value.");
            }
            break;
          case "--context":
            if (i + 1 < args.Length)
            {
              context = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --context option requires a value.");
            }
            break;
          case "--no-build":
            noBuild = true;
            break;
          default:
          positionalArgs.Add(args[i]);
            break;
        }
      }

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
        return Exe.Run("dotnet", [
            "exec",
                .. runtimeOpts,
                Path.Combine(loaderDirPath!, AssemblyName + ".dll"),
                .. arguments,
            ], _startupProject.ProjectDir);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex.Message);
        return 1;
      }
    }
  }
}
