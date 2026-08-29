# Agent Loop Status

Updated: 2026-08-30 08:12:00

- Run: expanded the deterministic candidate manifest with four newly captured game-dialogue scenes (archive indices `1027`–`1030`).
- Stage: archive replay and real desktop right-drag are executable; the candidate pool now contains eighteen reviewed game-scene indices and excludes the known terminal/editor capture at index `100`.
- Evidence: a real bounded replay using `-CandidateManifestPath tests/Data/replay-candidates.json -Stratified -SampleCount 18 -MaxRunSeconds 180` completed all 18 candidates with `ocr_nonempty=18/18`, `dictionary_found=18/18`, `replay_failures=0`, and no timeouts; the 14 samples with existing expected labels were `ocr_exact=14/14` and `context_top1_sense=14/14`. Evidence is in `artifacts/candidate-expanded-20260830.json`. The new screenshots for `1027`–`1030` are 2560x1600 in-bounds dialogue selections (crack, burden, limp, attendant).
- Fixes: `tests/Data/replay-candidates.json` now includes indices `1027`–`1030`; no OCR, ranking, or UI behavior changed.
- Remaining blocker: the four newly covered game-scene samples have no human-reviewed expected sense labels, so abstention/false-positive behavior and ambiguity remain unmeasured on this expanded pool.
- Next: review and label the four new contexts (including an explicit ambiguous/abstain outcome where appropriate), then rerun the expanded bounded replay and report labeled accuracy plus abstention counts.
