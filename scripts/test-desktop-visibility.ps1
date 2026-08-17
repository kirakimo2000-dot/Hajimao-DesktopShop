[CmdletBinding()]
param(
    [string]$ExecutablePath = (Join-Path $PSScriptRoot '..\Hajimao DesktopShop.exe'),
    [int]$TimeoutSeconds = 15,
    [switch]$EnterStore
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

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr window,
        int attribute,
        out Rect bounds,
        int boundsSize);

    public static bool GetPhysicalWindowRect(IntPtr window, out Rect bounds)
    {
        const int ExtendedFrameBounds = 9;
        return DwmGetWindowAttribute(
            window,
            ExtendedFrameBounds,
            out bounds,
            Marshal.SizeOf<Rect>()) == 0;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetPhysicalCursorPos(out Point point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetPhysicalCursorPos(int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    public struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        public uint Type;
        public MouseInput Mouse;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int inputSize);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr window);

    public static bool ClickPhysicalPoint(int x, int y)
    {
        const uint MouseInputType = 0;
        const uint Move = 0x0001;
        const uint LeftDown = 0x0002;
        const uint LeftUp = 0x0004;
        const uint VirtualDesktop = 0x4000;
        const uint Absolute = 0x8000;
        const int VirtualLeft = 76;
        const int VirtualTop = 77;
        const int VirtualWidth = 78;
        const int VirtualHeight = 79;
        var left = GetSystemMetrics(VirtualLeft);
        var top = GetSystemMetrics(VirtualTop);
        var width = Math.Max(2, GetSystemMetrics(VirtualWidth));
        var height = Math.Max(2, GetSystemMetrics(VirtualHeight));
        var absoluteX = (int)Math.Round((x - left) * 65535d / (width - 1));
        var absoluteY = (int)Math.Round((y - top) * 65535d / (height - 1));
        var inputs = new[]
        {
            new Input
            {
                Type = MouseInputType,
                Mouse = new MouseInput
                {
                    X = absoluteX,
                    Y = absoluteY,
                    Flags = Move | VirtualDesktop | Absolute
                }
            },
            new Input
            {
                Type = MouseInputType,
                Mouse = new MouseInput { Flags = LeftDown }
            },
            new Input
            {
                Type = MouseInputType,
                Mouse = new MouseInput { Flags = LeftUp }
            }
        };
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

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

    $bounds = New-Object DesktopVisibilityNative+Rect
    if (-not [DesktopVisibilityNative]::GetPhysicalWindowRect($window, [ref]$bounds)) {
        throw 'Could not read the desktop window bounds.'
    }

    $width = $bounds.Right - $bounds.Left
    $height = $bounds.Bottom - $bounds.Top
    if ($width -le 0 -or $height -le 0) {
        throw "Desktop window has invalid bounds: ${width}x${height}."
    }

    $expectedColors = [Collections.Generic.HashSet[int]]::new()
    foreach ($hex in @('#506878', '#6B4634', '#B87349', '#34383F')) {
        [void]$expectedColors.Add([Drawing.ColorTranslator]::FromHtml($hex).ToArgb())
    }

    $hasStreetPixel = $false
    $bitmap = [Drawing.Bitmap]::new($width, $height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        do {
            if (-not [DesktopVisibilityNative]::GetPhysicalWindowRect($window, [ref]$bounds)) {
                throw 'Could not refresh the desktop window bounds.'
            }

            $graphics.CopyFromScreen(
                $bounds.Left,
                $bounds.Top,
                0,
                0,
                $bitmap.Size,
                [Drawing.CopyPixelOperation]::SourceCopy)
            $sample = $bitmap.GetPixel(0, 0)
            Write-Verbose (
                "Street probe at {0},{1}: #{2:X2}{3:X2}{4:X2}" -f
                $bounds.Left,
                $bounds.Top,
                $sample.R,
                $sample.G,
                $sample.B)
            for ($y = 0; $y -lt $height -and -not $hasStreetPixel; $y += 2) {
                for ($x = 0; $x -lt $width; $x += 2) {
                    if ($expectedColors.Contains($bitmap.GetPixel($x, $y).ToArgb())) {
                        $hasStreetPixel = $true
                        break
                    }
                }
            }

            if (-not $hasStreetPixel) {
                Start-Sleep -Milliseconds 100
            }
        } while (-not $hasStreetPixel -and [DateTime]::UtcNow -lt $deadline)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }

    if (-not $hasStreetPixel) {
        throw 'Desktop window is present but did not compose any street-scene pixels.'
    }

    $center = New-Object DesktopVisibilityNative+Point
    $center.X = $bounds.Left + [int]($width / 2)
    $center.Y = $bounds.Top + [int]($height / 2)

    Write-Output "Desktop visibility passed for PID $($process.Id): ${width}x${height}."

    if ($EnterStore) {
        $originalCursor = New-Object DesktopVisibilityNative+Point
        if (-not [DesktopVisibilityNative]::GetPhysicalCursorPos([ref]$originalCursor)) {
            throw 'Could not preserve the current cursor position for the store-entry smoke test.'
        }

        try {
            [void][DesktopVisibilityNative]::SetForegroundWindow($window)
            Start-Sleep -Milliseconds 100
            if (-not [DesktopVisibilityNative]::ClickPhysicalPoint($center.X, $center.Y)) {
                throw 'Could not send a physical storefront click.'
            }
        }
        finally {
            [void][DesktopVisibilityNative]::SetPhysicalCursorPos($originalCursor.X, $originalCursor.Y)
        }

        $storeDeadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        $storeBounds = New-Object DesktopVisibilityNative+Rect
        do {
            Start-Sleep -Milliseconds 100
            $process.Refresh()
            if ($process.HasExited) {
                throw "Desktop process exited with code $($process.ExitCode) while entering the store."
            }

            if (-not $process.Responding) {
                continue
            }

            [void][DesktopVisibilityNative]::GetPhysicalWindowRect($window, [ref]$storeBounds)
            $storeWidth = $storeBounds.Right - $storeBounds.Left
            $storeHeight = $storeBounds.Bottom - $storeBounds.Top
            if ($storeWidth -gt $width -and $storeHeight -gt $height) {
                break
            }
        } while ([DateTime]::UtcNow -lt $storeDeadline)

        if (-not $process.Responding) {
            throw 'Desktop process stopped responding after the storefront click.'
        }

        if ($storeWidth -le $width -or $storeHeight -le $height) {
            throw "Store navigation did not resize the desktop surface: ${storeWidth}x${storeHeight}."
        }

        $storeBitmap = [Drawing.Bitmap]::new($storeWidth, $storeHeight)
        $storeGraphics = [Drawing.Graphics]::FromImage($storeBitmap)
        $storeColors = [Collections.Generic.HashSet[int]]::new()
        foreach ($hex in @('#23262C', '#2D323A', '#F1B844')) {
            [void]$storeColors.Add([Drawing.ColorTranslator]::FromHtml($hex).ToArgb())
        }

        $hasStorePixel = $false
        try {
            do {
                $storeGraphics.CopyFromScreen(
                    $storeBounds.Left,
                    $storeBounds.Top,
                    0,
                    0,
                    $storeBitmap.Size,
                    [Drawing.CopyPixelOperation]::SourceCopy)
                for ($y = 0; $y -lt $storeHeight -and -not $hasStorePixel; $y += 2) {
                    for ($x = 0; $x -lt $storeWidth; $x += 2) {
                        if ($storeColors.Contains($storeBitmap.GetPixel($x, $y).ToArgb())) {
                            $hasStorePixel = $true
                            break
                        }
                    }
                }

                if (-not $hasStorePixel) {
                    Start-Sleep -Milliseconds 100
                }
            } while (-not $hasStorePixel -and [DateTime]::UtcNow -lt $storeDeadline)
        }
        finally {
            $storeGraphics.Dispose()
            $storeBitmap.Dispose()
        }

        $process.Refresh()
        if (-not $process.Responding -or -not $hasStorePixel) {
            throw 'Store surface did not remain responsive with rendered combat pixels.'
        }

        Write-Output "Store surface visibility passed for PID $($process.Id): ${storeWidth}x${storeHeight}."
    }
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
