using NuProTypes = NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using Poc.HowFix;

// Init MSBuild with the version of Visual Studio Build Tools
var instance = Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(instance => instance.Version).First();
Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);

//var csproj = """C:\Projets\Azure\Bloc applicatif - Cockpit-it\Sources\Back\Back.Api\Back.Api.csproj""";
//var csproj = """C:\repos\vrac\Vernou.WebApi\Vernou.WebApi\Vernou.WebApi.csproj""";
var csproj = """C:\t\HowFix.Demo\HowFix.Demo\HowFix.Demo.csproj""";

await new HowFixCommandRunner().ExecuteCommandAsync(csproj);


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
