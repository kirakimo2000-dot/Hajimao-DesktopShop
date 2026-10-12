[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Source,
    [string]$OutputDirectory = "src/HajimaoDesktopShop.Desktop/Assets/Content/facades"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourcePath = [IO.Path]::GetFullPath($Source)
$outputPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
$project = Join-Path $repoRoot 'tools\HajimaoDesktopShop.StorefrontPrep\HajimaoDesktopShop.StorefrontPrep.csproj'
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Source atlas does not exist: $sourcePath"
}

& dotnet run --project $project --configuration Release -- $sourcePath $outputPath
if ($LASTEXITCODE -ne 0) {
    throw "Storefront texture preparation failed with exit code $LASTEXITCODE."
}

Get-ChildItem -LiteralPath $outputPath -File -Filter '*.png' |
    Sort-Object Name |
    Select-Object Name, Length, FullName
