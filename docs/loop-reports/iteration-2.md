# Loop Iteration 2

- Started: 2026-09-01 13:36:47
- Commit before: d95c63a5f3ade39340e1fd5e43834f6ffb1bc76e
- Commit after: 3ace692b3e5e4f8885e99e0e46c580dea471ec95
- Independent review: 
recorded in artifacts/agent-review-log.md

## Agent report/output


The OCR variant selector now permits a one-token improvement for multi-token context crops while retaining the stricter tight-word rule. Replay results: context Top-1 improved `10/16 -> 11/16`, exact word OCR remained `20/20`, with zero timeouts or regressions. Tests passed `23/23`; build passed with zero warnings/errors.

Report: [iteration-2.md](/D:/Projects/Code/realtime-translate-codex/docs/loop-reports/iteration-2.md)  
Status: [agentic-loop-status.md](/D:/Projects/Code/realtime-translate-codex/docs/agentic-loop-status.md)

Independent review: `insufficient-evidence`; automatic acceptance was not created.


diff --git a/docs/agentic-loop-status.md b/docs/agentic-loop-status.md
index b64f2021f6a41ccdad8b2ddbe4e5a0e4315034d4..0f27770139f1960dbba70e278bcf32e397543754
--- a/docs/agentic-loop-status.md
+++ b/docs/agentic-loop-status.md
@@ -1,11 +1,14 @@
 # Agentic Loop Status
 
-Updated: 2026-09-01 13:36:47
+Updated: 2026-09-01 14:12:00
 
 - Run: agent iteration 2
 - Stage: improving usable context OCR and learning-card quality
 - Goal: reliable sentence understanding plus memorable word learning
 - Scope: general desktop OCR baseline plus optional game-dialogue enhancement
-- Independent review: required after each relevant committed change
-- Completion: automatic threshold or user acceptance
-- Next: inspect docs/loop-reports/iteration-2.md
+- Independent review: required after each relevant committed change
+- Completion: automatic threshold or user acceptance
+- Hypothesis: allow a one-token OCR variant upgrade for multi-token context crops while preserving the stricter tight-word rule.
+- Result: plausible improvement; comparable replay context Top-1 rose 10/16 to 11/16 with exact word OCR 20/20 and 0 timeouts/failures.
+- Independent review: insufficient-evidence; no automatic acceptance.
+- Next: build a line-level gold corpus and general desktop noise set, then measure WER and context-gate precision/recall.
diff --git a/docs/loop-reports/iteration-2.md b/docs/loop-reports/iteration-2.md
new file mode 100644
index 0000000000000000000000000000000000000000..3112c3536372b6c6002d25819f611e5575075018
--- /dev/null
+++ b/docs/loop-reports/iteration-2.md
@@ -0,0 +1,29 @@
+# Iteration 2
+
+## Hypothesis
+
+The OCR selector was too conservative for context crops: requiring two additional usable tokens meant a scaled or binarized variant that recovered only one sentence token was discarded. Allowing a one-token upgrade only when the first result already has multiple usable tokens should recover dialogue edges while preserving tight word OCR stability.
+
+## Exact Changes And Commit
+
+`src/RealtimeTranslate.Core/Ocr/OcrVariantSelector.cs` now uses a required gain of 2 for one-token crops and 1 for multi-token crops. `tests/RealtimeTranslate.Tests/Program.cs` adds a marginal-context regression case proving the recovered-token path. The final coherent commit is recorded below after verification.
+
+## Before/After Examples
+
+On the comparable 20-sample archived game set, several context strings gained one recovered token. Index 1029 changed from a truncated context ending before the target to one including `the`; index 1028 gained the trailing `my`; other changed samples included 657, 700, 845, 939, 1030, and 1090. Some outputs remain visibly mangled, so these are examples of recovery, not proof of transcription quality. Tight word behavior remained unchanged in the `bark` selector test.
+
+## Metrics
+
+Before: exact word OCR 20/20, context available 16/16, context Top-1 10/16, replay timeouts/failures 0/0. After: exact word OCR 20/20, context available 16/16, context Top-1 11/16, replay timeouts/failures 0/0, and OCR regressions 0 (`artifacts/variant-selection-before-20260901.json`, `artifacts/variant-selection-after-20260901.json`, `artifacts/iteration-2-selector-after-20260901.json`). Focused executable tests passed 23/23 (including the new marginal-context assertions); build passed with 0 warnings and 0 errors.
+
+## Regressions And Limits
+
+The selector still chooses by usable-token count, so a multi-token general desktop crop could admit a noisier variant with one extra token. The synthetic test does not cover that adversarial case. The replay set has no independent line-level gold transcription or WER, is below the required 50 screenshots, and does not measure learning usefulness. The change is reversible in one selector rule and did not regress observed word OCR or replay stability.
+
+## Independent Review
+
+Decision: `insufficient-evidence`. The independent reviewer confirmed the 20/20 exact OCR, 0 timeouts/failures, and context Top-1 improvement from 10/16 to 11/16, but found no basis for acceptance without line-level fidelity labels, WER, context-gate precision/recall, and a general desktop noise corpus.
+
+## Next Question
+
+Can a 50-plus screenshot, two-reviewer line-level gold corpus show that the one-token context upgrade improves WER and dialogue-line recovery without admitting noisy variants in arbitrary desktop selections?
diff --git a/src/RealtimeTranslate.Core/Ocr/OcrVariantSelector.cs b/src/RealtimeTranslate.Core/Ocr/OcrVariantSelector.cs
index 853ba4156a54de88e0f4cb7765002e874d68fa3f..549f7a04df7da2062fc401063d68ed09a23da6d9
--- a/src/RealtimeTranslate.Core/Ocr/OcrVariantSelector.cs
+++ b/src/RealtimeTranslate.Core/Ocr/OcrVariantSelector.cs
@@ -26,9 +26,11 @@
 
         // A tight word crop normally has one meaningful token in every
         // variant. Do not replace that known-good first result on a marginal
-        // score difference. Context OCR needs a clear extra sentence token
-        // before another preprocessing result can take over.
-        return best.Score >= firstScore + 2 ? best.Candidates : first;
+        // score difference. For multi-token crops, a one-token gain is useful
+        // evidence that a scaled/preprocessed variant recovered a sentence
+        // edge, so allow that upgrade without changing single-word behavior.
+        var requiredGain = firstScore <= 1 ? 2 : 1;
+        return best.Score >= firstScore + requiredGain ? best.Candidates : first;
     }
 
     private static int UsableTokenCount(IReadOnlyList<OcrCandidate> candidates) => candidates.Count(candidate =>
diff --git a/tests/RealtimeTranslate.Tests/Program.cs b/tests/RealtimeTranslate.Tests/Program.cs
index 1212a87607657c6401ac92bdefeb5b2e1d968c47..0b56e3acfecc2ecb304d43a22981367ffd1225d8
--- a/tests/RealtimeTranslate.Tests/Program.cs
+++ b/tests/RealtimeTranslate.Tests/Program.cs
@@ -292,6 +292,13 @@
     ]);
     AssertEqual(5, context.Count);
     AssertEqual("bark", context[^1].Text);
+
+    var marginalContext = OcrVariantSelector.Select([
+        [new OcrCandidate("babbling", 1), new OcrCandidate("like", 1), new OcrCandidate("a", 1)],
+        [new OcrCandidate("babbling", 1), new OcrCandidate("like", 1), new OcrCandidate("a", 1), new OcrCandidate("parrot", 1)],
+    ]);
+    AssertEqual(4, marginalContext.Count);
+    AssertEqual("parrot", marginalContext[^1].Text);
 }
 
 static void WordCardIndexLoadsExactFields()

2026-09-01T05:45:03.018259Z  WARN codex_mcp::rmcp_client: failed to initialize MCP client during shutdown: MCP startup failed: Environment variable GITHUB_PAT_TOKEN for MCP server 'github' is not set
2026-09-01T05:45:03.275394Z  WARN codex_mcp::rmcp_client: failed to initialize MCP client during shutdown: MCP startup failed: Environment variable GITHUB_PAT_TOKEN for MCP server 'github' is not set
Implemented and committed iteration 2 as `3ace692`.

The OCR variant selector now permits a one-token improvement for multi-token context crops while retaining the stricter tight-word rule. Replay results: context Top-1 improved `10/16 -> 11/16`, exact word OCR remained `20/20`, with zero timeouts or regressions. Tests passed `23/23`; build passed with zero warnings/errors.

Report: [iteration-2.md](/D:/Projects/Code/realtime-translate-codex/docs/loop-reports/iteration-2.md)  
Status: [agentic-loop-status.md](/D:/Projects/Code/realtime-translate-codex/docs/agentic-loop-status.md)

Independent review: `insufficient-evidence`; automatic acceptance was not created.

tokens used
310,210


