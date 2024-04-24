// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System.Globalization;

namespace Poc.HowFix;

internal class MSBuildAPIUtility
{
    public const string PACKAGE_REFERENCE_TYPE_TAG = "PackageReference";
    public const string RESTORE_STYLE_TAG = "RestoreProjectStyle";
    public const string NUGET_STYLE_TAG = "NuGetProjectStyle";
    public const string ASSETS_FILE_PATH_TAG = "ProjectAssetsFile";

    /// <summary>
    /// Opens an MSBuild.Evaluation.Project type from a csproj file.
    /// </summary>
    internal static Project GetProject(string projectCSProjPath)
    {
        var projectRootElement = TryOpenProjectRootElement(projectCSProjPath);
        if (projectRootElement is null)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Error_MsBuildUnableToOpenProject", projectCSProjPath));
        }
        return new Project(projectRootElement, null, null, new ProjectCollection());
    }

    private static ProjectRootElement? TryOpenProjectRootElement(string filename)
    {
        try
        {
            // There is ProjectRootElement.TryOpen but it does not work as expected
            // I.e. it returns null for some valid projects
            return ProjectRootElement.Open(filename, ProjectCollection.GlobalProjectCollection, preserveFormatting: true);
        }
        catch (Microsoft.Build.Exceptions.InvalidProjectFileException)
        {
            return null;
        }
    }
}
