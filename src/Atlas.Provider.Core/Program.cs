﻿using System;
using Atlas.Provider.Core.Executor;

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
      using var executor = new EFDesign(
        assembly,
        startupAssembly,
        projectDir,
        null,
        rootNamespace,
        language,
        nullable,
        []
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
        if (ctxInfo["ProviderName"]!.ToString()!.EndsWith("SqlServer"))
        {
          sql = "-- atlas:delimiter GO \n" + sql;
        }
        if (!string.IsNullOrEmpty(sql))
        {
          Console.WriteLine(sql);
        }
      }
    }
  }
}