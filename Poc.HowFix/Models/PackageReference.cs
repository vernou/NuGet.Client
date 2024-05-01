// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Versioning;

namespace Poc.HowFix.Models;

internal class PackageReference
{
    public required PackageDependency Dependency { get; init; }
    public required VersionRange Version { get; init; }
}

internal class PackageDependency
{
    public required string Name { get; init; }
    public required SemanticVersion ResolvedVersion { get; init; }
    public IEnumerable<PackageReference> PackageReferences => _packageReferences;
    private List<PackageReference> _packageReferences = new();

    public void AddPackageReference(PackageDependency dependency, VersionRange version)
    {
        _packageReferences.Add(new PackageReference {
            Dependency = dependency,
            Version = version
        });
    }
}

internal class Project : PackageDependency
{
    public IEnumerable<Project> ProjectReference => _projectReferences;
    private List<Project> _projectReferences = new();

    public void AddProjectReference(Project project)
    {
        _projectReferences.Add(project);
    }
}
