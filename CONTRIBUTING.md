Atlas Provider for Entity Framework Core
=======================================

### Project Structure

The project is structured as follows:

```
.
├── artifacts # Build artifacts (e.g. binaries, NuGet packages)
├── src
│   ├── Atlas.Provider.Core    # main commands for the provider, help generate SQL scripts
│   └── Atlas.Provider.Loader  # help to build contexts to run main commands
└── test

```


### Local development

##### Prerequisites

- .NET SDK and runtime (https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

The idea is Atlas.Provider.Loader will be executed and invokes the main commands (Atlas.Provider.Core) to generate the scripts.

![How it works](/docs/howitworks.jpg)

Atlas.Provider.Loader and Atlas.Provider.Core are two separate projects, so we can run them separately.

#### Steps to run the project:

1. Clone the repository

```
$ git clone git@github.com:ariga/atlas-provider-ef.git
```

2. Navigate into the `Atlas.Provider.Loader` project

```
$ pwd
atlas-provider-ef-demo/src/Atlas.Provider.Loader
```

3. Change the build config for Atlas.Provider.Loader.csproj and Atlas.Provider.Loader.nuspec if needed.
4. Run the `Atlas.Provider.Loader` project (same for `Atlas.Provider.Core`)

```
$ dotnet restore
... 
$ dotnet run
```

#### Steps to build nuget package:

1. Navigate into the `Atlas.Provider.Loader` project
2. Build the project first, this also builds the `Atlas.Provider.Core` project and put the binaries in the `artifacts` folder

```
$ dotnet clean && dotnet build --configuration Release
```

3. Run the `pack` command to build the nuget package

```
$ dotnet pack --configuration Release
```

You can change the configuration for building the nuget package by modifying `src/Atlas.Provider.Loader/Atlas.Provider.Loader.nuspec` file

#### Run the built nuget package

Full documentation on how to install a local tool can be found here: https://learn.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use

###### Prerequisites

- A project that uses EF Core, you can use this sample project: https://github.com/peter-parker-inc/atlas-provider-ef-demo/tree/current-existing-project

Steps to use the built nuget package as a local tool:

1. Navigate to the demo project

```
$ pwd
atlas-provider-ef-demo/Atlas.Provider.Demo
```

Install dependencies

```
$ dotnet restore
```

2. Create a tool manifest file, this is a file that tells the dotnet cli where to find the tool

```
$ dotnet new tool-manifest
```

3. Add the tool to the manifest file

```
$ dotnet tool install --add-source ./artifacts/nupkg atlas-ef --version {version}
```

4. Run the tool

```
$ dotnet tool run atlas-ef
```
