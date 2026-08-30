param(
    [int]$SampleCount = 12,
    [int]$StartIndex = 0,
    [int]$DelayMilliseconds = 250,
    [int]$ReplaySettleMilliseconds = 1000,
    [int]$ReplayTimeoutSeconds = 30,
    [int]$MaxRunSeconds = 0,
    [string]$SummaryPath,
    [string]$ExpectedManifestPath,
    [string]$CandidateManifestPath,
    [int[]]$Indices,
    [switch]$Stratified
)

$ErrorActionPreference = 'Stop'
$null = Add-Type -AssemblyName System.Drawing
$repo = Split-Path -Parent $PSScriptRoot
$ExpectedManifestPath = if ($ExpectedManifestPath) { $ExpectedManifestPath } else { Join-Path $repo 'tests\Data\replay-expected.json' }
$exe = Join-Path $repo 'src\RealtimeTranslate.App\bin\Debug\net8.0-windows10.0.19041.0\RealtimeTranslate.App.exe'
$records = Join-Path $env:LOCALAPPDATA 'RealtimeTranslateCodex\records\lookups.jsonl'
$outputRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("RealtimeTranslateCodex-replay-" + [Guid]::NewGuid().ToString('N'))

if (!(Test-Path $exe)) { throw "Built app not found: $exe" }
if (!(Test-Path $records)) { throw "Lookup archive not found: $records" }
if ($SampleCount -lt 1) { throw 'SampleCount must be positive.' }
if ($ReplayTimeoutSeconds -lt 1) { throw 'ReplayTimeoutSeconds must be positive.' }
if ($MaxRunSeconds -lt 0) { throw 'MaxRunSeconds must be zero (unbounded) or positive.' }
$expectedByIndex = @{}
if ($ExpectedManifestPath -and (Test-Path $ExpectedManifestPath)) {
    # Parse the complete JSON document first. Piping line-by-line can wrap a
    # top-level JSON array as a nested Object[] on some PowerShell versions.
    $manifest = Get-Content -Raw -Encoding utf8 $ExpectedManifestPath | ConvertFrom-Json
    foreach ($entry in @($manifest)) {
        if ($null -eq $entry.index -or [string]::IsNullOrWhiteSpace([string]$entry.expectedText)) { throw "Expected manifest entries require index and expectedText: $ExpectedManifestPath" }
        $expectedByIndex[[int]$entry.index] = $entry
    }
}
$candidateIndices = $null
if ($CandidateManifestPath) {
    if (!(Test-Path $CandidateManifestPath)) { throw "Candidate manifest not found: $CandidateManifestPath" }
    $candidateDocument = Get-Content -Raw -Encoding utf8 $CandidateManifestPath | ConvertFrom-Json
    $candidateValues = if ($candidateDocument -is [System.Array]) { @($candidateDocument) } elseif ($candidateDocument.indices) { @($candidateDocument.indices) } else { @() }
    if ($candidateValues.Count -eq 0) { throw "Candidate manifest must contain a non-empty indices array: $CandidateManifestPath" }
    $candidateIndices = @($candidateValues | ForEach-Object {
        $parsed = 0
        if (![int]::TryParse([string]$_, [ref]$parsed) -or $parsed -lt 0) { throw "Candidate manifest indices must be non-negative integers: $CandidateManifestPath" }
        $parsed
    } | Select-Object -Unique)
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$archive = @(Get-Content -Encoding utf8 $records | ForEach-Object {
    try { $_ | ConvertFrom-Json } catch { $null }
} | Where-Object { $_ -and $_.screenshotPath -and $_.selection })
$available = if ($candidateIndices) {
    @($candidateIndices | Where-Object { $_ -lt $archive.Count -and $_ -ge $StartIndex })
} else {
    @(0..($archive.Count - 1) | Where-Object { $_ -ge $StartIndex })
}
if ($Indices) {
    $indices = @($Indices | Select-Object -Unique)
    $invalidIndices = @($indices | Where-Object { $_ -lt 0 -or $_ -ge $archive.Count })
    if ($invalidIndices.Count -gt 0) { throw "Replay indices out of range: $($invalidIndices -join ', ')" }
} elseif ($Stratified) {
    # Deterministic evenly spaced picks cover the selected pool without relying on random state.
    $indices = @(0..([Math]::Min($SampleCount, $available.Count) - 1) | ForEach-Object {
        $available[[Math]::Floor($_ * $available.Count / [double][Math]::Min($SampleCount, $available.Count))]
    } | Select-Object -Unique)
} else {
    $indices = @($available | Select-Object -First $SampleCount)
}
if (@($indices).Count -eq 0) { throw "No replayable records at or after index $StartIndex." }

$runStartedAt = Get-Date
$runTimeLimitReached = $false
$results = @(foreach ($index in $indices) {
    if ($MaxRunSeconds -gt 0 -and ((Get-Date) - $runStartedAt).TotalSeconds -ge $MaxRunSeconds) {
        $runTimeLimitReached = $true
        break
    }

    # Give every child its own record file as well as every verifier run its
    # own root. This prevents a parallel verifier from shifting record order.
    $processOutputRoot = Join-Path $outputRoot "sample-$index"
    $processOutputRecords = Join-Path $processOutputRoot 'records\lookups.jsonl'
    $process = Start-Process -FilePath $exe -ArgumentList @('--replay', $records, '--index', $index, '--replay-delay-ms', $ReplaySettleMilliseconds, '--replay-output', $processOutputRoot) -PassThru
    $remainingRunMilliseconds = if ($MaxRunSeconds -gt 0) {
        [Math]::Max(1, [int](($MaxRunSeconds - ((Get-Date) - $runStartedAt).TotalSeconds) * 1000))
    } else { [int]::MaxValue }
    $waitMilliseconds = [Math]::Min($ReplayTimeoutSeconds * 1000, $remainingRunMilliseconds)
    $replayTimedOut = -not $process.WaitForExit($waitMilliseconds)
    if ($replayTimedOut) {
        Stop-Process -Id $process.Id -Force
        $process.WaitForExit()
        if ($MaxRunSeconds -gt 0 -and ((Get-Date) - $runStartedAt).TotalSeconds -ge $MaxRunSeconds) {
            $runTimeLimitReached = $true
        }
    }
    Start-Sleep -Milliseconds $DelayMilliseconds
    # The replay app has exited, but Windows can expose the appended JSONL line
    # just before its final bytes become readable.  Treat a partial line as not
    # yet available instead of converting it into a false replay failure.
    $newRecord = $null
    $recordDeadline = (Get-Date).AddSeconds(30)
    do {
        if (Test-Path $processOutputRecords) {
            try {
                $newRecord = Get-Content -Raw -Encoding utf8 $processOutputRecords | ConvertFrom-Json
            }
            catch {
                $newRecord = $null
            }
        }
        if (!$newRecord -and (Get-Date) -lt $recordDeadline) {
            Start-Sleep -Milliseconds 50
        }
    } while (!$newRecord -and (Get-Date) -lt $recordDeadline)
    $sourceText = [string]$archive[$index].recognizedText
    $replayedText = if ($newRecord) { [string]$newRecord.recognizedText } else { '' }
    $quality = [pscustomobject]@{ Screenshot = 'missing'; Selection = 'unknown'; CropPixels = 0; ContrastRange = 0; EdgeDensity = 0; GreenRatio = 0; ContentClass = 'unknown' }
    $screenshotPath = [string]$archive[$index].screenshotPath
    if (Test-Path $screenshotPath) {
        try {
            $bitmap = [System.Drawing.Bitmap]::new($screenshotPath)
            try {
                $selection = $archive[$index].selection
                $selectionRight = [int]$selection.x + [int]$selection.width
                $selectionBottom = [int]$selection.y + [int]$selection.height
                $valid = [int]$selection.x -ge 0 -and [int]$selection.y -ge 0 -and [int]$selection.width -gt 0 -and [int]$selection.height -gt 0 -and $selectionRight -le $bitmap.Width -and $selectionBottom -le $bitmap.Height
                $minLuminance = 255.0
                $maxLuminance = 0.0
                $edgeTransitions = 0
                $greenPixels = 0
                $sampledPixels = 0
                if ($valid) {
                    for ($y = [int]$selection.y; $y -lt $selectionBottom; $y++) {
                        $previousLuminance = $null
                        for ($x = [int]$selection.x; $x -lt $selectionRight; $x++) {
                            $pixel = $bitmap.GetPixel($x, $y)
                            $luminance = $pixel.R * 0.299 + $pixel.G * 0.587 + $pixel.B * 0.114
                            $minLuminance = [Math]::Min($minLuminance, $luminance)
                            $maxLuminance = [Math]::Max($maxLuminance, $luminance)
                            if ($null -ne $previousLuminance -and [Math]::Abs($luminance - $previousLuminance) -ge 35) { $edgeTransitions++ }
                            if ($pixel.G -gt ($pixel.R * 1.2) -and $pixel.G -gt ($pixel.B * 1.1)) { $greenPixels++ }
                            $sampledPixels++
                            $previousLuminance = $luminance
                        }
                    }
                }
                $edgeDensity = if ($sampledPixels -gt 0) { $edgeTransitions / [double]$sampledPixels } else { 0 }
                $greenRatio = if ($sampledPixels -gt 0) { $greenPixels / [double]$sampledPixels } else { 0 }
                $contrast = $maxLuminance - $minLuminance
                $contentClass = if (!$valid) { 'invalid' } elseif ($selection.width * $selection.height -lt 400) { 'tiny' } elseif ($contrast -lt 20) { 'low-information' } elseif ($greenRatio -ge 0.12 -and $contrast -ge 60) { 'likely-terminal' } elseif ($edgeDensity -ge 0.08 -and $contrast -ge 40) { 'text-like' } elseif ($edgeDensity -lt 0.03 -and $contrast -ge 50) { 'likely-hud' } else { 'mixed-ui' }
                $quality = [pscustomobject]@{
                    Screenshot = "$($bitmap.Width)x$($bitmap.Height)"
                    Selection = if ($valid) { 'in-bounds' } else { 'out-of-bounds' }
                    CropPixels = [int]$selection.width * [int]$selection.height
                    ContrastRange = [int][Math]::Round($contrast)
                    EdgeDensity = [Math]::Round($edgeDensity, 4)
                    GreenRatio = [Math]::Round($greenRatio, 4)
                    ContentClass = $contentClass
                }
            } finally { $bitmap.Dispose() }
        } catch { $quality = [pscustomobject]@{ Screenshot = 'unreadable'; Selection = 'unknown'; CropPixels = 0; ContrastRange = 0; EdgeDensity = 0; GreenRatio = 0; ContentClass = 'unknown' } }
    }
    $sourceHasOcr = ![string]::IsNullOrWhiteSpace($sourceText)
    $replayHasOcr = ![string]::IsNullOrWhiteSpace($replayedText)
    $replayedContext = if ($newRecord -and $newRecord.PSObject.Properties.Name -contains 'contextText') { [string]$newRecord.contextText } else { '' }
    # DisplayWord is the normalized title that the overlay actually shows.
    # Preserve it in the evidence summary so blind review does not present
    # punctuation or OCR framing marks that the product removed before display.
    $displayWord = if ($newRecord -and $newRecord.PSObject.Properties.Name -contains 'displayWord') { [string]$newRecord.displayWord } else { '' }
    $contextHasText = ![string]::IsNullOrWhiteSpace($replayedContext)
    $primaryDefinition = if ($newRecord -and $newRecord.PSObject.Properties.Name -contains 'primaryDefinition') { [string]$newRecord.primaryDefinition } else { '' }
    $expected = $expectedByIndex[[int]$index]
    $normalize = { param($value) (([string]$value).ToLowerInvariant() -replace '[^a-z0-9]+', '') }
    $ocrExact = if ($expected) { (&$normalize $replayedText) -eq (&$normalize $expected.expectedText) } else { $null }
    $definitionValues = @($newRecord.definitions | ForEach-Object { [string]$_ })
    $hasUsableDefinition = $replayHasOcr -and @($definitionValues | Where-Object {
        $_ -and $_ -notmatch '未识别|未找到|失败'
    }).Count -gt 0
    $abstentionPrefix = '无法仅凭这一句确定；可能是：'
    $top1Sense = if ($expected -and $expected.expectedSense) {
        # A card that explicitly abstains is not a successful first-screen
        # sense decision, even when one of its flattened candidates contains
        # the expected translation. Count only a confident primary answer.
        $hasUsableDefinition -and !$primaryDefinition.StartsWith($abstentionPrefix, [StringComparison]::Ordinal) -and (([string]$definitionValues[0]).ToLowerInvariant().Contains(([string]$expected.expectedSense).ToLowerInvariant()))
    } else { $null }
    $abstentionExpected = $expected -and (([string]$expected.expectedOutcome).ToLowerInvariant() -eq 'abstain')
    $abstained = $null
    if ($abstentionExpected) {
        # The card's explicit uncertainty prefix is the product behavior under
        # review.  Whitespace in a flattened definition is only formatting and
        # must never be treated as evidence that the card honestly abstained.
        $abstained = $primaryDefinition.StartsWith($abstentionPrefix, [StringComparison]::Ordinal)
    }
    [pscustomobject]@{
        Index = $index
        SourceWord = $sourceText
        ExitCode = if ($replayTimedOut) { $null } else { $process.ExitCode }
        ReplayTimedOut = $replayTimedOut
        RecognizedText = $replayedText
        DisplayWord = $displayWord
        ContextText = $replayedContext
        ContextAvailable = $contextHasText
        Screenshot = $quality.Screenshot
        SelectionQuality = $quality.Selection
        CropPixels = $quality.CropPixels
        ContrastRange = $quality.ContrastRange
        EdgeDensity = $quality.EdgeDensity
        GreenRatio = $quality.GreenRatio
        ContentClass = $quality.ContentClass
        FoundDefinition = $hasUsableDefinition
        ExpectedText = if ($expected) { [string]$expected.expectedText } else { $null }
        OcrExact = $ocrExact
        ExpectedSense = if ($expected) { [string]$expected.expectedSense } else { $null }
        Top1Sense = $top1Sense
        ExpectedOutcome = if ($expected) { [string]$expected.expectedOutcome } else { $null }
        PrimaryDefinition = $primaryDefinition
        Abstained = $abstained
        Diagnostic = if ($replayTimedOut) { 'replay timeout' } elseif (!$newRecord) { 'no replay record' } elseif (!$sourceHasOcr -and !$replayHasOcr -and $quality.ContentClass -eq 'invalid') { 'historical empty: invalid selection' } elseif (!$sourceHasOcr -and !$replayHasOcr -and $quality.ContentClass -eq 'tiny') { 'historical empty: tiny selection' } elseif (!$sourceHasOcr -and !$replayHasOcr -and $quality.ContentClass -eq 'low-information') { 'historical empty: low contrast' } elseif (!$sourceHasOcr -and !$replayHasOcr -and $quality.ContentClass -in @('likely-terminal','likely-hud')) { "historical empty: $($quality.ContentClass)" } elseif (!$sourceHasOcr -and !$replayHasOcr) { 'historical OCR empty: input quality unknown' } elseif ($sourceHasOcr -and !$replayHasOcr) { 'OCR regression' } elseif (!$hasUsableDefinition) { 'no definition' } else { 'ok' }
    }
})

$results | Format-Table -AutoSize
$ocrSuccess = @($results | Where-Object { $_.RecognizedText }).Count
$lookupSuccess = @($results | Where-Object { $_.FoundDefinition }).Count
$regressions = @($results | Where-Object Diagnostic -eq 'OCR regression').Count
$recoveries = @($results | Where-Object Diagnostic -eq 'OCR recovery').Count
$replayTimeouts = @($results | Where-Object ReplayTimedOut).Count
$replayFailures = @($results | Where-Object { $_.ReplayTimedOut -or $_.ExitCode -ne 0 }).Count
$labeled = @($results | Where-Object { $null -ne $_.OcrExact })
$ocrExactCount = @($labeled | Where-Object OcrExact).Count
$senseLabeled = @($results | Where-Object { $null -ne $_.Top1Sense })
$top1SenseCount = @($senseLabeled | Where-Object Top1Sense).Count
$contextLabeled = @($senseLabeled | Where-Object ContextAvailable)
$contextTop1SenseCount = @($contextLabeled | Where-Object Top1Sense).Count
$abstentionLabeled = @($results | Where-Object { $null -ne $_.Abstained })
$abstentionCount = @($abstentionLabeled | Where-Object Abstained).Count
"Summary: samples=$($results.Count); ocr_nonempty=$ocrSuccess; dictionary_found=$lookupSuccess; ocr_regressions=$regressions; ocr_recoveries=$recoveries; replay_timeouts=$replayTimeouts; replay_failures=$replayFailures; run_time_limit_reached=$runTimeLimitReached"
if ($labeled.Count -gt 0) { "Labeled accuracy: ocr_exact=$ocrExactCount/$($labeled.Count); top1_sense=$top1SenseCount/$($senseLabeled.Count)" }
if ($senseLabeled.Count -gt 0) { "Context coverage: context_available=$($contextLabeled.Count)/$($senseLabeled.Count); context_top1_sense=$contextTop1SenseCount/$($contextLabeled.Count)" }
if ($abstentionLabeled.Count -gt 0) { "Abstention accuracy: abstained=$abstentionCount/$($abstentionLabeled.Count)" }

if ($SummaryPath) {
    $summary = [pscustomobject]@{
        GeneratedAt = (Get-Date).ToUniversalTime().ToString('o')
        StartIndex = $StartIndex
        RequestedSamples = $SampleCount
        ActualSamples = $results.Count
        MaxRunSeconds = $MaxRunSeconds
        CandidateManifestPath = $CandidateManifestPath
        CandidatePoolCount = $available.Count
        RunTimeLimitReached = $runTimeLimitReached
        OcrNonEmpty = $ocrSuccess
        DictionaryFound = $lookupSuccess
        OcrRegressions = $regressions
        OcrRecoveries = $recoveries
        ReplayTimeouts = $replayTimeouts
        ReplayFailures = $replayFailures
        LabeledSamples = $labeled.Count
        OcrExact = $ocrExactCount
        Top1SenseLabeled = $senseLabeled.Count
        Top1Sense = $top1SenseCount
        ContextLabeled = $contextLabeled.Count
        ContextTop1Sense = $contextTop1SenseCount
        AbstentionLabeled = $abstentionLabeled.Count
        Abstained = $abstentionCount
        Diagnostics = @($results | Group-Object Diagnostic | ForEach-Object {
            [pscustomobject]@{ Name = $_.Name; Count = $_.Count }
        })
        InputQuality = @($results | Group-Object ContentClass | ForEach-Object {
            [pscustomobject]@{ Name = $_.Name; Count = $_.Count }
        })
        Results = @($results)
    }
    $summaryDirectory = Split-Path -Parent $SummaryPath
    if ($summaryDirectory) { New-Item -ItemType Directory -Force -Path $summaryDirectory | Out-Null }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 $SummaryPath
    "Summary JSON: $SummaryPath"
}
