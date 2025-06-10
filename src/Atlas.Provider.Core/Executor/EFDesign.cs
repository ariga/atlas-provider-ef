using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
  private readonly string _projectDir;

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
    try
    {
      _commandsAssembly = Assembly.Load(new AssemblyName { Name = DesignAssemblyName });
    }
    catch (FileNotFoundException ex)
    when (ex.FileName != null &&
      new AssemblyName(ex.FileName).Name == DesignAssemblyName)
    {
      throw new FileNotFoundException(
        $"Could not find package {DesignAssemblyName}. " +
        $"This package is required for the tool to work. Ensure your startup project is correct, install the package, and try again."
      );
    }

    var reportHandlerType = _commandsAssembly.GetType(ReportHandlerTypeName, throwOnError: true, ignoreCase: false)!;
    var reportHandler = Activator.CreateInstance(
      reportHandlerType,
      (Action<string>)(_ => { }),
      (Action<string>)(_ => { }),
      (Action<string>)(_ => { }),
      (Action<string>)(_ => { })
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
    _projectDir = projectDir ?? Directory.GetCurrentDirectory();
    
    AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
  }

  public void Dispose()
  {
    AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
  }

  public IEnumerable<IDictionary> GetContextTypes()
    => InvokeOperation<IEnumerable<IDictionary>>(GetContextTypesTypeName,
      new Dictionary<string, object>(0));

  public IDictionary GetContextInfo(string? name)
    => InvokeOperation<IDictionary>(GetContextInfoTypeName,
      new Dictionary<string, object?> { ["contextType"] = name });

  public string ScriptDbContext(string? name)
    => InvokeOperation<string>(ScriptDbContextTypeName,
      new Dictionary<string, object?> { ["contextType"] = name });

  public List<(string EntityName, string TableName)> GetEntityTable(string? contextName)
  {
    var contextOps = GetDbContextOperations(_executor.GetType());
    
    if (contextOps == null)
      throw new InvalidOperationException("Could not access DbContext operations");

    var method = contextOps.GetType().GetMethod("CreateContext", new[] { typeof(string) });
    if (method == null)
      throw new InvalidOperationException("Could not find CreateContext method");

    using var context = (IDisposable)method.Invoke(contextOps, new object?[] { contextName });
    if (context == null)
      throw new InvalidOperationException("Could not create DbContext");

    var entityTypes = GetEntityTypes(GetModel(context));
    
    return entityTypes.Select(entityType => 
    {
      var entityName = GetEntityName(entityType);
      var tableName = GetTableName(entityType) ?? entityName;
      return (entityName, tableName);
    }).ToList();
  }

  private object? GetDbContextOperations(Type executorType)
  {
    var field = executorType.GetField("_contextOperations", BindingFlags.NonPublic | BindingFlags.Instance);
    if (field != null)
      return field.GetValue(_executor);
      
    var property = executorType.GetProperty("DbContextOperations", BindingFlags.NonPublic | BindingFlags.Instance);
    return property?.GetValue(_executor);
  }

  private object GetModel(IDisposable context)
  {
    var modelProperty = context.GetType().GetProperty("Model");
    if (modelProperty == null)
      throw new InvalidOperationException("Could not access Model property");
      
    var model = modelProperty.GetValue(context);
    if (model == null)
      throw new InvalidOperationException("Model is null");
      
    return model;
  }

  private IEnumerable<object> GetEntityTypes(object model)
  {
    var iModelType = FindIModelType();
    if (iModelType == null || !iModelType.IsAssignableFrom(model.GetType()))
      throw new InvalidOperationException("Could not cast to IModel");

    var method = iModelType.GetMethod("GetEntityTypes", Type.EmptyTypes);
    if (method == null)
      throw new InvalidOperationException("Could not find GetEntityTypes method");

    var entities = method.Invoke(model, null);
    if (entities == null)
      throw new InvalidOperationException("EntityTypes collection is null");

    return (IEnumerable<object>)entities;
  }

  private Type? FindIModelType()
  {
    return AppDomain.CurrentDomain.GetAssemblies()
      .Where(a => a.FullName?.Contains("EntityFrameworkCore") == true)
      .Select(assembly => assembly.GetType("Microsoft.EntityFrameworkCore.Metadata.IModel"))
      .FirstOrDefault(type => type != null);
  }

  private string GetEntityName(object entityType)
  {
    var name = entityType.GetType().GetProperty("Name");
    var fullName = name?.GetValue(entityType)?.ToString();
    
    if (string.IsNullOrEmpty(fullName))
      throw new InvalidOperationException("Entity name is null or empty");

    // fullName is expected to be in the format "Namespace.EntityName"
    var dot = fullName!.LastIndexOf('.');
    return dot >= 0 ? fullName.Substring(dot + 1) : fullName;
  }

  private string? GetTableName(object entityType)
  {
    try
    {
      var method = entityType.GetType().GetMethod("GetAnnotation", new[] { typeof(string) });
      if (method == null) return null;

      var table = method.Invoke(entityType, new object[] { "Relational:TableName" });
      var tableName = GetAnnotationValue(table);

      var schema = method.Invoke(entityType, new object[] { "Relational:Schema" });
      var schemaName = GetAnnotationValue(schema);

      return !string.IsNullOrEmpty(schemaName) && !string.IsNullOrEmpty(tableName) 
        ? $"{schemaName}.{tableName}" 
        : tableName;
    }
    catch
    {
      return null;
    }
  }

  private string? GetAnnotationValue(object? annotation)
  {
    if (annotation == null) return null;
    
    var value = annotation.GetType().GetProperty("Value");
    return value?.GetValue(annotation)?.ToString();
  }

  private TResult InvokeOperation<TResult>(string optype, IDictionary arguments)
  {
    var op = _commandsAssembly.GetType(optype, throwOnError: true, ignoreCase: true)!;
    var result = (dynamic)Activator.CreateInstance(_resultHandlerType)!;
    Activator.CreateInstance(op, _executor, result, arguments);
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
          continue;
        }
      }
    }
    return null;
  }

  public string? LocateEntitySource(string entityName, string? contextName)
  {
    try
    {
      var e = GetEntityInfo(entityName, contextName);
      if (!e.HasValue)
      {
        return null;
      }
      return Search(e.Value);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error finding entity location: {ex.Message}");
      return null;
    }
  }
  
  private (Type? ClrType, string FullName, string? Namespace)? GetEntityInfo(string entityName, string? contextName)
  {
    try
    {
      var dbContextOps = GetDbContextOperations(_executor.GetType());
      if (dbContextOps == null) return null;
      
      var method = dbContextOps.GetType().GetMethod("CreateContext", new[] { typeof(string) });
      
      using var context = (IDisposable?)method?.Invoke(dbContextOps, new object?[] { contextName });
      if (context == null) return null;

      var model = GetModel(context);
      var entityTypes = GetEntityTypes(model);
      
      foreach (var e in entityTypes)
      {
        if (GetEntityName(e) == entityName)
        {
          var clrType = GetClrType(e);
          
          string fullName = clrType?.FullName ?? string.Empty;
          if (string.IsNullOrEmpty(fullName))
          {
            var n = e.GetType().GetProperty("Name");
            fullName = n?.GetValue(e)?.ToString() ?? string.Empty;
          }
          
          string? namespaceName = clrType?.Namespace;
          if (string.IsNullOrEmpty(namespaceName) && !string.IsNullOrEmpty(fullName))
          {
            var dot = fullName.LastIndexOf('.');
            namespaceName = dot >= 0 ? fullName.Substring(0, dot) : null;
          }
          
          return (clrType, fullName, namespaceName);
        }
      }
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Error accessing entity information for '{entityName}': {ex.Message}");
      return null;
    }
    
    return null;
  }

  private Type? GetClrType(object entityType)
  {
    try
    {
      var clrTypeProperty = entityType.GetType().GetProperty("ClrType");
      return clrTypeProperty?.GetValue(entityType) as Type;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Warning: Unable to get CLR type for entity: {ex.Message}");
      return null;
    }
  }
  
  private string? Search((Type? ClrType, string FullName, string? Namespace) entityInfo)
  {
    var className = entityInfo.FullName.Split('.').Last();
    var dirs = new[] { _projectDir };

    if (entityInfo.Namespace != null)
    {
      var p = Path.Combine(_projectDir, entityInfo.Namespace.Replace('.', Path.DirectorySeparatorChar));
      if (Directory.Exists(p))
      {
        dirs = new[] { p };
      }
    }

    foreach (var d in dirs)
    {
      var files = Directory.GetFiles(d, "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains("bin") && !f.Contains("obj"));

      foreach (var f in files)
      {
        try
        {
          var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f);
          var root = syntaxTree.GetRoot();

          var dec = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.ValueText == className);

          if (dec != null)
          {
            var span = syntaxTree.GetLineSpan(dec.Span);
            var path = Path.GetRelativePath(_projectDir, f).Replace(Path.DirectorySeparatorChar, '/');
            return $"{path}:{span.StartLinePosition.Line + 1}-{span.EndLinePosition.Line + 1}";
          }
        }
        catch
        {
          continue;
        }
      }
    }

    return null;
  }
}
