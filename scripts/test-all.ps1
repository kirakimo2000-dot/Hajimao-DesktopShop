[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$testProjects = @(
    'tests/HajimaoDesktopShop.Domain.Tests/HajimaoDesktopShop.Domain.Tests.csproj',
    'tests/HajimaoDesktopShop.Application.Tests/HajimaoDesktopShop.Application.Tests.csproj',
    'tests/HajimaoDesktopShop.Infrastructure.Tests/HajimaoDesktopShop.Infrastructure.Tests.csproj',
    'tests/HajimaoDesktopShop.Rendering.Tests/HajimaoDesktopShop.Rendering.Tests.csproj',
    'tests/HajimaoDesktopShop.Desktop.Tests/HajimaoDesktopShop.Desktop.Tests.csproj',
    'tests/HajimaoDesktopShop.Release.Tests/HajimaoDesktopShop.Release.Tests.csproj'
)

Push-Location $repoRoot
try {
    foreach ($project in $testProjects) {
        $arguments = @('test', $project, '-c', $Configuration, '--nologo')
        if ($NoRestore) {
            $arguments += '--no-restore'
        }

        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Test project failed: $project"
        }
    }
}
finally {
    Pop-Location
}
