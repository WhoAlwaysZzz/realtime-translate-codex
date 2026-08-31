# Agentic Loop Status

Updated: 2026-08-31 23:58:00

- Run: agent iteration 1
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: learning metadata was presented as unexplained prose; attaching evidence source and confidence will reduce unsupported learning claims without displacing the sentence gloss.
- Change: `WordInsight` now carries `EvidenceSource`/`Confidence`; legacy four-column MorphoLex rows receive the licensed MorphoLex (2018) source and 0.75 confidence when non-empty. WordNet fallback usage is labelled separately at 90% confidence. Cards render a distinct `证据` line, after sentence meaning and alignment.
- Metric delta: baseline evidence-labelled learning blocks 0/20 in `blind-card-review-explicit-uncertainty-clean-20260831.json`; after this change, every non-empty insight is source-bearing by construction, and fallback usage is source-bearing. Automated suite: 19/19 checks passed (including new evidence presentation check). OCR replay metrics unchanged (20/20 exact; 15/16 context Top-1 on labelled samples).
- Real examples: `bark` with “The dog began to bark” remains a verb gloss and now gets explicit evidence metadata; the HUD-only `bark` sample remains abstention-safe and does not gain a guessed sense. `exquisite` keeps “精致的” on the first screen while its study metadata is separately attributed.
- Regressions/limits: no build or existing test regressions; the 20-card blind-review files still contain no completed human decisions, so learning-layer judge pass rate and source factuality are not yet independently established. MorphoLex-derived confidence is a conservative default for legacy rows, not a per-entry audit.
- Next question: run a fresh blind card review on the evidence-labelled output and audit whether the 0.75 default should be lowered or replaced with per-entry reviewed confidence.
