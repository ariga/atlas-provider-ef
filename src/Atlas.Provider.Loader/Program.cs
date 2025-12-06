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

    private static string EnsureLocalPackageStore(string loaderDirPath)
    {
      var storeRoot = Path.Combine(loaderDirPath, ".packages");
      var storeRootFull = Path.GetFullPath(storeRoot);
      var storeRootPrefix = storeRootFull.EndsWith(Path.DirectorySeparatorChar)
        ? storeRootFull
        : storeRootFull + Path.DirectorySeparatorChar;
      try
      {
        var depsPath = Path.Combine(loaderDirPath, AssemblyName + ".deps.json");
        if (!File.Exists(depsPath))
        {
          return storeRoot;
        }
        using var depsDoc = JsonDocument.Parse(File.ReadAllBytes(depsPath));
        var root = depsDoc.RootElement;
        if (!root.TryGetProperty("runtimeTarget", out var runtimeTargetProp)
            || !runtimeTargetProp.TryGetProperty("name", out var runtimeTargetNameProp))
        {
          return storeRoot;
        }
        var runtimeTargetName = runtimeTargetNameProp.GetString();
        if (string.IsNullOrEmpty(runtimeTargetName))
        {
          return storeRoot;
        }
        if (!root.TryGetProperty("targets", out var targetsProp)
            || !targetsProp.TryGetProperty(runtimeTargetName, out var targets))
        {
          return storeRoot;
        }
        if (!root.TryGetProperty("libraries", out var libraries))
        {
          return storeRoot;
        }

        var runtimeLibs = new List<(string Name, JsonElement RuntimeAssets)>();
        foreach (var lib in targets.EnumerateObject())
        {
          if (!lib.Value.TryGetProperty("runtime", out var runtimeAssets))
          {
            continue;
          }
          if (!runtimeAssets.EnumerateObject().Any())
          {
            continue;
          }
          runtimeLibs.Add((lib.Name, runtimeAssets));
        }
        if (runtimeLibs.Count == 0)
        {
          return storeRoot;
        }

        var filesByName = Directory.EnumerateFiles(loaderDirPath, "*", SearchOption.AllDirectories)
          .ToLookup(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
        foreach (var lib in runtimeLibs)
        {
          if (!libraries.TryGetProperty(lib.Name, out var libEntry))
          {
            continue;
          }
          var libPath = libEntry.TryGetProperty("path", out var pathProp) ? pathProp.GetString() : null;
          // For project refs (like Atlas.Provider.Core in our tool) there is no package path; use the library name.
          if (string.IsNullOrEmpty(libPath))
          {
            libPath = lib.Name;
          }
          foreach (var asset in lib.RuntimeAssets.EnumerateObject())
          {
            var relAssetPath = asset.Name.Replace('/', Path.DirectorySeparatorChar);
            var destPath = Path.GetFullPath(Path.Combine(storeRootPrefix, libPath, relAssetPath));
            if (!destPath.StartsWith(storeRootPrefix, StringComparison.OrdinalIgnoreCase))
            {
              continue;
            }
            var destDir = Path.GetDirectoryName(destPath);
            if (destDir == null)
            {
              continue;
            }
            Directory.CreateDirectory(destDir);
            if (File.Exists(destPath))
            {
              continue;
            }
            var fileName = Path.GetFileName(relAssetPath);
            var source = filesByName[fileName].FirstOrDefault();
            if (source == null)
            {
              continue;
            }
            File.Copy(source, destPath, true);
          }
        }
      }
      catch
      {
        // Best-effort; fall back to other probing paths if this fails.
      }
      return storeRoot;
    }
  }
}
