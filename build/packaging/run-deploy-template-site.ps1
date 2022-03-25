Param(
    $SiteName = "FROM_TC_BUILD",
    $SitePath = "FROM_TC_BUILD",
    $PackageName = "Alloy.Sample.TinyMce",
    $PackageVersion = "FROM_TC_BUILD",
    [bool]$DeleteSite = $true,
    [bool]$CreateSite = $true,
    $NugetFeed = "http://tc01.ep.se/httpAuth/app/nuget/feed/Netcore/netcore/v1",
    $DbServer = "(local)",
    $DbUsername = "Deployer",
    $DbPassword = "aj3YpcmkDVEuLSiL",
    $DbSiteUser = "episerver-site",
    $DbSitePassword = "PLeg3BiD9-uJMkpY"
)

$SiteName = $SiteName -replace "\W+", "-"
$SiteName = "netcore-blockenhancements-$SiteName"
$SiteName = $SiteName.subString(0, [System.Math]::Min(63, $SiteName.Length))
$SitePath = "e:\netcore-blockenhancements\$SiteName"
Write-Host "DEPLOYING ALLOY :: START :: $SiteName :: to :: $SitePath"

$tmpFolder = [System.IO.Path]::GetTempPath() + [guid]::NewGuid().ToString()
$tmpPackageFolder = "$tmpFolder\$PackageName.$PackageVersion"

$FrameworkDir = $([System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory())
Set-Alias aspnet_regsql (Join-Path $FrameworkDir "aspnet_regsql.exe")

#http://www.iis.net/learn/manage/powershell/powershell-snap-in-creating-web-sites-web-applications-virtual-directories-and-application-pools
Import-Module WebAdministration
Import-Module sqlps -DisableNameChecking
[System.Reflection.Assembly]::LoadWithPartialName("EPiServerInstall.Common.1") | Import-Module  -DisableNameChecking

function Delete-Site {
    param (
        $SiteName,
        $SitePath
    )

    $site = "IIS:\Sites\$SiteName"
    $appPoolName = "IIS:\AppPools\$SiteName"

    if(Test-Path ($appPoolName)) {
        "Stopping $appPoolName"
        Stop-WebItem $appPoolName -Passthru
    }

    Function DeleteIfExists($path) {
        if(Test-Path($path)) {
            "Deleting: $path"
            Remove-Item $path -Recurse
        }
    }

    DeleteIfExists $site
    DeleteIfExists $appPoolName
    DeleteIfExists $SitePath
}

function Create-Site {
    param (
        $SiteName,
        $SitePath
    )


    $site = "IIS:\Sites\$SiteName"
    $appPoolName = "IIS:\AppPools\$SiteName"

    if(!(Test-Path($SitePath))) {
        "Creating folder: $SitePath"
        md $SitePath
        "Creating folder: $SitePath\AppData"
        md "$Sitepath\AppData"
    }

    if(!(Test-Path($site))) {
        "Creating site: $site"
        New-Item $site -bindings @{protocol="http";bindingInformation=":80:$SiteName.sh-daily.ep.se"} -physicalPath "$SitePath\wwwroot"

        if(!(Test-Path($appPoolName)))
        {
            "Creating Application pool: $appPoolName"
            $appPool = New-Item ($appPoolName)
            $appPool.managedRuntimeVersion = "v4.0"
            $appPool | Set-Item
        }

        #Set the app pool on the site
        "Set $appPoolName as the application pool for $site"
        Set-ItemProperty $site -name applicationPool -value $SiteName
    } else {
        "Site $site already exists"
    }
}

function Deploy-Nuget {
    param (
        $TmpFolder,
        $TmpPackageFolder,
        $PackageName,
        $PackageVersion,
        $SitePath,
        $NugetFeed
    )

    $tmpPackageFolderSearch = "$TmpPackageFolder\*"

    #Install the nuget package into the $tmpFolder
    $command = "nuget install $PackageName -Version $PackageVersion -Prerelease -OutputDirectory $tmpFolder -Source $NugetFeed -NoCache"
    "Running nuget command $command"

    nuget install $PackageName -Version $PackageVersion -Prerelease -OutputDirectory $tmpFolder -Source $NugetFeed -NoCache

    "Copy the site from $tmpPackageFolderSearch to $SitePath"
    Copy-Item $tmpPackageFolderSearch -Destination $SitePath -Recurse
}

function Drop-Database {
    param (
        $SiteName,
        $DbServer,
        $DbUsername,
        $DbPassword
    )

    "Drop database [$SiteName]"
    Invoke-SqlCmd -Query "ALTER DATABASE [$SiteName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP database [$SiteName]" -ServerInstance $DbServer -Username $DbUserName -Password $DbPassword -ErrorAction SilentlyContinue
}

function Create-Database {
    param (
        $TmpPackageFolder,
        $SiteName,
        $DbServer,
        $DbUsername,
        $DbPassword,
        $DbSiteUser,
        $DbSitePassword
    )

    "Create database [$SiteName]"
    Invoke-SqlCmd -Query "CREATE database [$SiteName]" -ServerInstance $DbServer -Username $DbUserName -Password $DbPassword

    "Set db_owner [$dbSiteUser]"
    Invoke-SqlCmd -Query "USE [$SiteName]; EXEC sp_adduser '$DbSiteUser', '$DbSiteUser', 'db_owner'" -ServerInstance $DbServer -Username $DbUserName -Password $DbPassword

    "Register aspnet membership sql"
    aspnet_regsql -S $DbServer -U $DbUsername -P $DbPassword -d $SiteName -A all

    "Create tables"
    $rawSqlScript = Get-ChildItem -Path $TmpPackageFolder\setup\sql\**\EPiServer.Cms.Core.sql -Recurse
    $sqlScript = "$TmpPackageFolder\setup\CreateDatabase.sql"
    Add-Content -Path $sqlScript -Value (Get-Content $rawSqlScript.FullName)

    $var1 = "DBName=$SiteName"
    $varCollection = $var1
    Invoke-Sqlcmd -InputFile $sqlScript -Variable $varCollection -ServerInstance $DbServer -Username $DbUserName -Password $DbPassword

}

function Transform-Config {
    param (
        $TmpPackageFolder,
        $SitePath,
        $DbServer,
        $SiteName,
        $DbSiteUser,
        $DbSitePassword
    )

    "Transform config files"
    $appSettingsFile = Get-Content "$SitePath\wwwroot\appsettings.Production.json"
    $appSettingsFile = $appSettingsFile -replace [regex]::Escape('{DbServer}'), $DbServer
    $appSettingsFile = $appSettingsFile -replace [regex]::Escape('{DbDatabase}'), $SiteName
    $appSettingsFile = $appSettingsFile -replace [regex]::Escape('{DbUserName}'), $DbSiteUser
    $appSettingsFile = $appSettingsFile -replace [regex]::Escape('{DbPassword}'), $DbSitePassword

    Set-Content -Path "$SitePath\wwwroot\appsettings.Production.json" -Value $appSettingsFile
}

if ($DeleteSite -eq $true) {
    Delete-Site $SiteName $SitePath
    Drop-Database $SiteName $DbServer $DbUsername $DbPassword
}

if ($CreateSite -eq $true) {
    Create-Site  $SiteName $SitePath
    Deploy-Nuget $tmpFolder $tmpPackageFolder $PackageName $PackageVersion $SitePath $NugetFeed
    Create-Database $tmpPackageFolder $SiteName $DbServer $DbUsername $DbPassword $DbSiteUser $DbSitePassword
    Transform-Config $tmpPackageFolder $SitePath $DbServer $SiteName $DbSiteUser $DbSitePassword

    "Copy license file"
    Copy-Item "E:\License.config" -Destination "$SitePath\wwwroot"

    "Remove temp folder $tmpFolder"
    Remove-Item $tmpFolder -Recurse
}

Write-Host "DEPLOYING ALLOY :: END"
