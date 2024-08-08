using System;
using System.Collections.Generic;

namespace Atlas.Provider.Core
{
  public class Options
  {
    public string Assembly { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string StartupAssembly { get; set; } = string.Empty;
    public string StartupProject { get; set; } = string.Empty;
    public string ProjectDir { get; set; } = string.Empty;
    public string RootNamespace { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string WorkingDir { get; set; } = string.Empty;
    public bool Nullable { get; set; } = true;
    public List<string>? PositionalArgs { get; set; }

    public Options(string[] args)
    {
      for (int i = 0; i < args.Length; i++)
      {
        switch (args[i])
        {
          case "--assembly":
            if (i + 1 < args.Length) Assembly = args[++i];
            break;
          case "--project":
            if (i + 1 < args.Length) Project = args[++i];
            break;
          case "--startup-assembly":
            if (i + 1 < args.Length) StartupAssembly = args[++i];
            break;
          case "--startup-project":
            if (i + 1 < args.Length) StartupProject = args[++i];
            break;
          case "--project-dir":
            if (i + 1 < args.Length) ProjectDir = args[++i];
            break;
          case "--root-namespace":
            if (i + 1 < args.Length) RootNamespace = args[++i];
            break;
          case "--language":
            if (i + 1 < args.Length) Language = args[++i];
            break;
          case "--framework":
            if (i + 1 < args.Length) Framework = args[++i];
            break;
          case "--working-dir":
            if (i + 1 < args.Length) WorkingDir = args[++i];
            break;
          default:
            if (PositionalArgs == null)
            {
              PositionalArgs = new List<string>();
            }
            PositionalArgs.Add(args[i]);
            break;
        }
      }

      if (string.IsNullOrEmpty(Assembly)) throw new ArgumentException("--assembly option requires a value.");
      if (string.IsNullOrEmpty(Project)) throw new ArgumentException("--project option requires a value.");
      if (string.IsNullOrEmpty(StartupAssembly)) throw new ArgumentException("--startupAssembly option requires a value.");
      if (string.IsNullOrEmpty(StartupProject)) throw new ArgumentException("--startupProject option requires a value.");
      if (string.IsNullOrEmpty(ProjectDir)) throw new ArgumentException("--projectDir option requires a value.");
      if (string.IsNullOrEmpty(RootNamespace)) throw new ArgumentException("--rootNamespace option requires a value.");
      if (string.IsNullOrEmpty(Language)) throw new ArgumentException("--language option requires a value.");
      if (string.IsNullOrEmpty(Framework)) throw new ArgumentException("--framework option requires a value.");
      if (string.IsNullOrEmpty(WorkingDir)) throw new ArgumentException("--workingDir option requires a value.");
    }
  }
}