[CmdletBinding()]
param(
    [string]$Version,
    [switch]$SkipVerification
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-LastExitCode {
    param([Parameter(Mandatory = $true)][string]$Operation)

    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Parent,
        [Parameter(Mandatory = $true)][string]$Target
    )

    $parentFull = [System.IO.Path]::GetFullPath($Parent).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $targetFull = [System.IO.Path]::GetFullPath($Target)
    $prefix = $parentFull + [System.IO.Path]::DirectorySeparatorChar
    if (-not $targetFull.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside artifacts root: $targetFull"
    }

    return $targetFull
}

function Remove-OwnedPath {
    param(
        [Parameter(Mandatory = $true)][string]$ArtifactsRoot,
        [Parameter(Mandatory = $true)][string]$Target
    )

    $ownedTarget = Assert-ChildPath -Parent $ArtifactsRoot -Target $Target
    if (Test-Path -LiteralPath $ownedTarget) {
        Remove-Item -LiteralPath $ownedTarget -Recurse -Force
    }
}

function Get-ThreePartVersion {
    param([Parameter(Mandatory = $true)][string]$Value)

    $match = [System.Text.RegularExpressions.Regex]::Match($Value, '^\s*(\d+\.\d+\.\d+)')
    if (-not $match.Success) {
        throw "Could not read a three-part version from '$Value'."
    }

    return $match.Groups[1].Value
}

function Assert-PublishedVersion {
    param(
        [Parameter(Mandatory = $true)][string]$ExecutablePath,
        [Parameter(Mandatory = $true)][string]$ExpectedVersion,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
        throw "$Label executable was not found: $ExecutablePath"
    }

    $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ExecutablePath)
    $fileVersion = Get-ThreePartVersion -Value $versionInfo.FileVersion
    $productVersion = Get-ThreePartVersion -Value $versionInfo.ProductVersion
    if ($fileVersion -ne $ExpectedVersion -or $productVersion -ne $ExpectedVersion) {
        throw "$Label executable version mismatch. File=$fileVersion Product=$productVersion Expected=$ExpectedVersion."
    }
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$versionPropsPath = Join-Path $repoRoot 'Directory.Build.props'
[xml]$versionProps = Get-Content -LiteralPath $versionPropsPath -Raw
$activeVersion = [string]$versionProps.Project.PropertyGroup.VersionPrefix

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $activeVersion
}

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must use major.minor.patch format. Actual: '$Version'."
}

if (-not [string]::Equals($Version, $activeVersion, [System.StringComparison]::Ordinal)) {
    throw "Requested version '$Version' does not match active VersionPrefix '$activeVersion'."
}

$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$releaseRoot = Assert-ChildPath -Parent $artifactsRoot -Target (Join-Path $artifactsRoot "release\$Version")
$stagingRoot = Assert-ChildPath -Parent $artifactsRoot -Target (Join-Path $artifactsRoot "staging\$Version")
$portableDir = Assert-ChildPath -Parent $artifactsRoot -Target (Join-Path $stagingRoot 'portable')
$installerPublishDir = Assert-ChildPath -Parent $artifactsRoot -Target (Join-Path $stagingRoot 'installer-publish')
$installerOutput = Assert-ChildPath -Parent $artifactsRoot -Target (Join-Path $stagingRoot 'installer')

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
Remove-OwnedPath -ArtifactsRoot $artifactsRoot -Target $releaseRoot
Remove-OwnedPath -ArtifactsRoot $artifactsRoot -Target $stagingRoot
New-Item -ItemType Directory `
    -Path $releaseRoot, $portableDir, $installerPublishDir, $installerOutput `
    -Force | Out-Null

$completed = $false
Push-Location $repoRoot
try {
    if (-not $SkipVerification) {
        & dotnet restore HajimaoDesktopShop.slnx --nologo
        Assert-LastExitCode 'dotnet restore'
        & (Join-Path $PSScriptRoot 'test-all.ps1') -Configuration Release -NoRestore
        Assert-LastExitCode 'test-all.ps1'
        & dotnet build HajimaoDesktopShop.slnx -c Release --no-restore --nologo
        Assert-LastExitCode 'dotnet build'
    }

    & dotnet publish src/HajimaoDesktopShop.Desktop/HajimaoDesktopShop.Desktop.csproj `
        -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeAllContentForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None -p:DebugSymbols=false `
        -p:IncludeSourceRevisionInInformationalVersion=false `
        -o $portableDir --nologo
    Assert-LastExitCode 'portable dotnet publish'

    Get-ChildItem -LiteralPath $portableDir -Recurse -File -Filter '*.pdb' | ForEach-Object {
        $pdbPath = Assert-ChildPath -Parent $stagingRoot -Target $_.FullName
        Remove-Item -LiteralPath $pdbPath -Force
    }

    $portableHostPath = Join-Path $portableDir 'HajimaoDesktopShop.Desktop.exe'
    if (-not (Test-Path -LiteralPath $portableHostPath -PathType Leaf)) {
        throw "Portable host executable was not found: $portableHostPath"
    }

    $portableExecutablePath = Join-Path $portableDir 'Hajimao DesktopShop.exe'
    Move-Item -LiteralPath $portableHostPath -Destination $portableExecutablePath
    $portableFiles = @(Get-ChildItem -LiteralPath $portableDir -File -Recurse)
    if ($portableFiles.Count -ne 1 -or
        -not [string]::Equals(
            $portableFiles[0].FullName,
            $portableExecutablePath,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        $unexpectedFiles = ($portableFiles | ForEach-Object FullName) -join ', '
        throw "Portable output must contain only 'Hajimao DesktopShop.exe'. Actual: $unexpectedFiles"
    }

    Assert-PublishedVersion `
        -ExecutablePath $portableExecutablePath `
        -ExpectedVersion $Version `
        -Label 'Portable'

    & dotnet publish src/HajimaoDesktopShop.Desktop/HajimaoDesktopShop.Desktop.csproj `
        -c Release -r win-x64 --self-contained true `
        -p:DebugType=None -p:DebugSymbols=false `
        -p:IncludeSourceRevisionInInformationalVersion=false `
        -o $installerPublishDir --nologo
    Assert-LastExitCode 'installer payload dotnet publish'

    Get-ChildItem -LiteralPath $installerPublishDir -Recurse -File -Filter '*.pdb' | ForEach-Object {
        $pdbPath = Assert-ChildPath -Parent $stagingRoot -Target $_.FullName
        Remove-Item -LiteralPath $pdbPath -Force
    }

    $installerExecutablePath = Join-Path $installerPublishDir 'HajimaoDesktopShop.Desktop.exe'
    Assert-PublishedVersion `
        -ExecutablePath $installerExecutablePath `
        -ExpectedVersion $Version `
        -Label 'Installer payload'

    & dotnet build installer/HajimaoDesktopShop.Installer/HajimaoDesktopShop.Installer.wixproj `
        -c Release "-p:ProductVersion=$Version" "-p:PublishDir=$installerPublishDir" `
        "-p:OutputPath=$installerOutput" --nologo
    Assert-LastExitCode 'WiX dotnet build'

    $portableName = "HajimaoDesktopShop-$Version-win-x64-portable.zip"
    $installerName = "HajimaoDesktopShop-$Version-win-x64.msi"
    $manifestName = "HajimaoDesktopShop-$Version-win-x64-release.json"
    $checksumsName = "HajimaoDesktopShop-$Version-win-x64.sha256.txt"
    $portablePath = Join-Path $releaseRoot $portableName
    $installerSource = Join-Path $installerOutput $installerName
    $installerPath = Join-Path $releaseRoot $installerName

    if (-not (Test-Path -LiteralPath $installerSource -PathType Leaf)) {
        throw "WiX output was not found: $installerSource"
    }

    Compress-Archive `
        -LiteralPath $portableExecutablePath `
        -DestinationPath $portablePath `
        -CompressionLevel Optimal
    Copy-Item -LiteralPath $installerSource -Destination $installerPath

    $releaseFiles = @($portablePath, $installerPath) | Sort-Object
    $entries = foreach ($releaseFile in $releaseFiles) {
        $file = Get-Item -LiteralPath $releaseFile
        $hash = Get-FileHash -LiteralPath $releaseFile -Algorithm SHA256
        [ordered]@{
            fileName = $file.Name
            sizeBytes = $file.Length
            sha256 = $hash.Hash.ToLowerInvariant()
        }
    }

    $manifest = [ordered]@{
        product = 'Hajimao DesktopShop'
        version = $Version
        rid = 'win-x64'
        schemaVersion = 6
        signed = $false
        files = @($entries)
    }

    $manifestPath = Join-Path $releaseRoot $manifestName
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

    $checksumLines = $entries |
        Sort-Object -Property fileName |
        ForEach-Object { "$($_.sha256)  $($_.fileName)" }
    $checksumsPath = Join-Path $releaseRoot $checksumsName
    $checksumLines | Set-Content -LiteralPath $checksumsPath -Encoding UTF8

    $completed = $true
    Write-Output "Release artifacts created in $releaseRoot"
    Get-ChildItem -LiteralPath $releaseRoot -File | Sort-Object Name | Select-Object Name, Length
}
finally {
    Pop-Location
    Remove-OwnedPath -ArtifactsRoot $artifactsRoot -Target $stagingRoot
    if (-not $completed) {
        Remove-OwnedPath -ArtifactsRoot $artifactsRoot -Target $releaseRoot
    }
}
