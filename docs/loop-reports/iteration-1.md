# Loop Iteration 1

- Started: 2026-09-01 13:23:27
- Commit before: 3cb0e56e4bd107222220449b4ebd5096267dbd02
- Commit after: d95c63a5f3ade39340e1fd5e43834f6ffb1bc76e
- Independent review: 
recorded in artifacts/agent-review-log.md

## Agent report/output

index 0000000000000000000000000000000000000000..337498335fb9cfe718816d412bb2f15d858d4099
--- /dev/null
+++ b/docs/loop-reports/iteration-1.md
@@ -0,0 +1,29 @@
+# Iteration 1
+
+## Hypothesis
+
+The fixed 180 px vertical context padding may crop out nearby Reverse: 1999 dialogue text. A modest increase to 240 px in both live and replay capture was expected to recover more sentence text without changing the general desktop selection mode.
+
+## Exact Change And Commit
+
+The experiment changed `ContextVerticalPaddingPixels` from 180 to 240 in `WindowsScreenshotService` and `ReplayScreenshotService`, then reverted it after verification. The final committed state keeps the original 180 px behavior because the measured result was a regression. Commits: `1940132` (report/status) and `0e26673` (independent-review correction).
+
+## Before/After Examples
+
+The deterministic archive subset was indices 200 (`parrot`), 500 (`frail`), 939 (`thrust`), 1123 (`bark`), and 1127 (`bark`). Before, the existing 180 px replay evidence (`artifacts/context-unmasked-full-after-20260901.json`) had no replay timeouts on the 20-sample corpus; indices 1123 and 1127 safely abstained/selected respectively. After the temporary 240 px change, the three dialogue samples still returned the same visible context and meanings, while both HUD-heavy bark samples timed out. No sentence-edge recovery was observed.
+
+## Metrics
+
+The prior 20-sample production replay was 20/20 exact OCR, 16 labelled context cases, 14/16 context Top-1, and 4/4 correct abstentions with 0 timeouts/failures. The 240 px focused replay was 5/5 exact OCR, 3/3 context-labelled and 3/3 context Top-1 among completed context cases, 2/2 correct abstentions, but had 2 replay timeouts and 2 replay failures (`artifacts/iteration-1-padding240.json`). The executable test suite remained 23/23, and the Debug build had 0 warnings and 0 errors; these do not establish OCR usefulness.
+
+## Regressions And Limits
+
+The larger crop increased OCR work enough to time out both `bark` HUD samples. It did not improve the observed `parrot`, `frail`, or `thrust` transcription. The experiment used five archived screenshots, not the required 50-screenshot two-agent gold corpus, and provides no new learning-usefulness evidence. The temporary code was reverted, so the shipped behavior has no net OCR regression.
+
+## Independent Review
+
+Decision: `insufficient-evidence`. The independent reviewer confirmed that 240 px caused two HUD replay timeouts and that the three completed dialogue samples showed no changed text. The reviewer also noted that the five-case result has no independently transcribed line-level gold labels or WER, and that the 5/5 OCR figure must not be read as end-to-end success because two samples failed. The reviewer found no basis for automatic acceptance.
+
+## Next Question
+
+Can a detected, optional dialogue-region enhancement choose a smaller text-bearing crop (using screenshot content signals) so that context OCR improves without expanding every general desktop crop or increasing HUD timeouts?

codex
Iteration 1 is complete and documented.

- Tested 240 px context padding against five real archived screenshots.
- Result: no dialogue fidelity improvement; two HUD OCR timeouts. Change reverted.
- Independent review: `insufficient-evidence`.
- Verification: 23/23 focused tests passed; build succeeded with 0 warnings/errors.
- Report: [iteration-1.md](/D:/Projects/Code/realtime-translate-codex/docs/loop-reports/iteration-1.md)
- Evidence: [iteration-1-padding240.json](/D:/Projects/Code/realtime-translate-codex/artifacts/iteration-1-padding240.json)
- Commits: `1940132`, `0e26673`, `337165c`, `d95c63a`

Automatic acceptance was not created because the criteria were not met.
diff --git a/docs/agentic-loop-status.md b/docs/agentic-loop-status.md
index 1e58b3fb9c0f8728fde9dfb12a7b29927d3ed565..1939f7c5ad198156ee139aab496cc3cfaf12fa4a
--- a/docs/agentic-loop-status.md
+++ b/docs/agentic-loop-status.md
@@ -1,11 +1,14 @@
 # Agentic Loop Status
 
-Updated: 2026-09-01 13:23:27
+Updated: 2026-09-01 14:05:00
 
-- Run: agent iteration 1
+- Run: agent iteration 1
 - Stage: improving usable context OCR and learning-card quality
 - Goal: reliable sentence understanding plus memorable word learning
 - Scope: general desktop OCR baseline plus optional game-dialogue enhancement
 - Independent review: required after each relevant committed change
 - Completion: automatic threshold or user acceptance
-- Next: inspect docs/loop-reports/iteration-1.md
+- Hypothesis: increasing context vertical padding from 180 px to 240 px would recover cropped dialogue lines.
+- Result: rejected; focused replay showed no dialogue gain and 2 HUD OCR timeouts, so the temporary code was reverted.
+- Independent review: insufficient-evidence; no automatic acceptance.
+- Next: test detected, optional dialogue-region cropping with bounded OCR cost.
diff --git a/docs/loop-reports/iteration-1.md b/docs/loop-reports/iteration-1.md
new file mode 100644
index 0000000000000000000000000000000000000000..337498335fb9cfe718816d412bb2f15d858d4099
--- /dev/null
+++ b/docs/loop-reports/iteration-1.md
@@ -0,0 +1,29 @@
+# Iteration 1
+
+## Hypothesis
+
+The fixed 180 px vertical context padding may crop out nearby Reverse: 1999 dialogue text. A modest increase to 240 px in both live and replay capture was expected to recover more sentence text without changing the general desktop selection mode.
+
+## Exact Change And Commit
+
+The experiment changed `ContextVerticalPaddingPixels` from 180 to 240 in `WindowsScreenshotService` and `ReplayScreenshotService`, then reverted it after verification. The final committed state keeps the original 180 px behavior because the measured result was a regression. Commits: `1940132` (report/status) and `0e26673` (independent-review correction).
+
+## Before/After Examples
+
+The deterministic archive subset was indices 200 (`parrot`), 500 (`frail`), 939 (`thrust`), 1123 (`bark`), and 1127 (`bark`). Before, the existing 180 px replay evidence (`artifacts/context-unmasked-full-after-20260901.json`) had no replay timeouts on the 20-sample corpus; indices 1123 and 1127 safely abstained/selected respectively. After the temporary 240 px change, the three dialogue samples still returned the same visible context and meanings, while both HUD-heavy bark samples timed out. No sentence-edge recovery was observed.
+
+## Metrics
+
+The prior 20-sample production replay was 20/20 exact OCR, 16 labelled context cases, 14/16 context Top-1, and 4/4 correct abstentions with 0 timeouts/failures. The 240 px focused replay was 5/5 exact OCR, 3/3 context-labelled and 3/3 context Top-1 among completed context cases, 2/2 correct abstentions, but had 2 replay timeouts and 2 replay failures (`artifacts/iteration-1-padding240.json`). The executable test suite remained 23/23, and the Debug build had 0 warnings and 0 errors; these do not establish OCR usefulness.
+
+## Regressions And Limits
+
+The larger crop increased OCR work enough to time out both `bark` HUD samples. It did not improve the observed `parrot`, `frail`, or `thrust` transcription. The experiment used five archived screenshots, not the required 50-screenshot two-agent gold corpus, and provides no new learning-usefulness evidence. The temporary code was reverted, so the shipped behavior has no net OCR regression.
+
+## Independent Review
+
+Decision: `insufficient-evidence`. The independent reviewer confirmed that 240 px caused two HUD replay timeouts and that the three completed dialogue samples showed no changed text. The reviewer also noted that the five-case result has no independently transcribed line-level gold labels or WER, and that the 5/5 OCR figure must not be read as end-to-end success because two samples failed. The reviewer found no basis for automatic acceptance.
+
+## Next Question
+
+Can a detected, optional dialogue-region enhancement choose a smaller text-bearing crop (using screenshot content signals) so that context OCR improves without expanding every general desktop crop or increasing HUD timeouts?

2026-09-01T05:33:52.776576Z  WARN codex_mcp::rmcp_client: failed to initialize MCP client during shutdown: MCP startup failed: Environment variable GITHUB_PAT_TOKEN for MCP server 'github' is not set
2026-09-01T05:33:52.776858Z  WARN codex_mcp::rmcp_client: failed to initialize MCP client during shutdown: MCP startup failed: Environment variable GITHUB_PAT_TOKEN for MCP server 'github' is not set
tokens used
917,754
Iteration 1 is complete and documented.

- Tested 240 px context padding against five real archived screenshots.
- Result: no dialogue fidelity improvement; two HUD OCR timeouts. Change reverted.
- Independent review: `insufficient-evidence`.
- Verification: 23/23 focused tests passed; build succeeded with 0 warnings/errors.
- Report: [iteration-1.md](/D:/Projects/Code/realtime-translate-codex/docs/loop-reports/iteration-1.md)
- Evidence: [iteration-1-padding240.json](/D:/Projects/Code/realtime-translate-codex/artifacts/iteration-1-padding240.json)
- Commits: `1940132`, `0e26673`, `337165c`, `d95c63a`

Automatic acceptance was not created because the criteria were not met.

