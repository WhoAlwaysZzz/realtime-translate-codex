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
    @("# Agentic Loop Status", "", "Updated: $stamp", "", "- Run: agent iteration $iteration", "- Stage: experimenting with card content, translation, and presentation", "- Goal: materially improve what the lookup card tells the user", "- Completion: user acceptance marker only", "- Next: inspect agent log for the current card-quality experiment") | Set-Content -Encoding utf8 $status
    Add-Content -Encoding utf8 $log "[$stamp] START agent iteration=$iteration"
    $prompt = @"
You are the autonomous engineering agent for this repository. The ONLY product goal is improving lookup-card content quality as defined in docs/agentic-loop-goal.md. Do not spend an iteration revalidating OCR, replay plumbing, tray behavior, hotkeys, screenshot persistence, or other already-established infrastructure unless it directly blocks a card-content experiment. Read the current card pipeline and real game examples. Choose one high-value content hypothesis, implement a reversible architecture, translation-source, prompting, ranking, or presentation experiment, and preserve before/after card examples. Judge whether the result helps a user understand the selected word in its sentence; passing old technical tests is not evidence of product improvement. Build and run only the verification needed for that experiment, commit coherent changes, and update docs/agent-loop-status.md with the hypothesis, before/after examples, observed improvement or regression, and the next card-quality question. Do not ask the user for routine decisions. Never create or modify docs/agent-loop.accepted; only the user can mark final acceptance. Do not modify unrelated user changes.
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
