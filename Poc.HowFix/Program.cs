using NuProTypes = NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using Poc.HowFix;
using Microsoft.Build.Evaluation;
using System.Globalization;
using NuGet.ProjectModel;

// Init MSBuild with the version of Visual Studio Build Tools
var instance = Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(instance => instance.Version).First();
Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);

var csproj = """C:\Projets\Azure\Bloc applicatif - Cockpit-it\Sources\Back\Back.Api\Back.Api.csproj""";
var projectName = MSBuildAPIUtility.GetProjectName(csproj);

Console.WriteLine("projectName:" + projectName);

if (!MSBuildAPIUtility.IsPackageReferenceProject(csproj))
{
    throw new InvalidOperationException(
        $"""
        The project `{projectName}` uses package.config for NuGet packages, while the command works only with package reference projects.
        """);
}

var assetsPath = MSBuildAPIUtility.GetAssetsFilePath(csproj);

if (!File.Exists(assetsPath))
{
    throw new InvalidOperationException($"No assets file was found for `{projectName}`. Please run restore before running this command.");
}

var lockFileFormat = new LockFileFormat();
LockFile assetsFile = lockFileFormat.Read(assetsPath);

// Assets file validation
if (assetsFile.PackageSpec == null ||
    assetsFile.Targets == null ||
    assetsFile.Targets.Count == 0)
{
    throw new InvalidOperationException($"Unable to read the assets file `{assetsPath}`. Please make sure the file has the write format.");
}



// Create a source repository
var repository = NuProTypes.Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

// Create a PackageMetadataResource
var packageMetadataResource = await repository.GetResourceAsync<NuProTypes.PackageMetadataResource>();

// Get the package metadata for a specific package
var packages = await packageMetadataResource.GetMetadataAsync(
    "Microsoft.Data.SqlClient",
    false,
    false,
    new NuProTypes.SourceCacheContext(),
    new NuGet.Common.NullLogger(),
    CancellationToken.None);

foreach (var packageMetadata in packages)
{
    Console.WriteLine(packageMetadata.Identity);
    Console.WriteLine(packageMetadata.Vulnerabilities?.Count() ?? 0);
}
