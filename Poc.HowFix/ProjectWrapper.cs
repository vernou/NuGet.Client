// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Evaluation;

namespace Poc.HowFix;

internal class ProjectWrapper
{
    private Lazy<Project> _project;

    public ProjectWrapper(string csprojPath)
    {
        _project = new Lazy<Project>(() => MSBuildAPIUtility.GetProject(csprojPath));
    }

    private Project Project => _project.Value;
    public string ProjectName => Project.GetPropertyValue("MSBuildProjectName");
    public bool IsPackageReferenceProject => (Project.GetPropertyValue(MSBuildAPIUtility.RESTORE_STYLE_TAG) == "PackageReference" ||
                Project.GetItems(MSBuildAPIUtility.PACKAGE_REFERENCE_TYPE_TAG).Count != 0 ||
                Project.GetPropertyValue(MSBuildAPIUtility.NUGET_STYLE_TAG) == "PackageReference" ||
                Project.GetPropertyValue(MSBuildAPIUtility.ASSETS_FILE_PATH_TAG) != "");
    public string AssetsFilePath => Project.GetPropertyValue("ProjectAssetsFile");
}
