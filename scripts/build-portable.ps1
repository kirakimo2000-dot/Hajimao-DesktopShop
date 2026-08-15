[CmdletBinding()]
param(
    [string]$Version,
    [switch]$PrunePrevious
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Parent,
        [Parameter(Mandatory = $true)][string]$Target
    )

    $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar)
    $targetFull = [IO.Path]::GetFullPath($Target)
    $prefix = $parentFull + [IO.Path]::DirectorySeparatorChar
    if (-not $targetFull.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
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

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$latestExecutable = Join-Path $repoRoot 'Hajimao DesktopShop.exe'
[xml]$props = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props') -Raw
$activeVersion = [string]$props.Project.PropertyGroup.VersionPrefix
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $activeVersion
}

if ($Version -notmatch '^\d+\.\d+\.\d+$' -or $Version -ne $activeVersion) {
    throw "Portable version '$Version' must match active VersionPrefix '$activeVersion'."
}

$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$releaseRoot = Assert-ChildPath -Parent $artifactsRoot -Target (
    Join-Path $artifactsRoot "release\$Version")
$stagingRoot = Assert-ChildPath -Parent $artifactsRoot -Target (
    Join-Path $artifactsRoot "staging\$Version")
$portableDir = Assert-ChildPath -Parent $artifactsRoot -Target (
    Join-Path $stagingRoot 'portable')

Remove-OwnedPath -ArtifactsRoot $artifactsRoot -Target $releaseRoot
Remove-OwnedPath -ArtifactsRoot $artifactsRoot -Target $stagingRoot
New-Item -ItemType Directory -Path $releaseRoot, $portableDir -Force | Out-Null

$completed = $false
Push-Location $repoRoot
try {
    & dotnet publish src/HajimaoDesktopShop.Desktop/HajimaoDesktopShop.Desktop.csproj `
        -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeAllContentForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None -p:DebugSymbols=false `
        -p:IncludeSourceRevisionInInformationalVersion=false `
        -o $portableDir --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Portable publish failed with exit code $LASTEXITCODE."
    }

    Get-ChildItem -LiteralPath $portableDir -Recurse -File -Filter '*.pdb' |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }

    $publishedExe = Join-Path $portableDir 'HajimaoDesktopShop.Desktop.exe'
    $friendlyExe = Join-Path $portableDir 'Hajimao DesktopShop.exe'
    Move-Item -LiteralPath $publishedExe -Destination $friendlyExe

    $payload = @(Get-ChildItem -LiteralPath $portableDir -File -Recurse)
    if ($payload.Count -ne 1 -or $payload[0].FullName -ne $friendlyExe) {
        throw "Portable payload must contain only 'Hajimao DesktopShop.exe'."
    }

    $fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($friendlyExe).FileVersion
    if (-not $fileVersion.StartsWith($Version, [StringComparison]::Ordinal)) {
        throw "Portable file version '$fileVersion' does not match '$Version'."
    }

    Copy-Item -LiteralPath $friendlyExe -Destination $latestExecutable -Force

    $archive = Join-Path $releaseRoot "HajimaoDesktopShop-$Version-win-x64-portable.zip"
    Compress-Archive -LiteralPath $friendlyExe -DestinationPath $archive -CompressionLevel Optimal
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    $completed = $true

    if ($PrunePrevious) {
        $releaseParent = Join-Path $artifactsRoot 'release'
        Get-ChildItem -LiteralPath $releaseParent -Directory |
            Where-Object {
                -not [string]::Equals(
                    $_.FullName,
                    $releaseRoot,
                    [StringComparison]::OrdinalIgnoreCase)
            } |
            ForEach-Object {
                Remove-OwnedPath -ArtifactsRoot $artifactsRoot -Target $_.FullName
            }
    }

    Get-Item -LiteralPath $archive |
        Select-Object FullName, Length, @{Name = 'SHA256'; Expression = { $hash }}
}
finally {
    Pop-Location
    Remove-OwnedPath -ArtifactsRoot $artifactsRoot -Target $stagingRoot
    if (-not $completed) {
        Remove-OwnedPath -ArtifactsRoot $artifactsRoot -Target $releaseRoot
    }
}
