[CmdletBinding()]
param()

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
        throw "Refusing to clean outside '$parentFull': $targetFull"
    }

    return $targetFull
}

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$generatedDirectories = @(Get-ChildItem -LiteralPath $repoRoot -Directory -Recurse |
    Where-Object { $_.Name -in @('bin', 'obj') })

foreach ($directory in $generatedDirectories) {
    $ownedPath = Assert-ChildPath -Parent $repoRoot -Target $directory.FullName
    Remove-Item -LiteralPath $ownedPath -Recurse -Force
}

$stagingRoot = Assert-ChildPath -Parent $repoRoot -Target (
    Join-Path $repoRoot 'artifacts\staging')
if (Test-Path -LiteralPath $stagingRoot) {
    Get-ChildItem -LiteralPath $stagingRoot -Force | ForEach-Object {
        $ownedPath = Assert-ChildPath -Parent $stagingRoot -Target $_.FullName
        Remove-Item -LiteralPath $ownedPath -Recurse -Force
    }
}

$artifactsRoot = Assert-ChildPath -Parent $repoRoot -Target (
    Join-Path $repoRoot 'artifacts')
$temporaryArtifactDirectories = @(
    Get-ChildItem -LiteralPath $artifactsRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -in @('qa', 'diagnostics', 'visual-audit') -or $_.Name -like 'startup-smoke-*'
        }
)
foreach ($directory in $temporaryArtifactDirectories) {
    $ownedPath = Assert-ChildPath -Parent $artifactsRoot -Target $directory.FullName
    Remove-Item -LiteralPath $ownedPath -Recurse -Force
}
$temporaryArtifactFiles = @(
    Get-ChildItem -LiteralPath $artifactsRoot -File -Filter '*.png' -ErrorAction SilentlyContinue
)
foreach ($file in $temporaryArtifactFiles) {
    $ownedPath = Assert-ChildPath -Parent $artifactsRoot -Target $file.FullName
    Remove-Item -LiteralPath $ownedPath -Force
}

$systemTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$captureRoot = Assert-ChildPath -Parent $systemTemp -Target (
    Join-Path $systemTemp 'hajimao-v0133-street')
if (Test-Path -LiteralPath $captureRoot) {
    Remove-Item -LiteralPath $captureRoot -Recurse -Force
}

[pscustomobject]@{
    RemovedBuildDirectories = $generatedDirectories.Count
    StagingChildren = @(Get-ChildItem -LiteralPath $stagingRoot -Force).Count
    RemovedTemporaryArtifactDirectories = $temporaryArtifactDirectories.Count
    RemovedTemporaryArtifactFiles = $temporaryArtifactFiles.Count
    TestCaptureExists = Test-Path -LiteralPath $captureRoot
}
