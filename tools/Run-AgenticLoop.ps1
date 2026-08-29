param([int]$PauseSeconds = 5)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$log = Join-Path $repo 'docs\agentic-loop.log'
$status = Join-Path $repo 'docs\agentic-loop-status.md'
$pidPath = Join-Path $repo 'docs\agentic-loop.pid'
$done = Join-Path $repo 'docs\agent-loop.accepted'

function Repair-Mojibake([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return $text }
    try {
        $gb = [Text.Encoding]::GetEncoding(936)
        return [Text.Encoding]::UTF8.GetString($gb.GetBytes($text))
    } catch { return $text }
}
Set-Content -Encoding ascii $pidPath $PID
$iteration = 0

while (-not (Test-Path $done)) {
    $iteration++
    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    @("# Agentic Loop Status", "", "Updated: $stamp", "", "- Run: agent iteration $iteration", "- Stage: agent is inspecting, implementing, testing, and reflecting", "- Goal: deliver a reliable English game-reading assistant", "- Completion: user acceptance marker only", "- Next: inspect agent log for its decision") | Set-Content -Encoding utf8 $status
    Add-Content -Encoding utf8 $log "[$stamp] START agent iteration=$iteration"
    $prompt = @"
You are the autonomous engineering agent for this repository. Continue the full product goal in docs/agentic-loop-goal.md, not merely one replay milestone. Read the status, logs, goal, and code before acting. Inspect the latest evidence, identify the single highest-value remaining blocker, implement the smallest reversible fix, build and run a real verification, and commit coherent changes. Do not ask the user for routine decisions. Update docs/agent-loop-status.md with facts, evidence, and next action. Never create or modify docs/agent-loop.accepted; only the user can mark final acceptance. Do not modify unrelated user changes.
"@
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $output = $prompt | & codex exec -C $repo --dangerously-bypass-approvals-and-sandbox - 2>&1 | Out-String
    $ErrorActionPreference = $previousErrorAction
    $output = Repair-Mojibake $output
    Add-Content -Encoding utf8 $log "[$(Get-Date -Format s)] OUTPUT iteration=$iteration`r`n$output"
    Start-Sleep -Seconds $PauseSeconds
}

Add-Content -Encoding utf8 $log "[$(Get-Date -Format s)] DONE after iterations=$iteration"
Remove-Item $pidPath -ErrorAction SilentlyContinue
