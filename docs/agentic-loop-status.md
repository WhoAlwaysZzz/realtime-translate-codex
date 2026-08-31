# Agentic Loop Status

Updated: 2026-09-01 02:12:00

- Run: agent iteration 11
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: a readable OCR crop without the selected token or a recognized
  masked-word boundary can still let topical overlap choose a multi-sense
  meaning. Context steering must require an anchor; otherwise the card must
  abstain.
- Change: `LocalContextSenseRanker` now rejects unanchored multi-sense context
  before scoring. Target-visible text and the existing masked noun-frame rule
  remain eligible. Added a regression for `bark` with `The dog runs toward the
  stranger.` and changed the prior `staunch ... supporter` case to require
  abstention because its crop has no trustworthy boundary anchor.
- Metric delta: on the bounded six-sample replay probe
  (`artifacts/context-anchor-probe-r2-20260901.json`), OCR exact remained 6/6,
  replay failures 0/6, expected abstention 1/1, while contextual Top-1 changed
  from 3/5 to 2/5. The one lost Top-1 is the former `staunch` false-confidence
  case; this is an intentional safety tradeoff, not an acceptance result.
- Real examples: `parrot` and `bark` retain anchored Top-1 readings; noisy
  `thrust` still abstains; `frail` with `th` OCR debris and `staunch` without
  the target/boundary now abstain instead of presenting a main sense.
- Regressions/limits: this probe is six archived real-game screenshots, not the
  required 50-image dual-transcribed corpus. No WER, gate precision/recall,
  rendered-card capture, or independent learning verdict is established. The
  PowerShell 7 runner is required because Windows PowerShell 5.1 misparses the
  UTF-8 Chinese literals in the verifier.
- Verification: Debug build completed with 0 warnings and 0 errors; focused
  suite passed 22/22; replay probe completed with 0 timeouts and 0 failures.
- Next question: can geometry-aware masked-word anchors recover safe context
  coverage for cases like `staunch` without reopening topical-overlap errors?
