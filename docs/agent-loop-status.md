# Agent Loop Status

Updated: 2026-08-30 08:05:00

- Run: added a deterministic candidate manifest and wired replay sampling to use it.
- Stage: archive replay and real desktop right-drag are executable; the candidate pool now contains fourteen reviewed game-scene indices and excludes the known terminal/editor capture at index `100`.
- Evidence: `dotnet build RealtimeTranslateCodex.sln --no-restore` completed with 0 warnings and 0 errors; the executable suite passed all 19 checks. A real bounded replay using `-CandidateManifestPath tests/Data/replay-candidates.json -Stratified -SampleCount 5` completed indices `187,281,600,751,939` with `ocr_nonempty=5/5`, `dictionary_found=5/5`, `replay_failures=0`, `ocr_exact=5/5`, and `context_top1_sense=5/5`; evidence is in `artifacts/candidate-filter-20260830.json`.
- Fixes: `tools/Verify-ReplayCorpus.ps1` accepts an optional JSON candidate manifest (array or `{ "indices": [...] }`), validates indices, samples only that pool for stratified runs, and records pool metadata in summary JSON. No OCR or UI behavior changed.
- Remaining blocker: only fourteen candidate indices are curated and fifteen sense labels exist; broader game-scene coverage and abstention/false-positive measurement are still missing.
- Next: expand the candidate manifest from additional real game captures, then run a larger bounded replay and label ambiguous contexts without changing ranking heuristics prematurely.
