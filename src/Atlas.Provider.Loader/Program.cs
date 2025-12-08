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
    static int Main(
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
        runtimeOpts.Add("--additionalprobingpath");
        runtimeOpts.Add(loaderDirPath!);
        var corePackagePath = Path.Combine(loaderDirPath!, AssemblyName);
        if (Directory.Exists(corePackagePath))
        {
          runtimeOpts.Add("--additionalprobingpath");
          runtimeOpts.Add(corePackagePath);
        }
        var localStore = EnsureLocalPackageStore(loaderDirPath!);
        if (Directory.Exists(localStore))
        {
          runtimeOpts.Add("--additionalprobingpath");
          runtimeOpts.Add(localStore);
        }
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
        if (!string.IsNullOrEmpty(context))
        {
          arguments.Add("--context");
          arguments.Add(context);
        }
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

    /// <summary>
    /// Ensures a local package store exists by copying runtime assets from the loader directory.
    /// This enables the .NET runtime to resolve dependencies when running Atlas.Provider.Core.
    /// </summary>
    /// <param name="loaderDirPath">The directory containing the loader assembly.</param>
    /// <returns>The path to the local package store directory.</returns>
    private static string EnsureLocalPackageStore(string loaderDirPath)
    {
      var storeRoot = Path.Combine(loaderDirPath, ".packages");
      var depsPath = Path.Combine(loaderDirPath, AssemblyName + ".deps.json");

      if (!File.Exists(depsPath))
      {
        return storeRoot;
      }

      try
      {
        using var stream = File.OpenRead(depsPath);
        using var depsDoc = JsonDocument.Parse(stream);
        var root = depsDoc.RootElement;

        if (!root.TryGetProperty("runtimeTarget", out var runtimeTargetProp) ||
            !runtimeTargetProp.TryGetProperty("name", out var runtimeTargetNameProp) ||
            runtimeTargetNameProp.GetString() is not { Length: > 0 } runtimeTargetName)
        {
          return storeRoot;
        }

        if (!root.TryGetProperty("targets", out var targetsProp) ||
            !targetsProp.TryGetProperty(runtimeTargetName, out var targets) ||
            !root.TryGetProperty("libraries", out var libraries))
        {
          return storeRoot;
        }

        Dictionary<string, string>? filesByName = null;

        foreach (var lib in targets.EnumerateObject())
        {
          if (!lib.Value.TryGetProperty("runtime", out var runtimeAssets))
          {
            continue;
          }

          if (!libraries.TryGetProperty(lib.Name, out var libEntry))
          {
            continue;
          }

          var libPath = libEntry.TryGetProperty("path", out var pathProp) ? pathProp.GetString() : null;
          // For project refs (like Atlas.Provider.Core) there is no package path; use the library name.
          if (string.IsNullOrEmpty(libPath))
          {
            libPath = lib.Name;
          }

          foreach (var asset in runtimeAssets.EnumerateObject())
          {
            var relAssetPath = asset.Name.Replace('/', Path.DirectorySeparatorChar);
            var destPath = Path.Combine(storeRoot, libPath, relAssetPath);

            // Skip if destination already exists
            if (File.Exists(destPath))
            {
              continue;
            }

            filesByName ??= BuildFileIndex(loaderDirPath);
            var fileName = Path.GetFileName(relAssetPath);
            if (!filesByName.TryGetValue(fileName, out var source))
            {
              continue;
            }

            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
            {
              Directory.CreateDirectory(destDir);
              File.Copy(source, destPath, overwrite: false);
            }
          }
        }
      }
      catch
      {
        // Best-effort; fall back to other probing paths if this fails.
      }

      return storeRoot;
    }

    /// <summary>
    /// Builds a case-insensitive index of file names to their full paths.
    /// </summary>
    private static Dictionary<string, string> BuildFileIndex(string directory)
    {
      var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
      {
        var fileName = Path.GetFileName(path);
        result.TryAdd(fileName, path);
      }
      return result;
    }
  }
}
