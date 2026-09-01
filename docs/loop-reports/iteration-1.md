# Iteration 1

## Hypothesis

The fixed 180 px vertical context padding may crop out nearby Reverse: 1999 dialogue text. A modest increase to 240 px in both live and replay capture was expected to recover more sentence text without changing the general desktop selection mode.

## Exact Change And Commit

The experiment changed `ContextVerticalPaddingPixels` from 180 to 240 in `WindowsScreenshotService` and `ReplayScreenshotService`, then reverted it after verification. The final committed state keeps the original 180 px behavior because the measured result was a regression. This report and the status update are committed as the iteration record.

## Before/After Examples

The deterministic archive subset was indices 200 (`parrot`), 500 (`frail`), 939 (`thrust`), 1123 (`bark`), and 1127 (`bark`). Before, the existing 180 px replay evidence (`artifacts/context-unmasked-full-after-20260901.json`) had no replay timeouts on the 20-sample corpus; indices 1123 and 1127 safely abstained/selected respectively. After the temporary 240 px change, the three dialogue samples still returned the same visible context and meanings, while both HUD-heavy bark samples timed out. No sentence-edge recovery was observed.

## Metrics

The prior 20-sample production replay was 20/20 exact OCR, 16 labelled context cases, 14/16 context Top-1, and 4/4 correct abstentions with 0 timeouts/failures. The 240 px focused replay was 5/5 exact OCR, 3/3 context-labelled and 3/3 context Top-1 among completed context cases, 2/2 correct abstentions, but had 2 replay timeouts and 2 replay failures (`artifacts/iteration-1-padding240.json`). The executable test suite remained 23/23, and the Debug build had 0 warnings and 0 errors; these do not establish OCR usefulness.

## Regressions And Limits

The larger crop increased OCR work enough to time out both `bark` HUD samples. It did not improve the observed `parrot`, `frail`, or `thrust` transcription. The experiment used five archived screenshots, not the required 50-screenshot two-agent gold corpus, and provides no new learning-usefulness evidence. The temporary code was reverted, so the shipped behavior has no net OCR regression.

## Independent Review

`insufficient-evidence`: the experiment is a falsified crop-size hypothesis, not an accepted improvement. A reviewer must confirm that the timeout regression and unchanged dialogue text are correctly represented; no automatic acceptance is warranted.

## Next Question

Can a detected, optional dialogue-region enhancement choose a smaller text-bearing crop (using screenshot content signals) so that context OCR improves without expanding every general desktop crop or increasing HUD timeouts?
