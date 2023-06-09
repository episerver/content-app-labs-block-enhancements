Param([string]$version, [string] $configuration = "Release")
$ErrorActionPreference = "Stop"
$workingDirectory = Get-Location

# Set location to the Solution directory
(Get-Item $PSScriptRoot).Parent.FullName | Push-Location

Import-Module .\build\exechelper.ps1

# Install .NET tooling
exec .\build\dotnet-cli-install.ps1

[xml] $versionFile = Get-Content ".\build\dependencies.props"
# CMS dependency
$cmsUINode = $versionFile.SelectSingleNode("Project/PropertyGroup/CmsUIVersion")
$cmsUIVersion = $cmsUINode.InnerText
$cmsUIParts = $cmsUIVersion.Split(".")
$cmsUIMajor = [int]::Parse($cmsUIParts[0]) + 1
$cmsUINextMajorVersion = ($cmsUIMajor.ToString() + ".0.0")
# CMS Core dependency
$cmsCoreNode = $versionFile.SelectSingleNode("Project/PropertyGroup/CmsCoreVersion")
$cmsCoreVersion = $cmsCoreNode.InnerText

Set-Location $workingDirectory\
exec "dotnet" "publish --no-restore --no-build -c $configuration src\\Alloy.Sample\\Alloy.Sample.csproj"

# Packaging public packages
exec "dotnet" "pack --no-restore --no-build -c $configuration /p:PackageVersion=$version /p:CmsCoreVersion=$cmsCoreVersion /p:CmsUIVersion=$cmsUIVersion /p:CmsUINextMajorVersion=$cmsUINextMajorVersion episerver-labs-block-enhancements.sln"

Pop-Location
