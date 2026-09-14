# 项目参考：Realtime Translate Codex

Windows 桌面工具：**框选屏幕上的英文 → OCR 识别 → 离线查词 → 弹出词卡浮窗**。查词完全离线；句子/段落解释走 AI API（可选）。

## 技术栈

| 用途 | 选型 |
|---|---|
| 框架 | .NET 8，WPF（`net8.0-windows10.0.19041.0`，混用 WinForms 只为托盘 NotifyIcon） |
| OCR | `Windows.Media.Ocr`（Windows 系统自带引擎，离线，需在系统设置装英文语言包）；对同一裁剪图做多版本预处理（缩放/对比度等）竞争，选最优识别结果 |
| 截屏与裁剪 | System.Drawing（GDI+） |
| 全局热键 | Win32 `RegisterHotKey` |
| 框选交互 | Win32 低级鼠标钩子（全局右键拖选） |
| 词典 | ECDICT（离线 TSV） |
| 词卡增强数据 | Open English WordNet、MorphyNet、MorphoLex（构建期脚本生成 TSV / JSONL.gz 随程序分发） |
| AI 解释 | OpenAI 兼容 API（可配置，非核心） |
| 数据记录 | 本地 JSONL（查询历史）+ PNG（截图存档） |
| 数据构建脚本 | PowerShell + Python |
| 测试 | 自写控制台测试运行器（无第三方测试框架） |

## 代码结构

```
src/RealtimeTranslate.Core     纯逻辑库：OCR 编排、词典、规范化/词形还原、词卡、历史记录、模型
src/RealtimeTranslate.App      WPF 应用：浮窗 UI、截屏、鼠标钩子、热键、托盘（含暂停开关）
tests/RealtimeTranslate.Tests  控制台测试运行器
tools/                         词典导入、词卡数据构建、回放评测脚本
```

## 核心流程

1. 用户右键拖框（或按热键）→ 低级鼠标钩子拿到屏幕矩形
2. 截屏并裁剪该矩形（额外生成带上下文 padding 的大图）
3. 裁剪图做多版本预处理 → Windows OCR 逐版本识别 → 竞争选出最佳词
4. 词形还原（lemma）→ 查 ECDICT → 结合上下文挑选义项 → 生成词卡
5. WPF 浮窗展示；本次查询写入 JSONL，截图存入本地目录

## 自动评测基建（重要，重建时值得保留的思路）

历史记录 JSONL 每条含：截图路径 + 框选矩形 + 当时识别结果。因此**无需模拟鼠标**即可全自动评测：把（截图， 矩形）喂回生产管线重放，对比输出。对应 `tools/Verify-ReplayCorpus.ps1` 和 Core 里的 ReplayScreenshotService。

存档位置：`%LOCALAPPDATA%\RealtimeTranslateCodex\records\lookups.jsonl`（1300+ 条真实人工框选）。

## 已知短板（新会话的改进靶点）

- OCR 偶发 l/I、O/0 混淆 → 词典查不到；可在查词前做混淆字符替换重试
- 罕见词/游戏造词词典未收录 → 需要合理降级（弃权而非硬答）
- "结合上下文选出精确义项"的正确率没有可靠度量 → 需要人工/盲评判卷
