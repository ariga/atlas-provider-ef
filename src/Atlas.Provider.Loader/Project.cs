using System.Diagnostics;

namespace Atlas.Provider.Loader;

internal class Project
{
  private readonly string _file;
  private readonly string? _framework;
  private readonly string? _configuration;
  private readonly string? _runtime;

  public Project(string file, string? framework, string? configuration, string? runtime)
  {
    Debug.Assert(!string.IsNullOrEmpty(file), "file is null or empty.");

    _file = file;
    _framework = framework;
    _configuration = configuration;
    _runtime = runtime;
    ProjectName = Path.GetFileName(file);
  }

  public string ProjectName { get; }

  public string? AssemblyName { get; set; }
  public string? Language { get; set; }
  public string? OutputPath { get; set; }
  public string? PlatformTarget { get; set; }
  public string? ProjectAssetsFile { get; set; }
  public string? ProjectDir { get; set; }
  public string? RootNamespace { get; set; }
  public string? RuntimeFrameworkVersion { get; set; }
  public string? TargetFileName { get; set; }
  public string? TargetFrameworkMoniker { get; set; }
  public string? Nullable { get; set; }
  public string? TargetFramework { get; set; }
  public string? TargetPlatformIdentifier { get; set; }

  public static Project FromFile(
      string file,
      string? buildExtensionsDir,
      string? framework = null,
      string? configuration = null,
      string? runtime = null)
  {
    Debug.Assert(!string.IsNullOrEmpty(file), "file is null or empty.");

    buildExtensionsDir ??= Path.Combine(Path.GetDirectoryName(file)!, "obj");
    Directory.CreateDirectory(buildExtensionsDir);
    var targetsPath = Path.Combine(buildExtensionsDir, Path.GetFileName(file) + ".atlas-ef.targets");
    using (var input = typeof(Project).Assembly.GetManifestResourceStream("Atlas.Provider.Loader.Resources.AtlasEF.targets")!)
    {
      using var output = File.OpenWrite(targetsPath);
      input.CopyTo(output);
    }

    IDictionary<string, string> metadata;
    var metadataFile = Path.GetTempFileName();
    try
    {
      var propertyArg = "/property:OutputFile=" + metadataFile;
      if (framework != null)
      {
        propertyArg += ";TargetFramework=" + framework;
      }
      if (configuration != null)
      {
        propertyArg += ";Configuration=" + configuration;
      }
      if (runtime != null)
      {
        propertyArg += ";RuntimeIdentifier=" + runtime;
      }
      var exitCode = Exe.Run("dotnet", [
          "msbuild",
                "/target:AtlasEFProjectMetadata",
                propertyArg,
                "/verbosity:quiet",
                "/nologo",
                file,
            ]);
      if (exitCode != 0)
      {
        throw new Exception("Metadata extraction failed");
      }
      metadata = File.ReadLines(metadataFile)
          .Select(l => l.Split(':', 2))
          .ToDictionary(s => s[0], s => s[1].TrimStart());
    }
    finally
    {
      File.Delete(metadataFile);
    }
    var platformTarget = metadata["PlatformTarget"];
    if (platformTarget.Length == 0)
    {
      platformTarget = metadata["Platform"];
    }
    return new Project(file, framework, configuration, runtime)
    {
      AssemblyName = metadata["AssemblyName"],
      Language = metadata["Language"],
      OutputPath = metadata["OutputPath"],
      PlatformTarget = platformTarget,
      ProjectAssetsFile = metadata["ProjectAssetsFile"],
      ProjectDir = metadata["ProjectDir"],
      RootNamespace = metadata["RootNamespace"],
      RuntimeFrameworkVersion = metadata["RuntimeFrameworkVersion"],
      TargetFileName = metadata["TargetFileName"],
      TargetFrameworkMoniker = metadata["TargetFrameworkMoniker"],
      Nullable = metadata["Nullable"],
      TargetFramework = metadata["TargetFramework"],
      TargetPlatformIdentifier = metadata["TargetPlatformIdentifier"]
    };
  }

  public static string? Search(string? dir)
  {
    var non = dir == null;
    if (dir == null)
    {
      dir = Directory.GetCurrentDirectory();
    }
    else
    {
      dir = Path.GetFullPath(dir);
      if (!Directory.Exists(dir))
      {
        return null;
      }
    }
    var projectFiles = Directory.EnumerateFiles(dir, "*.*proj", SearchOption.TopDirectoryOnly)
        .Where(f => !string.Equals(Path.GetExtension(f), ".xproj", StringComparison.OrdinalIgnoreCase))
        .Take(2).ToList();
    if (projectFiles.Count > 1)
    {
      throw new InvalidOperationException("Multiple projects in directory");
    }
    if (non && projectFiles.Count == 0)
    {
      throw new InvalidOperationException("No project in directory");
    }
    return projectFiles[0];
  }

  public static (string, string) ResolveProjects(string? projectDir, string? startupProjectDir)
  {
    var project = Search(projectDir);
    if (projectDir == startupProjectDir)
    {
      if (project == null)
      {
        throw new InvalidOperationException("No project");
      }
      return (project, project);
    }
    var startupProject = Search(startupProjectDir);
    if (startupProject != null && project != null)
    {
      return (project, startupProject);
    }
    else if (startupProject != null)
    {
      return (startupProject, startupProject);
    }
    else if (project != null)
    {
      return (project, project);
    }
    throw new InvalidOperationException("No project");
  }

  public void Build()
  {
    var args = new List<string> { "build", _file };
    // TODO: Only build for the first framework when unspecified
    if (_framework != null)
    {
      args.Add("--framework");
      args.Add(_framework);
    }
    if (_configuration != null)
    {
      args.Add("--configuration");
      args.Add(_configuration);
    }
    if (_runtime != null)
    {
      args.Add("--runtime");
      args.Add(_runtime);
    }
    args.Add("/verbosity:quiet");
    args.Add("/nologo");
    args.Add("/p:PublishAot=false"); // Avoid NativeAOT warnings
    // Discard the build output, but show the errors
    var exitCode = Exe.Run("dotnet", args, handleOutput: delegate (string? line) { });
    if (exitCode != 0)
    {
      throw new Exception("Build failed. Use dotnet build to see the errors.");
    }
  }
}
