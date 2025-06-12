using Xunit;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

public class GenerateSchemaTest
{
  [Theory]
  [InlineData("SqlServer", "data/sqlserver_default")]
  [InlineData("Postgres", "data/postgres_default")]
  [InlineData("MySql", "data/mysql_default")]
  [InlineData("Sqlite", "data/sqlite_default")]
  public void Can_generate_script(string providerName, string expectedFile)
  {
    var dllFileName = Assembly.Load(new AssemblyName("Atlas.Provider.Loader")).Location;

    ProcessStartInfo startInfo = new ProcessStartInfo
    {
      WorkingDirectory = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", "src", "Atlas.Provider.Demo")),
      FileName = "dotnet",
      Arguments = $"exec {dllFileName} -- {providerName}",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using Process? process = Process.Start(startInfo);
    Assert.NotNull(process);
    string output = process.StandardOutput.ReadToEnd();
    output = output.Replace(
      Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", "src", "Atlas.Provider.Demo")) + Path.DirectorySeparatorChar,
      "",
      StringComparison.OrdinalIgnoreCase
    );
    if (Path.DirectorySeparatorChar == '\\')
    {
      // (Windows) Replace backslashes in file paths (patterns like "folder\file.ext:line-line")
      // but not escape sequences or other backslashes in SQL content
      output = Regex.Replace(output, @"\\(?=[^\\]*\.[a-zA-Z]+:\d+)", "/");
    }
    
    string error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    Assert.Equal(FileReader.Read(expectedFile), output);
    Assert.Equal("", error);
  }
}
internal static class FileReader
{
  public static string Read(string filePath)
  {
    return File.ReadAllText(
        Path.Combine(
            Directory.GetCurrentDirectory(),
            filePath
        ));
  }
}