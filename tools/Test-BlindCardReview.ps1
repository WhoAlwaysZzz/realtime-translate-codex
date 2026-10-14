param(
    [Parameter(Mandatory)]
    [string]$ReviewPath,
    [Parameter(Mandatory)]
    [string]$SummaryPath,
    [Parameter(Mandatory)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

foreach ($path in @($ReviewPath, $SummaryPath)) {
    if (!(Test-Path $path)) {
        throw "Required input not found: $path"
    }
}

# This is deliberately a post-review tool.  It does not write to the review
# packet and only uses the labelled summary after an independent reviewer has
# completed every decision and rationale.
$review = Get-Content -Raw -Encoding utf8 $ReviewPath | ConvertFrom-Json
$summary = Get-Content -Raw -Encoding utf8 $SummaryPath | ConvertFrom-Json
$cards = @($review.Cards)
$results = @($summary.Results)

if ($cards.Count -eq 0 -or $results.Count -eq 0) {
    throw 'The review packet and labelled replay summary must both contain cards.'
}

$sourceByIndex = @{}
foreach ($result in $results) {
    $index = [int]$result.Index
    if ($sourceByIndex.ContainsKey($index)) {
        throw "The labelled replay summary has duplicate source index $index."
    }
    $sourceByIndex[$index] = $result
}

$allowedDecisions = @('understandable', 'unclear', 'should-abstain')
$seenIndices = [System.Collections.Generic.HashSet[int]]::new()
$incomplete = @()
$invalid = @()
$mismatches = @()
$understandable = 0
$unclear = 0
$shouldAbstain = 0
$correctlyUnderstandable = 0
$correctlyAbstained = 0
$expectedSenseCards = 0
$expectedAbstentionCards = 0

foreach ($card in $cards) {
    $reviewId = [string]$card.ReviewId
    $index = [int]$card.SourceIndex
    if (!$seenIndices.Add($index)) {
        $invalid += "$reviewId duplicates source index $index"
        continue
    }
    if (!$sourceByIndex.ContainsKey($index)) {
        $invalid += "$reviewId references source index $index, which is absent from the replay summary"
        continue
    }

    $decision = ([string]$card.ReviewerDecision).Trim().ToLowerInvariant()
    $rationale = ([string]$card.ReviewerRationale).Trim()
    if ([string]::IsNullOrWhiteSpace($decision) -or [string]::IsNullOrWhiteSpace($rationale)) {
        $incomplete += "$reviewId (source $index)"
        continue
    }
    if ($decision -notin $allowedDecisions) {
        $invalid += "$reviewId has invalid decision '$decision'"
        continue
    }

    switch ($decision) {
        'understandable' { $understandable++ }
        'unclear' { $unclear++ }
        'should-abstain' { $shouldAbstain++ }
    }

    $source = $sourceByIndex[$index]
    $expectsAbstention = ![string]::IsNullOrWhiteSpace([string]$source.ExpectedOutcome) -and ([string]$source.ExpectedOutcome).Trim().ToLowerInvariant() -eq 'abstain'
    $expectedDecision = if ($expectsAbstention) { 'should-abstain' } else { 'understandable' }
    if ($expectsAbstention) { $expectedAbstentionCards++ } else { $expectedSenseCards++ }

    if ($decision -eq $expectedDecision) {
        if ($expectsAbstention) { $correctlyAbstained++ } else { $correctlyUnderstandable++ }
    }
    else {
        $mismatches += [pscustomobject]@{
            ReviewId = $reviewId
            SourceIndex = $index
            ReviewerDecision = $decision
            RequiredDecision = $expectedDecision
            ReviewerRationale = $rationale
        }
    }
}

$missingSourceCards = @($sourceByIndex.Keys | Where-Object { !$seenIndices.Contains($_) } | Sort-Object)
if ($missingSourceCards.Count -gt 0) {
    $invalid += "Review packet is missing replay source indices: $($missingSourceCards -join ', ')"
}

$complete = $incomplete.Count -eq 0 -and $invalid.Count -eq 0 -and $cards.Count -eq $results.Count
$passed = $complete -and $correctlyUnderstandable -ge 16 -and $correctlyAbstained -ge 4
$report = [pscustomobject]@{
    GeneratedAt = (Get-Date).ToUniversalTime().ToString('o')
    ReviewPath = (Resolve-Path $ReviewPath).Path
    SummaryPath = (Resolve-Path $SummaryPath).Path
    ReviewIntegrity = [pscustomobject]@{
        ReviewCards = $cards.Count
        ReplayCards = $results.Count
        Complete = $complete
        IncompleteCards = $incomplete
        InvalidEntries = $invalid
    }
    ReviewerDecisions = [pscustomobject]@{
        Understandable = $understandable
        Unclear = $unclear
        ShouldAbstain = $shouldAbstain
    }
    AcceptanceCheck = [pscustomobject]@{
        RequiredUnderstandable = 16
        RequiredAbstentions = 4
        ExpectedSenseCards = $expectedSenseCards
        ExpectedAbstentionCards = $expectedAbstentionCards
        CorrectlyUnderstandable = $correctlyUnderstandable
        CorrectlyAbstained = $correctlyAbstained
        Passed = $passed
        Mismatches = $mismatches
    }
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}
$report | ConvertTo-Json -Depth 7 | Set-Content -Encoding utf8 $OutputPath

Write-Host "Blind review reconciliation: $OutputPath"
Write-Host "Complete: $complete; understood: $correctlyUnderstandable/16; abstentions: $correctlyAbstained/4; passed: $passed"
if (!$complete) {
    exit 2
}
if (!$passed) {
    exit 1
}
