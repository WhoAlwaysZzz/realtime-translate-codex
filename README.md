# Realtime Translate Codex

Windows offline word lookup overlay for visual novels, games, web pages, chat windows, and any other visible English text.

## Usage

1. Run `RealtimeTranslate.App`.
2. Keep games in windowed or borderless-windowed mode. Exclusive fullscreen is not guaranteed in V1.
3. Press `Ctrl+Shift+X`.
4. Drag a rectangle tightly around one English word.
5. A small translation overlay appears near the top of the screen. Click an overlay to close it.

The app stays in the tray. Right-click the tray icon to select a word, open the archive folder, or exit.

The app is not game-specific. If you select English words in a browser, Codex, a video page, or another normal desktop window, OCR should still run. If every selection says no word was recognized, first check whether Windows has English OCR language support installed, then check whether the selected text is visible in the saved screenshot.

## Local Data

Lookup records are stored under:

`%LOCALAPPDATA%\RealtimeTranslateCodex`

The archive contains:

- `screenshots/`: full virtual-screen screenshots captured at lookup time.
- `records/lookups.jsonl`: one JSON object per lookup, including OCR candidates, normalized word, lemma, definitions, selection rectangle, and screenshot path.

## Dictionary

The app ships with a generated ECDICT-based English-Chinese dictionary at `src/RealtimeTranslate.App/dictionary.tsv`, then loads an optional user override dictionary at:

`%LOCALAPPDATA%\RealtimeTranslateCodex\dictionary.tsv`

Dictionary rows use:

```text
word	definition1; definition2
```

Load order is: small embedded fallback dictionary, bundled ECDICT dictionary, then the user override dictionary. Later entries override earlier entries with the same word.

To regenerate the bundled dictionary from ECDICT:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Import-Ecdict.ps1
```

See `THIRD_PARTY_NOTICES.md` for ECDICT attribution and license details.
