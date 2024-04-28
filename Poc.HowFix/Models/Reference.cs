// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Poc.HowFix.Models;

internal class Reference
{
    public required string Name { get; init; }
    public required ReferenceType Type { get; init; }
    public required string Version { get; init; }
    public required string ResolvedVersion { get; init; }
    public required IEnumerable<Reference> Dependencies { get; init; }
}

public enum ReferenceType
{
    Project,
    Package
}
