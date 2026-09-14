param(
    [string]$SourceUrl = "https://raw.githubusercontent.com/skywind3000/ECDICT/master/ecdict.csv",
    [string]$OutputPath = "src/RealtimeTranslate.App/dictionary.tsv"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$root = Split-Path -Parent $PSScriptRoot
$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $root $OutputPath
}

$downloadPath = Join-Path ([System.IO.Path]::GetTempPath()) "ecdict.csv"
Invoke-WebRequest -Uri $SourceUrl -OutFile $downloadPath

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $resolvedOutput) | Out-Null
$writer = [System.IO.StreamWriter]::new($resolvedOutput, $false, [System.Text.UTF8Encoding]::new($false))

try {
    $writer.WriteLine("# Generated from ECDICT: https://github.com/skywind3000/ECDICT")
    $writer.WriteLine("# Format: word`tdefinition1; definition2")

    Import-Csv -Path $downloadPath | ForEach-Object {
        $word = ([string]$_.word).Trim().ToLowerInvariant()
        $translation = ([string]$_.translation).Trim()
        if ($word.Length -eq 0 -or $translation.Length -eq 0) {
            return
        }

        if ($word -notmatch "^[a-z][a-z0-9+'\.-]*$") {
            return
        }

        $definition = $translation `
            -replace "\\n", "; " `
            -replace "`r?`n", "; " `
            -replace "\s+", " "
        $definition = $definition.Trim(" ;")
        if ($definition.Length -eq 0) {
            return
        }

        $writer.WriteLine(("{0}`t{1}" -f $word, $definition))
    }
}
finally {
    $writer.Dispose()
}

Get-Item $resolvedOutput | Select-Object FullName, Length
