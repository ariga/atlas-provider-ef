var target = Argument("Target", "Default");
var configuration =
  HasArgument("Configuration") ? Argument<string>("Configuration") :
  EnvironmentVariable("Configuration", "Release");

var artifactsDirectory = Directory("./artifacts");

Task("Clean")
  .Description("Cleans the artifacts, bin and obj directories.")
  .Does(() =>
  {
    CleanDirectory(artifactsDirectory);
    DeleteDirectories(GetDirectories("**/bin"), new DeleteDirectorySettings() { Force = true, Recursive = true });
    DeleteDirectories(GetDirectories("**/obj"), new DeleteDirectorySettings() { Force = true, Recursive = true });
  });

Task("Restore")
  .Description("Restores NuGet packages.")
  .IsDependentOn("Clean")
  .Does(() =>
  {
    DotNetRestore();
  });

Task("Build")
  .Description("Builds the solution.")
  .IsDependentOn("Restore")
  .Does(() =>
  {
    DotNetBuild(".", new DotNetBuildSettings()
    {
      Configuration = configuration,
      NoRestore = true,
    });
  });

Task("Test")
  .Description("Runs unit tests and outputs test results to the artifacts directory.")
  .DoesForEach(GetFiles("./test/**/*.csproj"), project =>
  {
    DotNetTest(project.ToString(), new DotNetTestSettings()
    {
      Blame = true,
      Configuration = configuration,
      Loggers = new string[]
      {
        $"trx;LogFileName={project.GetFilenameWithoutExtension()}.trx",
        $"html;LogFileName={project.GetFilenameWithoutExtension()}.html",
        $"junit;LogFileName={project.GetFilenameWithoutExtension()}.xml",
      },
      ResultsDirectory = artifactsDirectory,
    });
  });

Task("Pack")
  .Description("Creates NuGet packages and outputs them to the artifacts directory.")
  .Does(() =>
  {
    DotNetPack(".", new DotNetPackSettings()
    {
      Configuration = configuration,
      IncludeSymbols = true,
      MSBuildSettings = new DotNetMSBuildSettings()
      {
        ContinuousIntegrationBuild = !BuildSystem.IsLocalBuild,
      },
      NoBuild = true,
      NoRestore = true,
      OutputDirectory = artifactsDirectory,
    });
  });

Task("CI")
  .Description("Runs the default target in CI mode.")
  .IsDependentOn("Build")
  .IsDependentOn("Pack");

Task("Default")
  .Description("Cleans, restores NuGet packages, builds the solution, runs unit tests and then creates NuGet packages.")
  .IsDependentOn("CI")
  .IsDependentOn("Test");

RunTarget(target);