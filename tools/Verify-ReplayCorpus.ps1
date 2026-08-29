param(
    [int]$SampleCount = 12,
    [int]$StartIndex = 0,
    [int]$DelayMilliseconds = 250,
    [int]$ReplaySettleMilliseconds = 1000,
    [switch]$Stratified
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $repo 'src\RealtimeTranslate.App\bin\Debug\net8.0-windows10.0.19041.0\RealtimeTranslate.App.exe'
$records = Join-Path $env:LOCALAPPDATA 'RealtimeTranslateCodex\records\lookups.jsonl'
$outputRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'RealtimeTranslateCodex-replay'
$outputRecords = Join-Path $outputRoot 'records\lookups.jsonl'

if (!(Test-Path $exe)) { throw "Built app not found: $exe" }
if (!(Test-Path $records)) { throw "Lookup archive not found: $records" }
if ($SampleCount -lt 1) { throw 'SampleCount must be positive.' }

New-Item -ItemType Directory -Force -Path (Split-Path $outputRecords) | Out-Null

$archive = @(Get-Content -Encoding utf8 $records | ForEach-Object {
    try { $_ | ConvertFrom-Json } catch { $null }
} | Where-Object { $_ -and $_.screenshotPath -and $_.selection })
$available = 0..($archive.Count - 1) | Where-Object { $_ -ge $StartIndex }
if ($Stratified) {
    # Deterministic evenly spaced picks cover the full archive without relying on random state.
    $indices = @(0..([Math]::Min($SampleCount, $available.Count) - 1) | ForEach-Object {
        $available[[Math]::Floor($_ * $available.Count / [double][Math]::Min($SampleCount, $available.Count))]
    } | Select-Object -Unique)
} else {
    $indices = @($available | Select-Object -First $SampleCount)
}
if (!$indices) { throw "No replayable records at or after index $StartIndex." }

$results = foreach ($index in $indices) {
    $before = if (Test-Path $outputRecords) { (Get-Content -Encoding utf8 $outputRecords).Count } else { 0 }
    $process = Start-Process -FilePath $exe -ArgumentList @('--replay', $records, '--index', $index, '--replay-delay-ms', $ReplaySettleMilliseconds) -Wait -PassThru
    Start-Sleep -Milliseconds $DelayMilliseconds
    $lines = if (Test-Path $outputRecords) { @(Get-Content -Encoding utf8 $outputRecords) } else { @() }
    $newRecord = if ($lines.Count -gt $before) { $lines[$before] | ConvertFrom-Json } else { $null }
    $sourceText = [string]$archive[$index].recognizedText
    $replayedText = if ($newRecord) { [string]$newRecord.recognizedText } else { '' }
    $sourceHasOcr = ![string]::IsNullOrWhiteSpace($sourceText)
    $replayHasOcr = ![string]::IsNullOrWhiteSpace($replayedText)
    [pscustomobject]@{
        Index = $index
        SourceWord = $sourceText
        ExitCode = $process.ExitCode
        RecognizedText = $replayedText
        FoundDefinition = if ($newRecord) { @($newRecord.definitions | Where-Object { $_ -and $_ -notmatch '未识别|未找到|失败' }).Count -gt 0 } else { $false }
        Diagnostic = if (!$newRecord) { 'no replay record' } elseif (!$sourceHasOcr -and !$replayHasOcr) { 'historical OCR empty' } elseif ($sourceHasOcr -and !$replayHasOcr) { 'OCR regression' } elseif (!$sourceHasOcr -and $replayHasOcr) { 'OCR recovery' } elseif (!$newRecord.definitions -or @($newRecord.definitions | Where-Object { $_ -and $_ -notmatch '未识别|未找到|失败' }).Count -eq 0) { 'no definition' } else { 'ok' }
    }
}

$results | Format-Table -AutoSize
$ocrSuccess = @($results | Where-Object { $_.RecognizedText }).Count
$lookupSuccess = @($results | Where-Object { $_.FoundDefinition }).Count
$regressions = @($results | Where-Object Diagnostic -eq 'OCR regression').Count
$recoveries = @($results | Where-Object Diagnostic -eq 'OCR recovery').Count
"Summary: samples=$($results.Count); ocr_nonempty=$ocrSuccess; dictionary_found=$lookupSuccess; ocr_regressions=$regressions; ocr_recoveries=$recoveries; replay_failures=$(@($results | Where-Object { $_.ExitCode -ne 0 }).Count)"
