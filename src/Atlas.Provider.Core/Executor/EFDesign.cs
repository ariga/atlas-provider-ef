using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Atlas.Provider.Core.Executor;

// EFDesign is a class that provides a way to execute Entity Framework Core design-time operations.
// It is used by the AtlasProviderEF to execute EF Core commands.
internal class EFDesign : IDisposable
{
  // The name of the assembly that contains the design-time commands.
  private const string DesignAssemblyName = "Microsoft.EntityFrameworkCore.Design";
  // The fully qualified name of the report handler type.
  private const string ReportHandlerTypeName = DesignAssemblyName + ".OperationReportHandler";
  // The fully qualified name of the result handler type.
  private const string ResultHandlerTypeName = DesignAssemblyName + ".OperationResultHandler";
  // The fully qualified name of the executor type.
  private const string ExecutorTypeName = DesignAssemblyName + ".OperationExecutor";

  // All the operation types we are interested in.
  private const string GetContextTypesTypeName = ExecutorTypeName + "+GetContextTypes";
  private const string ScriptDbContextTypeName = ExecutorTypeName + "+ScriptDbContext";
  private const string GetContextInfoTypeName = ExecutorTypeName + "+GetContextInfo";

  private readonly Assembly _commandsAssembly;
  private readonly object _executor;
  private readonly Type _resultHandlerType;
  private readonly string _appBasePath;

  public EFDesign(
    string assembly,
    string? startupAssembly,
    string? projectDir,
    string? dataDirectory,
    string? rootNamespace,
    string? language,
    bool nullable,
    string[]? remainingArguments
  )
  {
    _commandsAssembly = Assembly.Load(new AssemblyName { Name = DesignAssemblyName });

    var reportHandlerType = _commandsAssembly.GetType(ReportHandlerTypeName, throwOnError: true, ignoreCase: false)!;
    var reportHandler = Activator.CreateInstance(
      reportHandlerType,
      (Action<string>)Reporter.WriteError,
      (Action<string>)Reporter.WriteWarning,
      (Action<string>)Reporter.WriteInformation,
      (Action<string>)Reporter.WriteVerbose
    )!;

    var assemblyFileName = Path.GetFileNameWithoutExtension(assembly);
    var startupAssemblyFileName = startupAssembly == null
        ? assemblyFileName
        : Path.GetFileNameWithoutExtension(startupAssembly);

    _executor = Activator.CreateInstance(
      _commandsAssembly.GetType(ExecutorTypeName, throwOnError: true, ignoreCase: false)!,
      reportHandler,
      new Dictionary<string, object?>
      {
        { "targetName", assemblyFileName },
        { "startupTargetName", startupAssemblyFileName },
        { "projectDir", projectDir ?? Directory.GetCurrentDirectory() },
        { "rootNamespace", rootNamespace ?? assemblyFileName },
        { "language", language },
        { "nullable", nullable },
        { "remainingArguments", remainingArguments ?? Array.Empty<string>() }
      })!;
    _resultHandlerType = _commandsAssembly.GetType(ResultHandlerTypeName, throwOnError: true, ignoreCase: false)!;

    // Setup the assembly resolution handler for the design-time commands.
    var configurationFile = (startupAssembly ?? assembly) + ".config";
    if (File.Exists(configurationFile))
    {
      AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configurationFile);
    }
    if (dataDirectory != null)
    {
      AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
    }
    _appBasePath = Path.GetFullPath(Path.Combine(
      Directory.GetCurrentDirectory(),
      Path.GetDirectoryName(startupAssembly ?? assembly)!
    ));
    AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
  }

  public void Dispose()
    => AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;

  public IEnumerable<IDictionary> GetContextTypes()
    => InvokeOperation<IEnumerable<IDictionary>>(GetContextTypesTypeName,
      new Dictionary<string, object>(0));

  public IDictionary GetContextInfo(string? name)
    => InvokeOperation<IDictionary>(GetContextInfoTypeName,
      new Dictionary<string, object?> { ["contextType"] = name });

  public string ScriptDbContext(string? name)
    => InvokeOperation<string>(ScriptDbContextTypeName,
      new Dictionary<string, object?> { ["contextType"] = name });

  private TResult InvokeOperation<TResult>(string optype, IDictionary arguments)
  {
    var operationType = _commandsAssembly.GetType(optype, throwOnError: true, ignoreCase: true)!;
    var result = (dynamic)Activator.CreateInstance(_resultHandlerType)!;
    Activator.CreateInstance(operationType, _executor, result, arguments);
    if (result.ErrorType != null)
    {
      throw new WrappedException(result.ErrorType, result.ErrorMessage, result.ErrorStackTrace);
    }
    return (TResult)result.Result;
  }

  private Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
  {
    var assemblyName = new AssemblyName(args.Name);
    foreach (var extension in new[] { ".dll", ".exe" })
    {
      var path = Path.Combine(_appBasePath, assemblyName.Name + extension);
      if (File.Exists(path))
      {
        try
        {
          return Assembly.LoadFrom(path);
        }
        catch
        {
        }
      }
    }
    return null;
  }
}
