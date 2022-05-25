Param([String]$WindowsSDKPath)

if(!$WindowsSDKPath) {
    Write-Host "WindowsSDKPath is a required parameter"
    exit 1
}
Write-Host "SDK tools path: " + $WindowsSDKPath

$sn = $WindowsSDKPath + "\sn.exe"
if (!(Test-Path $sn)) {
    Write-Error ($sn + " not found")
    exit 1
}

function Get-Cert()
{
    # First attempt to get cert based on environment variable
    if (Test-Path env:EPISERVER_CS_CERTIFICATE_THUMBPRINT)
    {
        $certFingerprint = (Get-Item env:EPISERVER_CS_CERTIFICATE_THUMBPRINT).Value
    }
    else
    {
        # If environment variable is not defined, try new (27th of May 2019) cert fingerprint
        $certFingerprint = "EABEA65470D9B25F92B09C202EB3DED15FD0B0A9"
    }

    $certificates = Get-ChildItem -Recurse -File cert:\ | Where-Object {$_.Thumbprint -match $certFingerprint}
    if ($certificates -eq $null)
    {
        return $null;
    }

    $almostExpiredCertificates = Get-ChildItem -Recurse -File cert:\ | Where-Object {$_.notafter -ge (get-date).AddDays(30) -AND $_.Thumbprint -match $certFingerprint}
    if ($almostExpiredCertificates -eq $null)
    {
        Write-Host "##teamcity[message text='WARNING! The certificate will expire in less than a month! Please talk to the IT to order a new certificate.' status='WARNING']"
    }

    return $certificates[0];
}

$rootDir = Get-Location

Write-Host "Finding assemblies"
$srcProjects = [Array](Get-ChildItem -Directory -Path (Join-Path ($rootDir) "\src\EPiServer.*") | Where {$_.FullName -notlike "*\node_modules\*"}
)

$assemblies = @()
foreach($item in $srcProjects)
{
    $dllName = $item.Name
    $dllName += ".dll"

    $projectAssemblies = (Get-ChildItem -Recurse -Path $item.FullName -File -Filter $dllName )
    if($projectAssemblies.length -lt 1){
        Write-Host ("File not found: " + $dllName + " in directory: " + $item.FullName)
        continue
    }

    $assemblies += $projectAssemblies
}

Write-Host "Signing assemblies"
$signError = $false
foreach ($assembly in $assemblies)
{
    Write-Host (" Signing " + $assembly.FullName)
    $LASTEXITCODE = 0
    &"$WindowsSDKPath\sn.exe" -q -Rc  $assembly.FullName "EPiServerProduct"
    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }
}

$url = "http://timestamp.digicert.com/scripts/timstamp.dll"
$cert = Get-Cert
if ($cert -eq $null)
{
    Write-Error "No certificate has been found"
    exit 1
}

foreach($item in $assemblies)
{
    Write-Host ("Authenticode signing " + $item.FullName)
    Set-AuthenticodeSignature -FilePath $item.FullName -Certificate $cert -TimestampServer $url -WarningAction Stop | Out-Null

    $signed = (Get-AuthenticodeSignature -filepath $item.FullName).Status
    if($signed -eq "NotSigned")
    {
        Write-Host ("Authenticode signing failed " + $item.FullName)
        exit 1
    }
}
