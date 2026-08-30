param(
    [Parameter(Mandatory)]
    [string]$SummaryPath,
    [Parameter(Mandatory)]
    [string]$OutputPath,
    [int]$ShuffleSeed = 20260831
)

$ErrorActionPreference = 'Stop'

if (!(Test-Path $SummaryPath)) {
    throw "Replay summary not found: $SummaryPath"
}

$summary = Get-Content -Raw -Encoding utf8 $SummaryPath | ConvertFrom-Json
$results = @($summary.Results)
if ($results.Count -eq 0) {
    throw "Replay summary has no results: $SummaryPath"
}

# A reviewer must see the actual first-screen answer and its available context,
# but never the expected sense, pass/fail metrics, or the curated abstention
# label. Keep the review order deterministic so a completed form can be
# reconciled with the source evidence without revealing the answer key.
$random = [System.Random]::new($ShuffleSeed)
$reviewCards = @($results |
    Sort-Object { $random.Next() } |
    ForEach-Object -Begin { $ordinal = 0 } -Process {
        $ordinal++
        $context = [string]$_.ContextText
        [pscustomobject]@{
            ReviewId = ('card-{0:D2}' -f $ordinal)
            SourceIndex = [int]$_.Index
            TargetWord = [string]$_.RecognizedText
            Context = if ([string]::IsNullOrWhiteSpace($context)) { '（本次未取得可用上下文）' } else { $context }
            FirstScreenMeaning = [string]$_.PrimaryDefinition
            ReviewerDecision = ''
            ReviewerRationale = ''
        }
    })

if (@($reviewCards | Where-Object { [string]::IsNullOrWhiteSpace($_.TargetWord) -or [string]::IsNullOrWhiteSpace($_.FirstScreenMeaning) }).Count -gt 0) {
    throw 'Every review card must have a target word and rendered first-screen meaning.'
}

$packet = [pscustomobject]@{
    GeneratedAt = (Get-Date).ToUniversalTime().ToString('o')
    SourceSummaryPath = (Resolve-Path $SummaryPath).Path
    ReviewInstructions = @(
        'Read only the target word, available context, and actual first-screen meaning.',
        'For each card choose: understandable, unclear, or should-abstain.',
        'Record a short reason. Do not open the labelled replay manifest until the form is complete.',
        'A card is understandable only if its first-screen meaning lets you continue reading this sentence.'
    )
    Cards = $reviewCards
    Integrity = [pscustomobject]@{
        CardCount = $reviewCards.Count
        ContainsExpectedSense = $false
        ContainsExpectedOutcome = $false
        ContainsAutomatedVerdict = $false
    }
}

$reviewCardPayload = $reviewCards | ConvertTo-Json -Depth 4
if ($reviewCardPayload -match 'ExpectedSense|ExpectedOutcome|Top1Sense|Abstained') {
    throw 'Blind-review packet unexpectedly contains a labelled verdict field.'
}

$serialized = $packet | ConvertTo-Json -Depth 6

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$serialized | Set-Content -Encoding utf8 $OutputPath
Write-Host "Blind review packet: $OutputPath"
Write-Host "Cards: $($reviewCards.Count); labels excluded: expected sense, expected outcome, automated verdict"
