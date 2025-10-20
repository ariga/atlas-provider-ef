using Xunit;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

public class ContextFilterTest
{
    private static string GetMultiContextDemoProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var solutionRoot = FindSolutionRoot(currentDir);
        return Path.Combine(solutionRoot, "src", "Atlas.Provider.Demo.MultiContext");
    }

    private static string FindSolutionRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Any())
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Could not find solution root directory");
    }

    [Theory]
    [InlineData("MySql", "BloggingContext", "data/mysql_context_blogging")]
    [InlineData("MySql", "ProductContext", "data/mysql_context_product")]
    public void Can_filter_by_context_name(string providerName, string contextName, string expectedFile)
    {
        var dllFileName = Assembly.Load(new AssemblyName("Atlas.Provider.Loader")).Location;
        var demoProjectPath = GetMultiContextDemoProjectPath();

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            WorkingDirectory = demoProjectPath,
            FileName = "dotnet",
            Arguments = $"exec {dllFileName} --context {contextName} -- {providerName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? process = Process.Start(startInfo);
        Assert.NotNull(process);
        string output = process.StandardOutput.ReadToEnd();
        output = output.Replace(
            demoProjectPath + Path.DirectorySeparatorChar,
            "",
            StringComparison.OrdinalIgnoreCase
        );
        if (Path.DirectorySeparatorChar == '\\')
        {
            output = Regex.Replace(output, @"\\(?=[^\\]*\.[a-zA-Z]+:\d+)", "/");
        }

        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.Equal(FileReader.Read(expectedFile), output);
        Assert.Equal("", error);
    }

    [Fact]
    public void Without_context_parameter_discovers_all_contexts()
    {
        // Test that without --context, both contexts are discovered
        var dllFileName = Assembly.Load(new AssemblyName("Atlas.Provider.Loader")).Location;
        var demoProjectPath = GetMultiContextDemoProjectPath();

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            WorkingDirectory = demoProjectPath,
            FileName = "dotnet",
            Arguments = $"exec {dllFileName} -- MySql",  // No --context
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? process = Process.Start(startInfo);
        Assert.NotNull(process);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        // Should contain tables from BOTH contexts
        Assert.Contains("Blogs", output);
        Assert.Contains("Posts", output);
        Assert.Contains("Products", output);
    }
}
