param(
    [int]$PauseSeconds = 5,
    [int]$MaxIterations = 80
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$log = Join-Path $repo 'docs\agentic-loop.log'
$status = Join-Path $repo 'docs\agentic-loop-status.md'
$pidPath = Join-Path $repo 'docs\agentic-loop.pid'
$done = Join-Path $repo 'docs\agent-loop.accepted'
$autoAccepted = Join-Path $repo 'docs\agent-loop.auto-accepted.md'
$reviewLog = Join-Path $repo 'artifacts\agent-review-log.md'
$reportDirectory = Join-Path $repo 'docs\loop-reports'

function Repair-Mojibake([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return $text }
    try {
        $gb = [Text.Encoding]::GetEncoding(936)
        return [Text.Encoding]::UTF8.GetString($gb.GetBytes($text))
    } catch { return $text }
}

Set-Content -Encoding ascii $pidPath $PID
$iteration = 0

while (-not (Test-Path $done) -and -not (Test-Path $autoAccepted) -and $iteration -lt $MaxIterations) {
    $iteration++
    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $headBefore = (& git -C $repo rev-parse HEAD).Trim()
    @("# Agentic Loop Status", "", "Updated: $stamp", "", "- Run: agent iteration $iteration", "- Stage: improving usable context OCR and learning-card quality", "- Goal: reliable sentence understanding plus memorable word learning", "- Scope: general desktop OCR baseline plus optional game-dialogue enhancement", "- Independent review: required after each relevant committed change", "- Completion: automatic threshold or user acceptance", "- Next: inspect docs/loop-reports/iteration-$iteration.md") | Set-Content -Encoding utf8 $status
    Add-Content -Encoding utf8 $log "[$stamp] START agent iteration=$iteration"

    $prompt = @"
You are the autonomous engineering agent for this repository. Follow docs/agentic-loop-goal.md. Keep the OCR foundation general for arbitrary desktop text; optimize the Reverse: 1999 dialogue scenario through a detected, optional enhancement layer, never fixed coordinates as the only mode. The current highest-value problem is usable context OCR and then memorable word learning. Choose one evidence-backed hypothesis, make a reversible architecture or implementation change, and preserve before/after real examples. Do not treat non-empty OCR, longer cards, generic AI prose, or unit tests as success. Measure actual text fidelity, context safety, and learning usefulness. Do not spend the iteration on unrelated maintenance. Build and run only needed verification, commit coherent changes, and update docs/agentic-loop-status.md. You MUST also create docs/loop-reports/iteration-$iteration.md containing the hypothesis, exact changes and commit, before/after examples, metrics, regressions, independent-review result, and next question. If all criteria are met, create docs/agent-loop.auto-accepted.md; otherwise keep working. Never create or modify docs/agent-loop.accepted. Do not modify unrelated user changes.
"@
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $output = $prompt | & codex exec -C $repo --dangerously-bypass-approvals-and-sandbox - 2>&1 | Out-String
    $ErrorActionPreference = $previousErrorAction
    Add-Content -Encoding utf8 $log "[$(Get-Date -Format s)] OUTPUT iteration=$iteration`r`n$(Repair-Mojibake $output)"

    $headAfter = (& git -C $repo rev-parse HEAD).Trim()
    if ($headAfter -ne $headBefore) {
        $reviewPrompt = @"
You are an independent, read-only and deliberately skeptical reviewer. Review the latest committed change against docs/agentic-loop-goal.md. Inspect real screenshot evidence, manual transcription data, before/after OCR outputs, actual rendered cards, learning-insight provenance, and the change itself. Do not edit files. Reject claims based only on non-empty OCR, a longer card, generic AI prose, unit tests, or the implementer's narrative. Check whether context is genuinely readable and safe, and whether learning content helps remember the word itself rather than repeating a Chinese gloss or inventing a story. Return exactly: DECISION: accept|reject|insufficient-evidence; then concise evidence, metric validity, factual/provenance problems, regressions, and the single next question.
"@
        $previousErrorAction = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $review = $reviewPrompt | & codex exec -C $repo --dangerously-bypass-approvals-and-sandbox - 2>&1 | Out-String
        $ErrorActionPreference = $previousErrorAction
        $review = Repair-Mojibake $review
        Add-Content -Encoding utf8 $log "[$(Get-Date -Format s)] REVIEW iteration=$iteration commit=$headAfter`r`n$review"
        Add-Content -Encoding utf8 $reviewLog "`n## $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | iteration $iteration | $headAfter`n`n$review"
    }
    if (!(Test-Path $reportDirectory)) { New-Item -ItemType Directory -Path $reportDirectory | Out-Null }
    $reportPath = Join-Path $reportDirectory "iteration-$iteration.md"
    $reportTail = (Repair-Mojibake $output) -split "`r?`n" | Select-Object -Last 120
    @("# Loop Iteration $iteration", "", "- Started: $stamp", "- Commit before: $headBefore", "- Commit after: $headAfter", "- Independent review: " + $(if($headAfter -ne $headBefore){"recorded in artifacts/agent-review-log.md"}else{"not triggered; no commit"}), "", "## Agent report/output", "", ($reportTail -join "`n")) | Set-Content -Encoding utf8 $reportPath
    Start-Sleep -Seconds $PauseSeconds
}

if (Test-Path $autoAccepted) {
    @("# Agentic Loop Status", "", "Updated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')", "", "- Run: stopped after automatic OCR-and-learning acceptance", "- Stage: two independent reviewer agents accepted the measured goal", "- Note: this is not user final acceptance", "- Evidence: docs/agent-loop.auto-accepted.md") | Set-Content -Encoding utf8 $status
    Add-Content -Encoding utf8 $log "[$(Get-Date -Format s)] AUTO-ACCEPTED after iterations=$iteration"
}
else {
    @("# Agentic Loop Status", "", "Updated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')", "", "- Run: stopped at safety cap", "- Stage: maximum iteration count reached without automatic acceptance", "- Next: inspect agentic-loop.log and resume only with a new focused goal") | Set-Content -Encoding utf8 $status
    Add-Content -Encoding utf8 $log "[$(Get-Date -Format s)] STOPPED after iterations=$iteration (safety cap or user acceptance)"
}
Remove-Item $pidPath -ErrorAction SilentlyContinue
