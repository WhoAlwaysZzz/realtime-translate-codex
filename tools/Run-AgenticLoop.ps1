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
    @("# Agentic Loop Status", "", "Updated: $stamp", "", "- Run: agent iteration $iteration", "- Stage: improving usable context OCR and learning-card quality", "- Goal: reliable sentence understanding plus memorable word learning", "- Independent review: required after each relevant committed change", "- Completion: automatic threshold or user acceptance", "- Next: inspect agent log for the current OCR/learning-card experiment") | Set-Content -Encoding utf8 $status
    Add-Content -Encoding utf8 $log "[$stamp] START agent iteration=$iteration"

    $prompt = @"
You are the autonomous engineering agent for this repository. The product goal is the two-part objective in docs/agentic-loop-goal.md: usable game-dialogue context OCR AND memorable, evidence-backed word learning. Read the current pipeline, real game screenshots, card output, and evidence. Choose one highest-value hypothesis, which may change OCR architecture, context safety, dictionary/insight data architecture, or card information hierarchy. Preserve before/after outputs against manually transcribed screenshots and learning evaluation cases. Do not treat non-empty OCR, a longer card, generic AI prose, or passing unit tests as success. Measure the relevant OCR or learning quality gate and regressions. Context that fails its trust gate must not steer a main meaning. Learning insights must explain the word beyond the current Chinese gloss and cite reliable data; omit weak insights. Do not spend the iteration on tray, hotkeys, generic maintenance, or replay plumbing. Build and run only the needed verification, commit coherent changes, and update docs/agentic-loop-status.md with the hypothesis, metric delta, real examples, regressions, and next question. If all automatic completion criteria are actually met, create docs/agent-loop.auto-accepted.md with exact evidence and two accepted reviewer-report paths; otherwise keep working. Never create or modify docs/agent-loop.accepted. Do not modify unrelated user changes.
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
