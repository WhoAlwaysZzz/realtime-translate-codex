# Sentence Explanation Feature

Status: active feature branch.

## Goal

Add a separate selection flow for complex sentences or short paragraphs. The user presses a dedicated hotkey, selects visible English text, and receives a larger explanation panel with the recognized text, Chinese translation, and key notes.

## Non-Goals

- Do not merge paragraph explanation into the existing word lookup path.
- Do not make AI required for normal word lookup, manual lookup, or recent lookup replay.
- Do not store API keys in git-tracked files.
- Do not attempt a local model runtime in this iteration.

## Shape

The feature reuses selection, screenshot capture, OCR, tray, and hotkey infrastructure. It adds a separate text-oriented flow:

`selection -> screenshot -> OCR text lines -> AI explanation service -> explanation panel`

The explanation flow should have its own service interfaces and presentation model so future offline, API, or local-model backends can be swapped without disturbing word lookup.

## API Configuration

V1 uses an OpenAI-compatible chat completions endpoint, with DeepSeek as the expected provider.

Runtime configuration lives under:

`%LOCALAPPDATA%\RealtimeTranslateCodex\ai-settings.json`

The API key can also come from `DEEPSEEK_API_KEY`, which takes precedence over a missing config value. The config file is local and user-controlled; it is not checked into git.

Recommended default provider values:

- `baseUrl`: `https://api.deepseek.com`
- `model`: `deepseek-v4-flash`

## UX

- Default hotkey: `F6`.
- Tray entry: explain selected text.
- Tray entry: configure explanation hotkey.
- If AI config is missing or the network/API fails, show a non-crashing explanation error panel.
- The explanation panel should be larger and longer-lived than word lookup popups, support scrolling, and include a copy button.

## Archival

Keep the existing word lookup JSONL unchanged. Add a separate `records/explanations.jsonl` file for text explanation events. Each record should include timestamp, source text, translation, notes, selection rectangle, screenshot path, and error text when applicable.
