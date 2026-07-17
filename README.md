# Realtime Translate Codex

[![CI](https://github.com/WhoAlwaysZzz/realtime-translate-codex/actions/workflows/ci.yml/badge.svg)](https://github.com/WhoAlwaysZzz/realtime-translate-codex/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Windows word lookup and text explanation overlay for visual novels, games, web pages, chat windows, and any other visible English text. Word lookup is offline; sentence explanation uses a configured AI API.

## Features

- Right-drag or hotkey-driven OCR selection from any visible desktop window.
- Offline English-to-Chinese dictionary lookup with normalization and basic lemmatization.
- Offline word-part breakdowns, related word families, and compact usage-context hints when reliable local data is available.
- Compact stacked overlays, recent lookup replay, query counts, and manual keyboard lookup.
- Optional AI sentence and short-paragraph explanation through an OpenAI-compatible API.
- Local JSONL records and screenshots for lookup history.

## Requirements

- Windows 10 or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for source builds
- Windows English OCR language support

## Build

```powershell
git clone https://github.com/WhoAlwaysZzz/realtime-translate-codex.git
cd realtime-translate-codex
dotnet restore .\RealtimeTranslateCodex.sln
dotnet build .\RealtimeTranslateCodex.sln
dotnet run --project .\src\RealtimeTranslate.App\RealtimeTranslate.App.csproj
```

## Usage

1. Run `RealtimeTranslate.App`.
2. Keep games in windowed or borderless-windowed mode. Exclusive fullscreen is not guaranteed in V1.
3. Hold the right mouse button and drag a rectangle tightly around one English word.
4. Release the right mouse button to look it up.
5. A small translation overlay appears near the top of the screen. Click an overlay to close it.

When video subtitles have already moved on, press the manual lookup hotkey to open a small input popup without a screen mask. Type a word, press `Enter` to look it up, or press `Esc` to close the popup.

If a translation overlay disappeared before you read it, press the recent lookups hotkey to show the latest three looked-up words again.

For complex sentences or short paragraphs, press the explanation hotkey, drag a rectangle around the text, and wait for the AI explanation panel. The panel shows OCR text, Chinese translation, key notes, and a copy button.

The app stays in the tray. Right-click the tray icon to configure hotkeys, choose the selection mode, toggle screenshot saving, pause or resume lookup input, open the archive folder, or exit. Screenshot saving is enabled at every startup and applies to both word lookup and text explanation selections. Disabling it keeps OCR active without writing PNG files. Recent lookup replay never captures a screenshot. While paused, right-drag selection and all global lookup hotkeys are disabled until you choose `Resume` from the tray menu.

Selection modes:

- `One-shot selection`: pressing the hotkey opens one selection box, then exits selection mode after one lookup.
- `Continuous selection`: pressing the hotkey toggles selection mode on. After each lookup, the next selection box opens automatically. Press the hotkey again or press `Esc` in the selection box to stop.

The default selection hotkey is `Ctrl+Shift+X`, the default manual lookup hotkey is `F4`, the default recent lookups hotkey is `F5`, and the default explanation hotkey is `F6`. They can be changed from the tray menu. Settings are saved to `%LOCALAPPDATA%\RealtimeTranslateCodex\settings.json`.

The app is not game-specific. If you select English words in a browser, Codex, a video page, or another normal desktop window, OCR should still run. If every selection says no word was recognized, first check whether Windows has English OCR language support installed, then check whether the selected text is visible in the saved screenshot.

## Local Data

Lookup records are stored under:

`%LOCALAPPDATA%\RealtimeTranslateCodex`

The archive contains:

- `screenshots/`: full virtual-screen screenshots captured at lookup time.
- `records/lookups.jsonl`: one JSON object per lookup, including OCR candidates, normalized word, lemma, definitions, selection rectangle, and screenshot path.
- `records/recent-lookups.json`: recent overlay entries used by the replay hotkey.
- `records/explanations.jsonl`: one JSON object per text explanation, including source text, translation, notes, selection rectangle, screenshot path, provider/model, and any error message.

## AI Explanation

Sentence explanation uses an OpenAI-compatible chat completions API. The default generated config targets DeepSeek:

```json
{
  "provider": "DeepSeek",
  "baseUrl": "https://api.deepseek.com",
  "model": "deepseek-v4-flash",
  "apiKey": ""
}
```

On first launch, the app creates:

`%LOCALAPPDATA%\RealtimeTranslateCodex\ai-settings.json`

Set the API key in either place:

- Environment variable: `DEEPSEEK_API_KEY`
- Local config field: `apiKey`

The environment variable takes precedence. The config file is local and is not meant to be committed. Text selected for explanation is sent to the configured API provider.

Never commit an API key. Prefer the `DEEPSEEK_API_KEY` environment variable or keep the key only in the generated local settings file.

## Dictionary

The app ships with a generated ECDICT-based English-Chinese dictionary at `src/RealtimeTranslate.App/dictionary.tsv`, then loads an optional user override dictionary at:

`%LOCALAPPDATA%\RealtimeTranslateCodex\dictionary.tsv`

Dictionary rows use:

```text
word	definition1; definition2
```

Load order is: small embedded fallback dictionary, bundled ECDICT dictionary, then the user override dictionary. Later entries override earlier entries with the same word.

Vocabulary cards use a structured local lexicon generated from ECDICT and Open English WordNet evidence. The runtime keeps Chinese short senses, parts of speech, English definitions/examples, and BNC/FRQ frequency bands. A second build step fuses precision-filtered MorphyNet relations with selected MorphoLex analyses. Only reviewed modern derivations, useful compounds, and inflectional forms are displayed; uncertain analyses are omitted and replaced by a short WordNet usage/context hint.

Word lookup captures two in-memory crops from one screenshot: a tight word crop and a wider line-context crop. The line context ranks senses locally. A sense is promoted only when its evidence clearly wins; otherwise the card displays compact numbered senses instead of guessing.

The deterministic 200-word advanced-vocabulary audit is stored in `tests/Data/rendered-card-audit.tsv`. Tests enforce 100% translations, visible frequency bands, at least 90% family/form coverage, at least 35% reviewed morphology coverage, and at least 95% visible morphology-or-usage coverage. Regression cases also reject known false relationships.

To regenerate the bundled dictionary from ECDICT:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Import-Ecdict.ps1
```

To regenerate the structured lexicon and enriched cards after downloading the source datasets:

```powershell
python .\tools\Build-Structured-Lexicon.py --ecdict "$env:TEMP\ecdict.csv" --wordnet-dir "$env:TEMP\english-wordnet-2025-json" --output .\src\RealtimeTranslate.App\Data\lexicon.jsonl.gz
python .\tools\Build-WordCards.py --ecdict "$env:TEMP\ecdict.csv" --derivational "$env:TEMP\morphynet-eng\eng.derivational.v1.tsv" --morpholex "$env:TEMP\MorphoLEX_en.xlsx" --contexts .\src\RealtimeTranslate.App\Data\word-insights.tsv --output .\src\RealtimeTranslate.App\Data\word-cards.tsv --audit-sample .\tests\Data\advanced-word-audit-sample.txt
dotnet run --project .\tests\RealtimeTranslate.Tests\RealtimeTranslate.Tests.csproj -- --export-card-audit .\tests\Data\rendered-card-audit.tsv
```

See `THIRD_PARTY_NOTICES.md` for ECDICT attribution and license details.

## Privacy

Word OCR and dictionary lookup run locally. The application stores screenshots and lookup records under `%LOCALAPPDATA%\RealtimeTranslateCodex`; these may contain private on-screen content. Sentence explanation sends only the selected OCR text to the API provider configured by the user. See [SECURITY.md](SECURITY.md) for reporting security issues.

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request and follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## License

Released under the [MIT License](LICENSE). The bundled dictionary has separate attribution described in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
