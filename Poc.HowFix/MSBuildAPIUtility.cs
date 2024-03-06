// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System.Globalization;

namespace Poc.HowFix;

internal class MSBuildAPIUtility
{
    private const string PACKAGE_REFERENCE_TYPE_TAG = "PackageReference";
    private const string RESTORE_STYLE_TAG = "RestoreProjectStyle";
    private const string NUGET_STYLE_TAG = "NuGetProjectStyle";
    private const string ASSETS_FILE_PATH_TAG = "ProjectAssetsFile";

    internal static string GetProjectName(string projectCSProjPath)
    {
        return GetProject(projectCSProjPath).GetPropertyValue("MSBuildProjectName");
    }

    /// <summary>
    /// A simple check for some of the evaluated properties to check
    /// if the project is package reference project or not
    /// </summary>
    internal static bool IsPackageReferenceProject(string projectCSProjPath)
    {
        Project project = GetProject(projectCSProjPath);
        return (project.GetPropertyValue(RESTORE_STYLE_TAG) == "PackageReference" ||
                project.GetItems(PACKAGE_REFERENCE_TYPE_TAG).Count != 0 ||
                project.GetPropertyValue(NUGET_STYLE_TAG) == "PackageReference" ||
                project.GetPropertyValue(ASSETS_FILE_PATH_TAG) != "");
    }

    internal static string GetAssetsFilePath(string projectCSProjPath)
    {
        return GetProject(projectCSProjPath).GetPropertyValue("ProjectAssetsFile");
    }

    /// <summary>
    /// Opens an MSBuild.Evaluation.Project type from a csproj file.
    /// </summary>
    private static Project GetProject(string projectCSProjPath)
    {
        var projectRootElement = TryOpenProjectRootElement(projectCSProjPath);
        if (projectRootElement == null)
        {
            //throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Strings.Error_MsBuildUnableToOpenProject, projectCSProjPath));
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
