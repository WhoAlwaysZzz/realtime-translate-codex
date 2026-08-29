param(
    [int]$Iterations = 10,
    [int]$PauseSeconds = 2
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $repo 'src\RealtimeTranslate.App\bin\Debug\net8.0-windows10.0.19041.0\RealtimeTranslate.App.exe'
$records = Join-Path $env:LOCALAPPDATA 'RealtimeTranslateCodex\records\lookups.jsonl'
$status = Join-Path $repo 'docs\agent-loop-status.md'
$pidPath = Join-Path $repo 'docs\agent-loop.pid'
$logPath = Join-Path $repo 'docs\agent-loop.log'
Set-Content -Encoding ascii $pidPath $PID
Add-Content -Encoding utf8 $logPath "[$(Get-Date -Format s)] runner started pid=$PID iterations=$Iterations"

for ($i = 1; $i -le $Iterations; $i++) {
    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    @("# Agent Loop Status", "", "Updated: $stamp", "", "- Run: iteration $i of $Iterations", "- Stage: archive replay (PNG -> OCR -> lookup -> card)", "- Records: $records", "- Next: wait for replay and start next iteration") | Set-Content -Encoding utf8 $status
    $index = ($i - 1) * 25
    $child = Start-Process -FilePath $exe -ArgumentList @('--replay', $records, '--index', $index) -PassThru
    while (-not $child.HasExited) {
        $heartbeat = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
        Add-Content -Encoding utf8 $logPath "[$heartbeat] heartbeat iteration=$i childPid=$($child.Id)"
        Start-Sleep -Seconds 2
    }
    Add-Content -Encoding utf8 $logPath "[$(Get-Date -Format s)] iteration=$i exitCode=$($child.ExitCode)"
    Start-Sleep -Seconds $PauseSeconds
}

$stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
@("# Agent Loop Status", "", "Updated: $stamp", "", "- Run: completed $Iterations iterations", "- Stage: awaiting result review", "- Next: classify replay output") | Set-Content -Encoding utf8 $status
Remove-Item $pidPath -ErrorAction SilentlyContinue
Add-Content -Encoding utf8 $logPath "[$stamp] runner completed"
