// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Construction;
using System.Globalization;
using Microsoft.Build.Evaluation;
using NuGet.Common;

namespace Poc.HowFix.MSBuild;

internal class ProjectWrapper
{
    private const string PACKAGE_REFERENCE_TYPE_TAG = "PackageReference";
    private const string RESTORE_STYLE_TAG = "RestoreProjectStyle";
    private const string NUGET_STYLE_TAG = "NuGetProjectStyle";
    private const string ASSETS_FILE_PATH_TAG = "ProjectAssetsFile";
    private const string PACKAGE_VERSION_TYPE_TAG = "PackageVersion";
    /// <summary>
    /// The name of the MSBuild property that represents the path to the central package management file,
    /// usually Directory.Packages.props.
    /// </summary>
    private const string DirectoryPackagesPropsPathPropertyName = "DirectoryPackagesPropsPath";

    private Lazy<Project> _project;

    public ProjectWrapper(string csprojPath)
    {
        _project = new Lazy<Project>(() => GetProject(csprojPath));
    }

    private Project Project => _project.Value;
    public string ProjectName => Project.GetPropertyValue("MSBuildProjectName");
    public string FullPath => Project.FullPath;
    public bool IsPackageReferenceProject => Project.GetPropertyValue(RESTORE_STYLE_TAG) == "PackageReference" ||
                Project.GetItems(PACKAGE_REFERENCE_TYPE_TAG).Count != 0 ||
                Project.GetPropertyValue(NUGET_STYLE_TAG) == "PackageReference" ||
                Project.GetPropertyValue(ASSETS_FILE_PATH_TAG) != "";
    public string AssetsFilePath => Project.GetPropertyValue("ProjectAssetsFile");

    /// <summary>
    /// Get the Directory build props root element for projects onboarded to CPM.
    /// </summary>
    /// <returns>The directory build props root element.</returns>
    internal ProjectRootElement GetDirectoryBuildPropsRootElement()
    {
        // Get the Directory.Packages.props path.
        var directoryPackagesPropsPath = Project.GetPropertyValue(DirectoryPackagesPropsPathPropertyName);
        ProjectRootElement directoryBuildPropsRootElement = Project.Imports.FirstOrDefault(i => i.ImportedProject.FullPath.Equals(directoryPackagesPropsPath, PathUtility.GetStringComparisonBasedOnOS())).ImportedProject;
        return directoryBuildPropsRootElement;
    }

    private static Project GetProject(string projectCSProjPath)
    {
        var projectRootElement = TryOpenProjectRootElement(projectCSProjPath);
        if(projectRootElement is null)
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
        catch(Microsoft.Build.Exceptions.InvalidProjectFileException)
        {
            return null;
        }
    }
}
