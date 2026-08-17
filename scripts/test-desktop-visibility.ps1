[CmdletBinding()]
param(
    [string]$ExecutablePath = (Join-Path $PSScriptRoot '..\Hajimao DesktopShop.exe'),
    [int]$TimeoutSeconds = 15
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class DesktopVisibilityNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public delegate bool EnumWindowsProcedure(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProcedure callback, IntPtr parameter);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr window, out Rect bounds);

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(Point point);

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }
}
'@

function Find-VisibleWindow {
    param([Parameter(Mandatory = $true)][int]$ProcessId)

    $script:foundWindow = [IntPtr]::Zero
    [DesktopVisibilityNative]::EnumWindows({
        param($window, $parameter)
        $ownerProcessId = 0
        [void][DesktopVisibilityNative]::GetWindowThreadProcessId($window, [ref]$ownerProcessId)
        if ($ownerProcessId -eq $ProcessId -and [DesktopVisibilityNative]::IsWindowVisible($window)) {
            $script:foundWindow = $window
            return $false
        }

        return $true
    }, [IntPtr]::Zero) | Out-Null
    return $script:foundWindow
}

$resolvedExecutable = [IO.Path]::GetFullPath($ExecutablePath)
if (-not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) {
    throw "Desktop executable is missing: $resolvedExecutable"
}

$existing = @(Get-CimInstance Win32_Process | Where-Object {
    $_.ExecutablePath -and [string]::Equals(
        [IO.Path]::GetFullPath($_.ExecutablePath),
        $resolvedExecutable,
        [StringComparison]::OrdinalIgnoreCase)
})
if ($existing.Count -ne 0) {
    throw "Visibility test requires no running instance; found $($existing.Count)."
}

$process = Start-Process `
    -FilePath $resolvedExecutable `
    -WorkingDirectory (Split-Path $resolvedExecutable -Parent) `
    -PassThru
try {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $window = [IntPtr]::Zero
    do {
        $process.Refresh()
        if ($process.HasExited) {
            throw "Desktop process exited with code $($process.ExitCode) before showing its window."
        }

        $window = Find-VisibleWindow -ProcessId $process.Id
        if ($window -ne [IntPtr]::Zero) {
            break
        }

        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)

    if ($window -eq [IntPtr]::Zero) {
        throw 'Desktop process did not create a visible top-level window.'
    }

    Start-Sleep -Seconds 2
    $bounds = New-Object DesktopVisibilityNative+Rect
    if (-not [DesktopVisibilityNative]::GetWindowRect($window, [ref]$bounds)) {
        throw 'Could not read the desktop window bounds.'
    }

    $width = $bounds.Right - $bounds.Left
    $height = $bounds.Bottom - $bounds.Top
    if ($width -le 0 -or $height -le 0) {
        throw "Desktop window has invalid bounds: ${width}x${height}."
    }

    $center = New-Object DesktopVisibilityNative+Point
    $center.X = $bounds.Left + [int]($width / 2)
    $center.Y = $bounds.Top + [int]($height / 2)
    if ([DesktopVisibilityNative]::WindowFromPoint($center) -ne $window) {
        throw 'Desktop window is not the composited topmost window at its center point.'
    }

    $expectedColors = [Collections.Generic.HashSet[int]]::new()
    foreach ($hex in @('#506878', '#6B4634', '#B87349', '#34383F')) {
        [void]$expectedColors.Add([Drawing.ColorTranslator]::FromHtml($hex).ToArgb())
    }

    $hasStreetPixel = $false
    $bitmap = [Drawing.Bitmap]::new($width, $height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(
            $bounds.Left,
            $bounds.Top,
            0,
            0,
            $bitmap.Size,
            [Drawing.CopyPixelOperation]::SourceCopy)
        for ($y = 0; $y -lt $height -and -not $hasStreetPixel; $y += 2) {
            for ($x = 0; $x -lt $width; $x += 2) {
                if ($expectedColors.Contains($bitmap.GetPixel($x, $y).ToArgb())) {
                    $hasStreetPixel = $true
                    break
                }
            }
        }
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }

    if (-not $hasStreetPixel) {
        throw 'Desktop window is present but did not compose any street-scene pixels.'
    }

    Write-Output "Desktop visibility passed for PID $($process.Id): ${width}x${height}."
}
finally {
    $process.Refresh()
    if (-not $process.HasExited) {
        $actualPath = [IO.Path]::GetFullPath($process.Path)
        if (-not [string]::Equals(
                $actualPath,
                $resolvedExecutable,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to stop PID $($process.Id) from unexpected path '$actualPath'."
        }

        $process.Kill()
        [void]$process.WaitForExit(10000)
    }
}
