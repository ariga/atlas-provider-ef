using System;
using System.IO;
using Atlas.Provider.Core.Executor;
using System.Collections.Generic;

namespace Atlas.Provider.Core
{
  static class Program
  {
    static int Main(string[] args)
    {
      string? assembly = null;
      string? project = null;
      string? startupAssembly = null;
      string? startupProject = null;
      string? projectDir = null;
      string? rootNamespace = null;
      string? language = null;
      string? framework = null;
      string? workingDir = null;
      bool nullable = true;
      var positionalArgs = new List<string>();

      for (int i = 0; i < args.Length; i++)
      {
        switch (args[i])
        {
          case "--assembly":
            if (i + 1 < args.Length)
            {
              assembly = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --assembly option requires a value.");
            }
            break;
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
          case "--startupAssembly":
            if (i + 1 < args.Length)
            {
              startupAssembly = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --startupAssembly option requires a value.");
            }
            break;
          case "--startupProject":
            if (i + 1 < args.Length)
            {
              startupProject = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --startupProject option requires a value.");
            }
            break;
          case "--projectDir":
            if (i + 1 < args.Length)
            {
              projectDir = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --projectDir option requires a value.");
            }
            break;
          case "--rootNamespace":
            if (i + 1 < args.Length)
            {
              rootNamespace = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --rootNamespace option requires a value.");
            }
            break;
          case "--language":
            if (i + 1 < args.Length)
            {
              language = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --language option requires a value.");
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
          case "--workingDir":
            if (i + 1 < args.Length)
            {
              workingDir = args[++i];
            }
            else
            {
              Console.WriteLine("Error: --workingDir option requires a value.");
            }
            break;
          case "--nullable":
            if (i + 1 < args.Length)
            {
              if (args[++i] == "true")
              {
                nullable = true;
              }
              else if (args[i] == "false")
              {
                nullable = false;
              }
            }
            break;
          default:
            positionalArgs.Add(args[i]);
            break;
        }
      }

      try
      {
        // prevent any output from being written to the console, including warn, info, error etc messages from EF Core
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);
        using var executor = new EFDesign(
          assembly,
          startupAssembly,
          projectDir,
          null,
          rootNamespace,
          language,
          nullable,
          args
        );
        var types = executor.GetContextTypes();
        foreach (var type in types)
        {
          if (!type.Contains("Name") || type["Name"] == null)
          {
            continue;
          }
          var name = type["Name"]!.ToString();
          if (string.IsNullOrEmpty(name))
          {
            continue;
          }
          var ctxInfo = executor.GetContextInfo(name);
          if (ctxInfo == null || !ctxInfo.Contains("ProviderName") || ctxInfo["ProviderName"] == null)
          {
            continue;
          }
          var sql = executor.ScriptDbContext(name);
          Console.SetOut(originalOut);
          if (!string.IsNullOrEmpty(sql))
          {
            if (ctxInfo["ProviderName"]!.ToString()!.EndsWith("SqlServer"))
            {
              Console.WriteLine("-- atlas:delimiter GO");
            }
            Console.WriteLine(sql);
          }
        }
        return 0;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex.Message);
        return 1;
      }
    }
  }
}