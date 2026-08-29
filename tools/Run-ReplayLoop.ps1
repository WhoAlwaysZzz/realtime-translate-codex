param(
    [int]$Iterations = 10,
    [int]$PauseSeconds = 2
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $repo 'src\RealtimeTranslate.App\bin\Debug\net8.0-windows10.0.19041.0\RealtimeTranslate.App.exe'
$records = Join-Path $env:LOCALAPPDATA 'RealtimeTranslateCodex\records\lookups.jsonl'
$status = Join-Path $repo 'docs\agent-loop-status.md'

for ($i = 1; $i -le $Iterations; $i++) {
    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    "# Agent Loop Status`r`n`r`n更新时间：$stamp`r`n`r`n## 当前状态`r`n`r`n- **运行状态**：第 $i/$Iterations 轮回放中`r`n- **阶段**：历史截图 → OCR → 查词 → 卡片`r`n- **记录源**：$records`r`n- **下一动作**：等待本轮回放结束并启动下一轮`r`n" | Set-Content -Encoding utf8 $status
    Start-Process -FilePath $exe -ArgumentList @('--replay', $records) -Wait
    Start-Sleep -Seconds $PauseSeconds
}

$stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
"# Agent Loop Status`r`n`r`n更新时间：$stamp`r`n`r`n## 当前状态`r`n`r`n- **运行状态**：已完成 $Iterations 轮`r`n- **阶段**：等待结果审查`r`n- **下一动作**：读取回放输出并分类问题`r`n" | Set-Content -Encoding utf8 $status
