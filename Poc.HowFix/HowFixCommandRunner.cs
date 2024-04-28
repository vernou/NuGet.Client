// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.CommandLine.XPlat;
using NuGet.ProjectModel;
using Poc.HowFix.MSBuild;

namespace Poc.HowFix;

internal class HowFixCommandRunner
{
    public async Task<int> ExecuteCommandAsync(string csproj)
    {
        var project = new ProjectWrapper(csproj);
        var projectName = project.ProjectName;

        Console.WriteLine("projectName:" + projectName);

        if (!project.IsPackageReferenceProject)
        {
            throw new InvalidOperationException(
                $"""
                The project `{projectName}` uses package.config for NuGet packages, while the command works only with package reference projects.
                """);
        }

        var assetsPath = project.AssetsFilePath;

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

        // Get all the packages that are referenced in a project
        List<FrameworkPackages> frameworks;
        try
        {
            frameworks = MSBuildAPIUtility.GetResolvedVersions(project, [], assetsFile, true);
        }
        catch (InvalidOperationException ex)
        {
            //projectModel.AddProjectInformation(ProblemType.Error, ex.Message);
            return 1;
        }

        return 0;
    }
}
