# Agent Loop Status

Updated: 2026-08-30 07:18:00

- Run: added the human-verified ambiguous noun/verb sample `dome` from the stratified archive replay.
- Stage: archive replay and real desktop right-drag are executable; the manifest now covers fifteen labeled records across nouns, verbs, adjectives, and game/UI text.
- Evidence: `dotnet build RealtimeTranslateCodex.sln --no-restore` completed with 0 warnings and 0 errors, and the executable suite passed all 19 checks. A real replay across indices `100,200,500,600,700,187,281,563,657,751,799,845,939,1090,1127` completed with no timeout/process failure: `ocr_exact=15/15`, `dictionary_found=15/15`, `context_available=15/15`, and `context_top1_sense=15/15` (100%). Full per-record evidence is archived in `artifacts/labeled-context-20260830-r6.json`.
- Fixes: `tests/Data/replay-expected.json` now records `dome` (expected noun sense `圆顶`). The stratified replay also found `shelter` without recoverable context, so it remains unlabeled. No OCR or UI behavior was changed.
- Remaining blocker: the fifteen-item set passes the 85% context target, but it is still too small for broad scene coverage; most historical screenshots have noisy or absent context, so ranking changes would risk overfitting.
- Next: continue stratified replay over uncovered archive ranges, label only samples with unambiguous human-reviewed context, and track false-positive/abstention rates before changing ranking heuristics.
