Param([string] $configuration = "Release")
$ErrorActionPreference = "Stop"
$SolutionDirectory = (Get-Item $PSScriptRoot).Parent.FullName

Import-Module ./build/exechelper.ps1

# Install .NET tooling
exec ./build/dotnet-cli-install.ps1

exec "dotnet" "test EPiServer.Labs.ContentManager.sln -c $configuration -l trx --no-build --verbosity normal"

Pop-Location