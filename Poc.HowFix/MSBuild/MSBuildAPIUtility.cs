// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using NuGet.CommandLine.XPlat;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Poc.HowFix.Models;
using System.Globalization;

namespace Poc.HowFix.MSBuild;

internal class MSBuildAPIUtility
{
    private const string PACKAGE_VERSION_TYPE_TAG = "PackageVersion";
    private const string CollectPackageReferences = "CollectPackageReferences";

    internal static Models.PackageDependency Resolve(LockFile assetsFile)
    {
        // Filtering the Targets to ignore TargetFramework + RID combination, only keep TargetFramework in requestedTargets.
        // So that only one section will be shown for each TFM.
        var target = assetsFile.Targets.Where(t => t.RuntimeIdentifier == null).Single();
        var packageLibraries = target.Libraries.Where(l => l.Type == "package");
        var projectLibraries = target.Libraries.Where(l => l.Type == "project");
        var rootDependencies = assetsFile.PackageSpec.TargetFrameworks.Single(tfm => tfm.FrameworkName.Equals(target.TargetFramework)).Dependencies;
        var dependenciesCache = new Dictionary<string, Models.PackageDependency>();
        var projectsCache = new Dictionary<string, Models.Project>();

        var root = new Models.Project {
            Name = assetsFile.PackageSpec.Name,
            ResolvedVersion = assetsFile.PackageSpec.Version
        };

        foreach(var projectDependency in rootDependencies)
        {
            root.AddPackageReference(GetPackageDependency(projectDependency.Name), projectDependency.LibraryRange.VersionRange!);
        }

        foreach(var p in projectLibraries)
        {
            var project = GetProject(p.Name);
            root.AddProjectReference(project);
        }

        return root;

        Models.Project GetProject(string name)
        {
            if (projectsCache.TryGetValue(name, out var projectFromCache))
            {
                return projectFromCache;
            }

            var foundLibraries = projectLibraries.Where(l => l.Name!.Equals(name));
            if (!foundLibraries.Any())
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "No project with the name `{0}` found in the assets file.", name));
            }

            if (foundLibraries.Count() > 1)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Multiple projects with the same name `{0}` found in the assets file.", name));
            }

            var projectLibrary = projectLibraries.Single(l => l.Name!.Equals(name));

            var project = new Models.Project
            {
                Name = projectLibrary.Name,
                ResolvedVersion = projectLibrary.Version
            };
            projectsCache.Add(name, project);
            foreach (var projectDependency in projectLibrary.Dependencies)
            {
                if (projectLibraries.Any(l => l.Name == projectDependency.Id))
                {
                    project.AddProjectReference(GetProject(projectDependency.Id));
                }
                else
                {
                    project.AddPackageReference(GetPackageDependency(projectDependency.Id), projectDependency.VersionRange);
                }
            }
            return project;
        }

        Models.PackageDependency GetPackageDependency(string name)
        {
            if(dependenciesCache.TryGetValue(name, out var dependency))
            {
                return dependency;
            }

            var foundLibraries = packageLibraries.Where(l => l.Name!.Equals(name));
            if (!foundLibraries.Any())
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "No library with the name `{0}` found in the assets file.", name));
            }

            if (foundLibraries.Count() > 1)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Multiple libraries with the same name `{0}` found in the assets file.", name));
            }

            var library = packageLibraries.Single<LockFileTargetLibrary>(l => l.Name!.Equals(name));
            var dep = new Models.PackageDependency {
                Name = library.Name!,
                ResolvedVersion = library.Version!
            };
            dependenciesCache.Add(name, dep);
            foreach(var d in library.Dependencies)
            {
                var subDep = GetPackageDependency(d.Id);
                dep.AddPackageReference(subDep, d.VersionRange);
            }
            return dep;
        }
    }

    internal static List<FrameworkPackages> GetResolvedVersions(
        ProjectWrapper project, IEnumerable<string> userInputFrameworks, LockFile assetsFile, bool transitive)
    {
        if (userInputFrameworks == null)
        {
            throw new ArgumentNullException(nameof(userInputFrameworks));
        }

        if (project == null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (assetsFile == null)
        {
            throw new ArgumentNullException(nameof(assetsFile));
        }

        var projectPath = project.FullPath;
        var resultPackages = new List<FrameworkPackages>();
        var requestedTargetFrameworks = assetsFile.PackageSpec.TargetFrameworks;
        var requestedTargets = assetsFile.Targets;

        // If the user has entered frameworks, we want to filter
        // the targets and frameworks from the assets file
        if (userInputFrameworks.Any())
        {
            //Target frameworks filtering
            var parsedUserFrameworks = userInputFrameworks.Select(f =>
                                           NuGetFramework.Parse(f.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray()[0]));
            requestedTargetFrameworks = requestedTargetFrameworks.Where(tfm => parsedUserFrameworks.Contains(tfm.FrameworkName)).ToList();

            //Assets file targets filtering by framework and RID
            var filteredTargets = new List<LockFileTarget>();
            foreach (var frameworkAndRID in userInputFrameworks)
            {
                var splitFrameworkAndRID = frameworkAndRID.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                // If a / is not present in the string, we get all of the targets that
                // have matching framework regardless of RID.
                if (splitFrameworkAndRID.Count() == 1)
                {
                    filteredTargets.AddRange(requestedTargets.Where(target => target.TargetFramework.Equals(NuGetFramework.Parse(splitFrameworkAndRID[0]))));
                }
                else
                {
                    //RID is present in the user input, so we filter using it as well
                    filteredTargets.AddRange(requestedTargets.Where(target => target.TargetFramework.Equals(NuGetFramework.Parse(splitFrameworkAndRID[0])) &&
                                                                              target.RuntimeIdentifier != null && target.RuntimeIdentifier.Equals(splitFrameworkAndRID[1], StringComparison.OrdinalIgnoreCase)));
                }
            }
            requestedTargets = filteredTargets;
        }

        // Filtering the Targets to ignore TargetFramework + RID combination, only keep TargetFramework in requestedTargets.
        // So that only one section will be shown for each TFM.
        requestedTargets = requestedTargets.Where(target => target.RuntimeIdentifier == null).ToList();

        foreach (var target in requestedTargets)
        {
            // Find the tfminformation corresponding to the target to
            // get the top-level dependencies
            TargetFrameworkInformation tfmInformation;
            try
            {
                tfmInformation = requestedTargetFrameworks.First(tfm => tfm.FrameworkName.Equals(target.TargetFramework));
            }
            catch (Exception)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Unable to read the assets file `{0}`. Please make sure the file has the write format.", assetsFile.Path));
            }

            //The packages for the framework that were retrieved with GetRequestedVersions
            var frameworkDependencies = tfmInformation.Dependencies;
            var projectPackages = GetPackageReferencesFromTargets(projectPath, tfmInformation.ToString());
            var topLevelPackages = new List<InstalledPackageReference>();
            var transitivePackages = new List<InstalledPackageReference>();

            foreach (var library in target.Libraries)
            {
                var matchingPackages = frameworkDependencies.Where(d =>
                    d.Name.Equals(library.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                var resolvedVersion = library.Version.ToString();

                //In case we found a matching package in requestedVersions, the package will be
                //top level.
                if (matchingPackages.Any())
                {
                    var topLevelPackage = matchingPackages.Single();
                    InstalledPackageReference installedPackage = default;

                    //If the package is not auto-referenced, get the version from the project file. Otherwise fall back on the assets file
                    if (!topLevelPackage.AutoReferenced)
                    {
                        try
                        { // In case proj and assets file are not in sync and some refs were deleted
                            var projectPackage = projectPackages.Where(p => p.Name.Equals(topLevelPackage.Name, StringComparison.Ordinal)).First();

                            // If the project is using CPM and it's not using VersionOverride, get the version from Directory.Package.props file
                            if (assetsFile.PackageSpec.RestoreMetadata.CentralPackageVersionsEnabled && !projectPackage.IsVersionOverride)
                            {
                                ProjectRootElement directoryBuildPropsRootElement = project.GetDirectoryBuildPropsRootElement();
                                ProjectItemElement packageInCPM = directoryBuildPropsRootElement.Items.Where(i => (i.ItemType == PACKAGE_VERSION_TYPE_TAG || i.ItemType.Equals("GlobalPackageReference")) && i.Include.Equals(topLevelPackage.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                installedPackage = new InstalledPackageReference(topLevelPackage.Name)
                                {
                                    OriginalRequestedVersion = packageInCPM.Metadata.FirstOrDefault(i => i.Name.Equals("Version", StringComparison.OrdinalIgnoreCase)).Value,
                                };
                            }
                            else
                            {
                                installedPackage = projectPackage;
                            }
                        }
                        catch (Exception)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Strings.ListPkg_ErrorReadingReferenceFromProject", projectPath));
                        }
                    }
                    else
                    {
                        var projectFileVersion = topLevelPackage.LibraryRange.VersionRange.ToString();
                        installedPackage = new InstalledPackageReference(library.Name)
                        {
                            OriginalRequestedVersion = projectFileVersion
                        };
                    }

                    installedPackage.ResolvedPackageMetadata = PackageSearchMetadataBuilder
                        .FromIdentity(new PackageIdentity(library.Name, library.Version))
                        .Build();

                    installedPackage.AutoReference = topLevelPackage.AutoReferenced;

                    if (library.Type != "project")
                    {
                        topLevelPackages.Add(installedPackage);
                    }
                }
                // If no matching packages were found, then the package is transitive,
                // and include-transitive must be used to add the package
                else if (transitive) // be sure to exclude "project" references here as these are irrelevant
                {
                    var installedPackage = new InstalledPackageReference(library.Name)
                    {
                        ResolvedPackageMetadata = PackageSearchMetadataBuilder
                            .FromIdentity(new PackageIdentity(library.Name, library.Version))
                            .Build()
                    };

                    if (library.Type != "project")
                    {
                        transitivePackages.Add(installedPackage);
                    }
                }
            }

            var frameworkPackages = new FrameworkPackages(
                target.TargetFramework.GetShortFolderName(),
                topLevelPackages,
                transitivePackages);

            resultPackages.Add(frameworkPackages);
        }

        return resultPackages;
    }

    /// <summary>
    /// Returns all package references after invoking the target CollectPackageReferences.
    /// </summary>
    /// <param name="projectPath"> Path to the project for which the package references have to be obtained.</param>
    /// <param name="framework">Framework to get reference(s) for</param>
    /// <returns>List of Items containing the package reference for the package.
    /// If the libraryDependency is null then it returns all package references</returns>
    private static IEnumerable<InstalledPackageReference> GetPackageReferencesFromTargets(string projectPath, string framework)
    {
        var globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                { { "TargetFramework", framework } };
        var newProject = new ProjectInstance(projectPath, globalProperties, null);
        newProject.Build(new[] { CollectPackageReferences }, new List<Microsoft.Build.Framework.ILogger> { }, out var targetOutputs);

        return targetOutputs.First(e => e.Key.Equals(CollectPackageReferences, StringComparison.OrdinalIgnoreCase)).Value.Items.Select(p =>
        {
            var isVersionOverride = p.GetMetadata("VersionOverride") != string.Empty;
            var originalRequestedVersion = isVersionOverride ? p.GetMetadata("VersionOverride") : p.GetMetadata("version");
            return new InstalledPackageReference(p.ItemSpec)
            {
                OriginalRequestedVersion = originalRequestedVersion,
                IsVersionOverride = isVersionOverride,
            };
        });
    }
}
