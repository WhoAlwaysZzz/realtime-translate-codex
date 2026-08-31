# Agentic Loop Status

Updated: 2026-09-01 00:49:14

- Run: agent iteration 7
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis (iteration 7): WordNet usage examples must be gated by the same
  selected-sense boundary as the first-screen meaning. An abstaining context
  ranker must not expose examples from every dictionary sense as a 90% learning
  claim.
- Change: `WordNetExampleUsageHintService` now returns no hint when
  `SenseRankingResult.SelectedSense` is null; target-bearing and inflected
  examples remain available for a selected sense. Added a regression fixture for
  ambiguous `bark` plus existing `dwindle`/false-prefix cases.
- Metric delta (200-word rendered-card audit): ambiguous cards with a
  `用法/语境` learning line 33 → 0 (−33; 100% removal of unsafe mixed-sense
  hints). Source-bearing learning lines 113 → 80 (−33), an intentional
  fail-closed tradeoff. OCR replay evidence is unchanged: 20/20 exact OCR,
  15/20 context Top-1 on the labeled 20-sample batch; this iteration did not
  claim an OCR gain.
- Real examples: `adolescent` and `attendant` previously showed an abstaining
  first screen plus WordNet examples that implicitly selected one POS; they now
  show only the explicit candidate meanings. `bark` with no trusted context
  abstains without a learning usage line, while `bark` in “The dog began to bark”
  keeps the selected verb sense and contextual gloss.
- Regression / limitation: 33 ambiguous cards lose a potentially useful usage
  example until sense selection or a separately labeled multi-sense study
  insight exists. No OCR, crop, or latency path was changed. Independent
  read-only review is still required; current review artifacts are forms without
  completed verdicts, so automatic completion criteria are not met.
- Verification: `dotnet build RealtimeTranslateCodex.sln -c Release --no-restore`
  (0 warnings, 0 errors); focused test runner passes all 23 checks; before/after
  audit artifacts are `artifacts/card-audit-before-sense-bound-learning-20260901.tsv`
  and `artifacts/card-audit-sense-bound-learning-20260901.tsv`.
- Next question: can we add a separately sourced, explicitly multi-sense
  learning insight (with per-sense labels and evidence) without reintroducing
  ambiguity into the first screen?
