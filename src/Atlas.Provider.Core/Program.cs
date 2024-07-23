using System;

namespace Atlas.Provider.Core
{
  static class Program
  {
    static void Main(
      string assembly,
      string project,
      string startupAssembly,
      string startupProject,
      string projectDir,
      string rootNamespace,
      string language,
      string framework,
      string workingDir,
      bool nullable = true
    )
    {
      using var executor = new ReflectionOperationExecutor(
        assembly: assembly,
        startupAssembly: startupAssembly,
        projectDir: projectDir,
        dataDirectory: null,
        rootNamespace: rootNamespace,
        language: language,
        nullable: nullable,
        remainingArguments: null
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

        var sql = executor.ScriptDbContext(name);
        var ctxInfo = executor.GetContextInfo(name);

        if (ctxInfo == null || !ctxInfo.Contains("ProviderName") || ctxInfo["ProviderName"] == null)
        {
          continue;
        }

        var providerName = ctxInfo["ProviderName"]!.ToString();
        if (providerName!.EndsWith("SqlServer"))
        {
          sql = "-- atlas:delimiter GO \n" + sql;
        }

        Console.WriteLine(sql ?? string.Empty);
      }
    }
  }
}
