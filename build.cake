var target = Argument("Target", "Default");
var configuration =
  HasArgument("Configuration") ? Argument<string>("Configuration") :
  EnvironmentVariable("Configuration", "Release");

var artifactsDirectory = Directory("./Artifacts");

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

Task("Default")
  .Description("Cleans, restores NuGet packages, builds the solution, runs unit tests and then creates NuGet packages.")
  .IsDependentOn("Build")
  .IsDependentOn("Pack");

RunTarget(target);