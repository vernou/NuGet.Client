// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.Build.Locator;

namespace NuGet.DotnetTool
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();
            return NuGet.CommandLine.XPlat.Program.MainInternal(args, "nugetcdk");
        }
    }
}
