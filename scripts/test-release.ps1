[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,
    [switch]$RequireFullMsiInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class ReleaseSmokeNative
{
    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_APPWINDOW = 0x00040000L;

    private delegate bool EnumWindowsProcedure(IntPtr window, IntPtr parameter);

    public static int AssertVisibleWindowsStayOutOfTaskbar(int processId)
    {
        var visibleWindowCount = 0;
        string failure = null;
        EnumWindows((window, parameter) =>
        {
            int ownerProcessId;
            GetWindowThreadProcessId(window, out ownerProcessId);
            if (ownerProcessId != processId || !IsWindowVisible(window))
            {
                return true;
            }

            visibleWindowCount++;
            var extendedStyle = GetWindowLongPtr(window, GWL_EXSTYLE).ToInt64();
            if ((extendedStyle & WS_EX_APPWINDOW) != 0)
            {
                failure = string.Format(
                    "Window 0x{0:X} exposes WS_EX_APPWINDOW.",
                    window.ToInt64());
            }

            return true;
        }, IntPtr.Zero);

        if (failure != null)
        {
            throw new InvalidOperationException(failure);
        }

        return visibleWindowCount;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProcedure callback, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
}
'@

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
        throw "Path is outside the owned root: $targetFull"
    }

    return $targetFull
}

function Wait-ForCondition {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Condition,
        [Parameter(Mandatory = $true)][string]$Description,
        [int]$TimeoutSeconds = 30
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if (& $Condition) {
            return
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Timed out waiting for $Description."
}

function Start-IsolatedApplication {
    param(
        [Parameter(Mandatory = $true)][string]$ExecutablePath,
        [Parameter(Mandatory = $true)][string]$DataDirectory
    )

    $previousDataDirectory = [Environment]::GetEnvironmentVariable(
        'HAJIMAO_DATA_DIRECTORY',
        [EnvironmentVariableTarget]::Process)
    try {
        [Environment]::SetEnvironmentVariable(
            'HAJIMAO_DATA_DIRECTORY',
            $DataDirectory,
            [EnvironmentVariableTarget]::Process)
        return Start-Process `
            -FilePath $ExecutablePath `
            -WorkingDirectory (Split-Path $ExecutablePath -Parent) `
            -PassThru
    }
    finally {
        [Environment]::SetEnvironmentVariable(
            'HAJIMAO_DATA_DIRECTORY',
            $previousDataDirectory,
            [EnvironmentVariableTarget]::Process)
    }
}

function Stop-OwnedApplication {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$ExpectedPath
    )

    if ($null -eq $Process) {
        return
    }

    $Process.Refresh()
    if ($Process.HasExited) {
        return
    }

    $actualPath = [System.IO.Path]::GetFullPath($Process.Path)
    $expectedFullPath = [System.IO.Path]::GetFullPath($ExpectedPath)
    if (-not [string]::Equals(
            $actualPath,
            $expectedFullPath,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to stop PID $($Process.Id): expected '$expectedFullPath', actual '$actualPath'."
    }

    $Process.Kill()
    if (-not $Process.WaitForExit(10000)) {
        throw "Owned application PID $($Process.Id) did not exit."
    }
}

function Get-MsiProperty {
    param(
        [Parameter(Mandatory = $true)][string]$PackagePath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z][A-Za-z0-9_]*$')][string]$PropertyName
    )

    $installer = $null
    $database = $null
    $view = $null
    $record = $null
    try {
        $installer = New-Object -ComObject WindowsInstaller.Installer
        $database = $installer.OpenDatabase($PackagePath, 0)
        $view = $database.OpenView(
            "SELECT ``Value`` FROM ``Property`` WHERE ``Property``='$PropertyName'")
        [void]$view.Execute()
        $record = $view.Fetch()
        if ($null -eq $record) {
            throw "MSI property is missing: $PropertyName"
        }

        return [string]$record.StringData(1)
    }
    finally {
        foreach ($comObject in @($record, $view, $database, $installer)) {
            if ($null -ne $comObject -and [System.Runtime.InteropServices.Marshal]::IsComObject($comObject)) {
                [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($comObject)
            }
        }
    }
}

function Get-InstalledMsiProducts {
    param([Parameter(Mandatory = $true)][string]$ProductCode)

    $installer = $null
    try {
        $installer = New-Object -ComObject WindowsInstaller.Installer
        $products = @($installer.ProductsEx($ProductCode, '', 7))
        return @($products | ForEach-Object {
            [pscustomobject]@{
                ProductCode = $ProductCode
                InstallLocation = [string]$_.InstallProperty('InstallLocation')
            }
        })
    }
    finally {
        if ($null -ne $installer -and [System.Runtime.InteropServices.Marshal]::IsComObject($installer)) {
            [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
        }
    }
}

function Quote-NativeArgument {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value.Contains('"')) {
        throw 'Native arguments containing quote characters are not supported.'
    }

    return '"' + $Value + '"'
}

function Invoke-MsiExec {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Operation,
        [int[]]$AllowedExitCodes = @(0, 3010)
    )

    $msiExecutable = Join-Path $env:SystemRoot 'System32\msiexec.exe'
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $msiExecutable
    $startInfo.UseShellExecute = $false
    if ($startInfo.PSObject.Properties.Name -contains 'ArgumentList') {
        foreach ($argument in $Arguments) {
            [void]$startInfo.ArgumentList.Add($argument)
        }
    }
    else {
        $startInfo.Arguments = ($Arguments | ForEach-Object {
            Quote-NativeArgument -Value $_
        }) -join ' '
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if (-not $process.WaitForExit(120000)) {
        $process.Kill()
        $process.WaitForExit(10000) | Out-Null
        throw "$Operation timed out for owned msiexec PID $($process.Id)."
    }

    if ($process.ExitCode -notin $AllowedExitCodes) {
        throw "$Operation failed with exit code $($process.ExitCode)."
    }
}

function Invoke-CleanupStep {
    param(
        [Parameter(Mandatory = $true)][string]$Description,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    try {
        & $Action
    }
    catch {
        Write-Warning "$Description failed: $($_.Exception.Message)"
    }
}

function Test-IsAdministrator {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object System.Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole(
        [System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-ApplicationRuntime {
    param(
        [Parameter(Mandatory = $true)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$DataDirectory,
        [Parameter(Mandatory = $true)][string]$Label
    )

    Wait-ForCondition -Description "$Label process responsiveness" -Condition {
        $Process.Refresh()
        -not $Process.HasExited -and $Process.Responding
    }
    Wait-ForCondition -Description "$Label SQLite save" -Condition {
        Test-Path -LiteralPath (Join-Path $DataDirectory 'hajimao.db') -PathType Leaf
    }
    Wait-ForCondition -Description "$Label diagnostic log" -Condition {
        $logDirectory = Join-Path $DataDirectory 'logs'
        (Test-Path -LiteralPath $logDirectory -PathType Container) -and
            ($null -ne (Get-ChildItem -LiteralPath $logDirectory -File -ErrorAction SilentlyContinue |
                Select-Object -First 1))
    }
    Wait-ForCondition -Description "$Label visible desktop window" -Condition {
        [ReleaseSmokeNative]::AssertVisibleWindowsStayOutOfTaskbar($Process.Id) -gt 0
    }

    Start-Sleep -Seconds 7
    $Process.Refresh()
    if ($Process.HasExited -or -not $Process.Responding) {
        throw "$Label process stopped responding before autosave verification."
    }
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releaseRoot = Join-Path $repoRoot "artifacts\release\$Version"
$portableName = "HajimaoDesktopShop-$Version-win-x64-portable.zip"
$installerName = "HajimaoDesktopShop-$Version-win-x64.msi"
$manifestName = "HajimaoDesktopShop-$Version-win-x64-release.json"
$portableArchive = Join-Path $releaseRoot $portableName
$installerPackage = Join-Path $releaseRoot $installerName
$manifestPath = Join-Path $releaseRoot $manifestName

foreach ($requiredFile in @($portableArchive, $installerPackage, $manifestPath)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required release artifact is missing: $requiredFile"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.version -ne $Version -or $manifest.rid -ne 'win-x64' -or $manifest.signed -ne $false) {
    throw 'Release manifest version, RID, or unsigned disclosure is invalid.'
}

foreach ($entry in $manifest.files) {
    $artifactPath = Join-Path $releaseRoot $entry.fileName
    $artifact = Get-Item -LiteralPath $artifactPath
    $actualHash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($artifact.Length -ne [long]$entry.sizeBytes -or $actualHash -ne [string]$entry.sha256) {
        throw "Artifact metadata mismatch: $($entry.fileName)"
    }
}

$productCode = Get-MsiProperty -PackagePath $installerPackage -PropertyName 'ProductCode'
$parsedProductCode = [guid]::Empty
if (-not [guid]::TryParse($productCode, [ref]$parsedProductCode)) {
    throw "MSI ProductCode is invalid: $productCode"
}

$preExistingProducts = @(Get-InstalledMsiProducts -ProductCode $productCode)
if ($preExistingProducts.Count -ne 0) {
    throw "Refusing to smoke-test ProductCode $productCode because it is already installed."
}

$systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$testRoot = Assert-ChildPath `
    -Parent $systemTemp `
    -Target (Join-Path $systemTemp ("HajimaoDesktopShop-release-smoke-" + [guid]::NewGuid().ToString('N')))
$portableExtract = Assert-ChildPath -Parent $testRoot -Target (Join-Path $testRoot 'portable')
$portableData = Assert-ChildPath -Parent $testRoot -Target (Join-Path $testRoot 'portable-data')
$installedData = Assert-ChildPath -Parent $testRoot -Target (Join-Path $testRoot 'installed-data')
$installDirectory = Assert-ChildPath -Parent $testRoot -Target (Join-Path $testRoot 'installed-app')
$administrativeImage = Assert-ChildPath -Parent $testRoot -Target (Join-Path $testRoot 'msi-image')
$installLog = Assert-ChildPath -Parent $testRoot -Target (Join-Path $testRoot 'install.log')
$uninstallLog = Assert-ChildPath -Parent $testRoot -Target (Join-Path $testRoot 'uninstall.log')

$portableProcess = $null
$installedProcess = $null
$portableExecutable = $null
$installedExecutable = Join-Path $installDirectory 'HajimaoDesktopShop.Desktop.exe'
$ownedProductCode = $null
$isAdministrator = Test-IsAdministrator

if ($RequireFullMsiInstall -and -not $isAdministrator) {
    throw 'Full per-machine MSI smoke requires an elevated administrator session.'
}

New-Item -ItemType Directory -Path $testRoot, $portableExtract, $portableData, $installedData -Force |
    Out-Null
try {
    Expand-Archive -LiteralPath $portableArchive -DestinationPath $portableExtract
    $portableExecutable = Join-Path $portableExtract 'HajimaoDesktopShop.Desktop.exe'
    $portableProcess = Start-IsolatedApplication `
        -ExecutablePath $portableExecutable `
        -DataDirectory $portableData
    Test-ApplicationRuntime `
        -Process $portableProcess `
        -DataDirectory $portableData `
        -Label 'Portable'
    Stop-OwnedApplication -Process $portableProcess -ExpectedPath $portableExecutable
    $portableProcess = $null

    if ($isAdministrator) {
        $installArguments = @(
            '/i',
            $installerPackage,
            '/qn',
            '/norestart',
            'ALLUSERS=1',
            "INSTALLFOLDER=$installDirectory",
            '/L*v',
            $installLog)
        Invoke-MsiExec -Operation 'MSI install' -Arguments $installArguments
        $ownedProductCode = $productCode

        $installedProducts = @(Get-InstalledMsiProducts -ProductCode $productCode)
        if ($installedProducts.Count -ne 1) {
            throw "MSI registration count is not one after install: $($installedProducts.Count)."
        }

        $registeredInstallLocation = [System.IO.Path]::GetFullPath(
            $installedProducts[0].InstallLocation).TrimEnd(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar)
        $expectedInstallLocation = [System.IO.Path]::GetFullPath($installDirectory).TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
        if (-not [string]::Equals(
                $registeredInstallLocation,
                $expectedInstallLocation,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "MSI registered unexpected install location: $registeredInstallLocation"
        }

        [void](Assert-ChildPath -Parent $testRoot -Target $registeredInstallLocation)
        if (-not (Test-Path -LiteralPath $installedExecutable -PathType Leaf)) {
            throw "Installed executable is missing: $installedExecutable"
        }

        $installedProcess = Start-IsolatedApplication `
            -ExecutablePath $installedExecutable `
            -DataDirectory $installedData
        Test-ApplicationRuntime `
            -Process $installedProcess `
            -DataDirectory $installedData `
            -Label 'Installed'
        Stop-OwnedApplication -Process $installedProcess -ExpectedPath $installedExecutable
        $installedProcess = $null

        $uninstallArguments = @(
            '/x',
            $ownedProductCode,
            '/qn',
            '/norestart',
            '/L*v',
            $uninstallLog)
        Invoke-MsiExec -Operation 'MSI uninstall' -Arguments $uninstallArguments
        if (Test-Path -LiteralPath $installedExecutable) {
            throw 'MSI uninstall left the application executable behind.'
        }

        $remainingProducts = @(Get-InstalledMsiProducts -ProductCode $productCode)
        if ($remainingProducts.Count -ne 0) {
            throw 'MSI registration remains after uninstall.'
        }

        if (-not (Test-Path -LiteralPath $installedData -PathType Container)) {
            throw 'MSI uninstall removed the isolated application data directory.'
        }

        $ownedProductCode = $null
        Write-Output "Release smoke passed for Hajimao DesktopShop $Version (full per-machine MSI)."
    }
    else {
        $administrativeArguments = @(
            '/a',
            $installerPackage,
            '/qn',
            '/norestart',
            "TARGETDIR=$administrativeImage",
            '/L*v',
            $installLog)
        Invoke-MsiExec -Operation 'MSI administrative-image extraction' `
            -Arguments $administrativeArguments

        $extractedExecutables = @(Get-ChildItem `
            -LiteralPath $administrativeImage `
            -Filter 'HajimaoDesktopShop.Desktop.exe' `
            -File `
            -Recurse)
        if ($extractedExecutables.Count -ne 1) {
            throw "MSI administrative image contains $($extractedExecutables.Count) main executables."
        }

        $installedExecutable = $extractedExecutables[0].FullName
        [void](Assert-ChildPath -Parent $administrativeImage -Target $installedExecutable)
        $installedProcess = Start-IsolatedApplication `
            -ExecutablePath $installedExecutable `
            -DataDirectory $installedData
        Test-ApplicationRuntime `
            -Process $installedProcess `
            -DataDirectory $installedData `
            -Label 'MSI administrative image'
        Stop-OwnedApplication -Process $installedProcess -ExpectedPath $installedExecutable
        $installedProcess = $null

        $administrativeProducts = @(Get-InstalledMsiProducts -ProductCode $productCode)
        if ($administrativeProducts.Count -ne 0) {
            throw 'MSI administrative-image extraction unexpectedly registered the product.'
        }

        Write-Output (
            "Release smoke passed for Hajimao DesktopShop $Version " +
                '(non-admin MSI administrative image; full install gate requires elevation).')
    }
}
finally {
    Invoke-CleanupStep -Description 'Portable process cleanup' -Action {
        Stop-OwnedApplication -Process $portableProcess -ExpectedPath $portableExecutable
    }
    Invoke-CleanupStep -Description 'Installed process cleanup' -Action {
        Stop-OwnedApplication -Process $installedProcess -ExpectedPath $installedExecutable
    }
    if ($null -ne $ownedProductCode) {
        Invoke-CleanupStep -Description 'MSI cleanup uninstall' -Action {
            $cleanupArguments = @(
                '/x',
                $ownedProductCode,
                '/qn',
                '/norestart',
                '/L*v',
                $uninstallLog)
            Invoke-MsiExec -Operation 'MSI cleanup uninstall' -Arguments $cleanupArguments
        }
    }

    Invoke-CleanupStep -Description 'Temporary directory cleanup' -Action {
        $ownedTestRoot = Assert-ChildPath -Parent $systemTemp -Target $testRoot
        if (Test-Path -LiteralPath $ownedTestRoot) {
            Remove-Item -LiteralPath $ownedTestRoot -Recurse -Force
        }
    }
}
