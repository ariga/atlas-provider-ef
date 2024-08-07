using System;
using System.IO;
using Atlas.Provider.Core.Executor;

namespace Atlas.Provider.Core
{
  static class Program
  {
    static int Main(
      string assembly,
      string project,
      string startupAssembly,
      string startupProject,
      string projectDir,
      string rootNamespace,
      string language,
      string framework,
      string workingDir,
      bool nullable = true,
      string[]? args = null
    )
    {
      try
      {
        // prevent any output from being written to the console, including warn, info, error etc messages from EF Core
        var originalOut = Console.Out;
        Console.SetOut(Console.Error);
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