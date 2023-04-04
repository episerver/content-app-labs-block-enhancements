Param([string]$version, [string] $configuration = "Release")
$ErrorActionPreference = "Stop"
$workingDirectory = Get-Location
$zip = "$workingDirectory\packages\7-Zip.CommandLine\18.1.0\tools\7za.exe"

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

#cleanup all by dtk folder which is used by tests
Get-ChildItem -Path out\ -Exclude dtk | Remove-Item -Recurse -Force

#copy assets CM
Copy-Item -Path src\EPiServer.Labs.BlockEnhancements\ClientResources\ -Destination out\episerver-labs-block-enhancements\$version\ClientResources -recurse -Force
Copy-Item src\EPiServer.Labs.BlockEnhancements\module.config out\episerver-labs-block-enhancements
((Get-Content -Path out\episerver-labs-block-enhancements\module.config -Raw).TrimEnd() -Replace '=""', "=`"$version`"" ) | Set-Content -Path out\episerver-labs-block-enhancements\module.config
Set-Location $workingDirectory\out\episerver-labs-block-enhancements
Start-Process -NoNewWindow -Wait -FilePath $zip -ArgumentList "a", "episerver-labs-block-enhancements.zip", "$version", "module.config"

Set-Location $workingDirectory\
exec "dotnet" "publish --no-restore --no-build -c $configuration src\\Alloy.Sample\\Alloy.Sample.csproj"

# Packaging public packages
exec "dotnet" "pack --no-restore --no-build -c $configuration /p:PackageVersion=$version /p:CmsCoreVersion=$cmsCoreVersion /p:CmsUIVersion=$cmsUIVersion /p:CmsUINextMajorVersion=$cmsUINextMajorVersion episerver-labs-block-enhancements.sln"

Pop-Location
