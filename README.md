# Realtime Translate Codex

Windows offline word lookup overlay for visual novels, games, web pages, chat windows, and any other visible English text.

## Usage

1. Run `RealtimeTranslate.App`.
2. Keep games in windowed or borderless-windowed mode. Exclusive fullscreen is not guaranteed in V1.
3. Hold the right mouse button and drag a rectangle tightly around one English word.
4. Release the right mouse button to look it up.
5. A small translation overlay appears near the top of the screen. Click an overlay to close it.

When video subtitles have already moved on, press the manual lookup hotkey to open a small input popup without a screen mask. Type a word, press `Enter` to look it up, or press `Esc` to close the popup.

If a translation overlay disappeared before you read it, press the recent lookups hotkey to show the latest three looked-up words again.

The app stays in the tray. Right-click the tray icon to configure the selection hotkey, configure the manual lookup hotkey, configure the recent lookups hotkey, choose the selection mode, select a word, manually type a word, replay recent words, open the archive folder, or exit.

Selection modes:

- `One-shot selection`: pressing the hotkey opens one selection box, then exits selection mode after one lookup.
- `Continuous selection`: pressing the hotkey toggles selection mode on. After each lookup, the next selection box opens automatically. Press the hotkey again or press `Esc` in the selection box to stop.

The default selection hotkey is `Ctrl+Shift+X`, the default manual lookup hotkey is `F4`, and the default recent lookups hotkey is `F5`. They can be changed from the tray menu. Settings are saved to `%LOCALAPPDATA%\RealtimeTranslateCodex\settings.json`.

The app is not game-specific. If you select English words in a browser, Codex, a video page, or another normal desktop window, OCR should still run. If every selection says no word was recognized, first check whether Windows has English OCR language support installed, then check whether the selected text is visible in the saved screenshot.

## Local Data

Lookup records are stored under:

`%LOCALAPPDATA%\RealtimeTranslateCodex`

The archive contains:

- `screenshots/`: full virtual-screen screenshots captured at lookup time.
- `records/lookups.jsonl`: one JSON object per lookup, including OCR candidates, normalized word, lemma, definitions, selection rectangle, and screenshot path.
- `records/recent-lookups.json`: recent overlay entries used by the replay hotkey.

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
