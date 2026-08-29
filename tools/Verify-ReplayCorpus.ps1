param(
    [int]$SampleCount = 12,
    [int]$StartIndex = 0,
    [int]$DelayMilliseconds = 250
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
$indices = 0..($SampleCount - 1) | ForEach-Object { $StartIndex + $_ } | Where-Object { $_ -lt $archive.Count }
if (!$indices) { throw "No replayable records at or after index $StartIndex." }

$results = foreach ($index in $indices) {
    $before = if (Test-Path $outputRecords) { (Get-Content -Encoding utf8 $outputRecords).Count } else { 0 }
    $process = Start-Process -FilePath $exe -ArgumentList @('--replay', $records, '--index', $index) -Wait -PassThru
    Start-Sleep -Milliseconds $DelayMilliseconds
    $lines = if (Test-Path $outputRecords) { @(Get-Content -Encoding utf8 $outputRecords) } else { @() }
    $newRecord = if ($lines.Count -gt $before) { $lines[$before] | ConvertFrom-Json } else { $null }
    [pscustomobject]@{
        Index = $index
        SourceWord = [string]$archive[$index].recognizedText
        ExitCode = $process.ExitCode
        RecognizedText = if ($newRecord) { [string]$newRecord.recognizedText } else { '' }
        FoundDefinition = if ($newRecord) { @($newRecord.definitions | Where-Object { $_ -and $_ -notmatch '未识别|未找到|失败' }).Count -gt 0 } else { $false }
        Diagnostic = if (!$newRecord) { 'no replay record' } elseif ([string]::IsNullOrWhiteSpace($newRecord.recognizedText)) { 'OCR empty' } elseif (@($newRecord.definitions).Count -eq 0) { 'no definition' } else { 'ok' }
    }
}

$results | Format-Table -AutoSize
$ocrSuccess = @($results | Where-Object { $_.RecognizedText }).Count
$lookupSuccess = @($results | Where-Object { $_.FoundDefinition }).Count
"Summary: samples=$($results.Count); ocr_nonempty=$ocrSuccess; dictionary_found=$lookupSuccess; replay_failures=$(@($results | Where-Object { $_.ExitCode -ne 0 }).Count)"
