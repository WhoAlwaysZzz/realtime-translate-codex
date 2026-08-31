# Agentic Loop Status

Updated: 2026-09-01 02:11:00

- Run: agent iteration 11
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: OCR may faithfully retain the selected inflected surface form
  while dictionary lookup uses its lemma. Passing only the lemma to the
  context ranker then makes a trustworthy sentence fail the target-anchor
  check. The ranker should accept only exact OCR tokens from the selected
  normalizer candidates, never topical context alone.
- Change: `AppController` now passes the selected `NormalizedWord` to
  `ISenseRanker`. `LocalContextSenseRanker` matches its explicit candidates
  (for example, `barked` and `bark`) for the anchor, POS scoring, and the
  adjective-comma frame. The existing string overload remains for lookup
  paths without an OCR selection.
- Metric delta: the preserved 20-screen real-game replay is unchanged before
  and after: OCR exact 20/20, contextual Top-1 10/16 (62.5%), expected
  abstention 4/4, replay failures 0. The six anchor safety cases also remain
  6/6 OCR exact, 2/5 contextual Top-1, and 1/1 expected abstention. A focused
  form-aware ranking regression now proves `barked` anchors the verbal
  `bark` sense while a sentence without either `barked` or `bark` abstains.
- Real examples: archived game index 281 retains `golems` -> `golem` and its
  reviewed `机器人` reading; index 1127 retains the anchored `bark` -> `吠叫`;
  index 939 (`thrust`) still abstains when OCR has split-token debris; index
  563 (`staunch`) still abstains because its OCR crop has no target anchor.
- Outputs: before `artifacts/context-anchor-inflection-before-20260901.json`;
  after `artifacts/context-anchor-inflection-full-after-20260901.json`.
- Regressions and limits: no replay regression was observed, but the current
  archived set does not exercise a case where this handoff changes the card;
  the new direct regression is therefore necessary but not evidence of a
  corpus improvement. These 20 historical selections are not the required
  50 dual-transcribed screenshot corpus, so no sentence-line recovery, WER,
  trust-gate precision/recall, latency, learning-layer quality, or independent
  reviewer acceptance is established.
- Verification: Debug build succeeded (four transient MSB3026 retries while a
  previous replay child still held the test DLL); focused test suite passed
  22/22; full production replay completed in 149 seconds with 0 failures.
- Next question: can a dual-transcribed screenshot corpus reveal whether
  target-form-aware anchoring recovers valid context coverage, without
  admitting any unanchored topical-sense selection?
