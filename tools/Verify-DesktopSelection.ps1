param(
    [int]$TimeoutSeconds = 25,
    [int]$SettleMilliseconds = 2500,
    [int]$Attempts = 3
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $repo 'src\RealtimeTranslate.App\bin\Debug\net8.0-windows10.0.19041.0\RealtimeTranslate.App.exe'
$records = Join-Path $env:LOCALAPPDATA 'RealtimeTranslateCodex\records\lookups.jsonl'
if (!(Test-Path $exe)) { throw "Built app not found: $exe" }

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class DesktopInput {
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
'@

$before = if (Test-Path $records) { (Get-Content -Encoding utf8 $records).Count } else { 0 }
$process = Start-Process -FilePath $exe -ArgumentList '--desktop-test-host' -PassThru
try {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $handle = [IntPtr]::Zero
    while ((Get-Date) -lt $deadline -and $handle -eq [IntPtr]::Zero) {
        Start-Sleep -Milliseconds 200
        $candidate = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($candidate) { $handle = $candidate.MainWindowHandle }
    }
    if ($handle -eq [IntPtr]::Zero) { throw 'Desktop test host window did not appear before timeout.' }
    # A visible window can precede completion of application startup; allow the
    # production low-level hook to attach before injecting the gesture.
    Start-Sleep -Milliseconds 750
    $rect = New-Object DesktopInput+RECT
    if (![DesktopInput]::GetWindowRect($handle, [ref]$rect)) { throw 'Unable to read host window bounds.' }
    # The loaded fixture centers the target word; a sentence below it exercises
    # the production context crop/OCR and sense-ranking path.
    # Stay within its 520x96 bounds rather than dragging through the title bar
    # or blank lower half of a DPI-scaled window.
    $startX = [int](($rect.Left + $rect.Right) / 2 - 180)
    $startY = [int](($rect.Top + $rect.Bottom) / 2 - 24)
    $endX = $startX + 360
    $endY = $startY + 48
    $record = $null
    for ($attempt = 1; $attempt -le $Attempts -and $null -eq $record; $attempt++) {
        $screenWidth = [DesktopInput]::GetSystemMetrics(0)
        $screenHeight = [DesktopInput]::GetSystemMetrics(1)
        $moveFlags = 0x0001 -bor 0x8000
        $toAbsolute = {
            param([int]$coordinate, [int]$size)
            $bounded = [Math]::Min([Math]::Max($coordinate, 0), $size - 1)
            [uint32][Math]::Round($bounded * 65535.0 / ($size - 1))
        }
        [DesktopInput]::mouse_event($moveFlags, (& $toAbsolute $startX $screenWidth), (& $toAbsolute $startY $screenHeight), 0, [UIntPtr]::Zero)
        [DesktopInput]::mouse_event(0x0008, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 250
        foreach ($step in 1..24) {
            $x = $startX + [int](($endX - $startX) * $step / 24)
            $y = $startY + [int](($endY - $startY) * $step / 24)
            [DesktopInput]::mouse_event($moveFlags, (& $toAbsolute $x $screenWidth), (& $toAbsolute $y $screenHeight), 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 12
        }
        Start-Sleep -Milliseconds 250
        [DesktopInput]::mouse_event(0x0010, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds $SettleMilliseconds
        $lines = if (Test-Path $records) { @(Get-Content -Encoding utf8 $records) } else { @() }
        if ($lines.Count -gt $before) { $record = $lines[-1] | ConvertFrom-Json }
    }
    if ($null -eq $record) {
        $lines = if (Test-Path $records) { @(Get-Content -Encoding utf8 $records) } else { @() }
        [pscustomobject]@{ WindowHandle = $handle; RecordCountBefore = $before; RecordCountAfter = $lines.Count; Result = 'no-record' } | Format-List
        throw "Right-drag completed but no lookup record was appended (before=$before after=$($lines.Count)). Check that the process owns an interactive desktop hook."
    }
    $validSelection = $record.selection.width -gt 0 -and $record.selection.height -gt 0
    $hasScreenshot = ![string]::IsNullOrWhiteSpace($record.screenshotPath) -and (Test-Path $record.screenshotPath)
    $hasDefinition = @($record.definitions | Where-Object { $_ -and $_ -notmatch '未识别|未找到|失败' }).Count -gt 0
    $hasContext = ![string]::IsNullOrWhiteSpace([string]$record.contextText)
    [pscustomobject]@{ WindowHandle = $handle; Selection = "$($record.selection.x),$($record.selection.y) $($record.selection.width)x$($record.selection.height)"; RecognizedText = $record.recognizedText; ContextText = $record.contextText; ScreenshotExists = $hasScreenshot; DefinitionFound = $hasDefinition; ContextCaptured = $hasContext } | Format-List
    if (!$validSelection -or !$hasScreenshot -or !$hasDefinition -or !$hasContext) { throw 'Desktop selection produced incomplete context-aware lookup evidence.' }
}
finally {
    if (!$process.HasExited) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
}
