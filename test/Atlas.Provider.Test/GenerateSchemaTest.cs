using Xunit;
using System.Diagnostics;
using System.Reflection;

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
      WorkingDirectory = Path.GetFullPath("../../../../../src/Atlas.Provider.Demo"),
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
    string error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    string normalizedOutput = FileReader.Read(expectedFile).Replace("\r\n", "\n");
    Assert.Equal(FileReader.Read(expectedFile), normalizedOutput);
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