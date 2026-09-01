# Iteration 2

## Hypothesis

The OCR selector was too conservative for context crops: requiring two additional usable tokens meant a scaled or binarized variant that recovered only one sentence token was discarded. Allowing a one-token upgrade only when the first result already has multiple usable tokens should recover dialogue edges while preserving tight word OCR stability.

## Exact Changes And Commit

`src/RealtimeTranslate.Core/Ocr/OcrVariantSelector.cs` now uses a required gain of 2 for one-token crops and 1 for multi-token crops. `tests/RealtimeTranslate.Tests/Program.cs` adds a marginal-context regression case proving the recovered-token path. The final coherent commit is recorded below after verification.

## Before/After Examples

On the comparable 20-sample archived game set, several context strings gained one recovered token. Index 1029 changed from a truncated context ending before the target to one including `the`; index 1028 gained the trailing `my`; other changed samples included 657, 700, 845, 939, 1030, and 1090. Some outputs remain visibly mangled, so these are examples of recovery, not proof of transcription quality. Tight word behavior remained unchanged in the `bark` selector test.

## Metrics

Before: exact word OCR 20/20, context available 16/16, context Top-1 10/16, replay timeouts/failures 0/0. After: exact word OCR 20/20, context available 16/16, context Top-1 11/16, replay timeouts/failures 0/0, and OCR regressions 0 (`artifacts/variant-selection-before-20260901.json`, `artifacts/variant-selection-after-20260901.json`, `artifacts/iteration-2-selector-after-20260901.json`). Focused executable tests passed 23/23 (including the new marginal-context assertions); build passed with 0 warnings and 0 errors.

## Regressions And Limits

The selector still chooses by usable-token count, so a multi-token general desktop crop could admit a noisier variant with one extra token. The synthetic test does not cover that adversarial case. The replay set has no independent line-level gold transcription or WER, is below the required 50 screenshots, and does not measure learning usefulness. The change is reversible in one selector rule and did not regress observed word OCR or replay stability.

## Independent Review

Decision: `insufficient-evidence`. The independent reviewer confirmed the 20/20 exact OCR, 0 timeouts/failures, and context Top-1 improvement from 10/16 to 11/16, but found no basis for acceptance without line-level fidelity labels, WER, context-gate precision/recall, and a general desktop noise corpus.

## Next Question

Can a 50-plus screenshot, two-reviewer line-level gold corpus show that the one-token context upgrade improves WER and dialogue-line recovery without admitting noisy variants in arbitrary desktop selections?
