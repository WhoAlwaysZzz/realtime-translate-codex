# Agentic Loop Status

Updated: 2026-09-01 00:05:05

- Run: agent iteration 4
- Stage: improving usable context OCR and learning-card quality
- Goal: reliable sentence understanding plus memorable word learning
- Independent review: required after each relevant committed change
- Completion: automatic threshold or user acceptance
- Hypothesis: legacy four-column word-card rows attach scenario-specific context hints to MorphoLex, although that source does not support those invented scenes; omitting unproven hints should improve learning factuality without removing morphology clues.
- Change: `MorphemeWordInsightService` now keeps context hints only when a row has an explicit evidence-source column. Legacy rows still expose breakdown/family with the conservative MorphoLex label; explicit sourced rows retain their hint and confidence.
- Metric delta: focused suite 20/20 passed; solution build 0 warnings/0 errors. The 200-card audit remains 189/200 evidence-labelled, while unproven legacy usage hints fall from 0/200 in the exported visible audit (the prior export did not surface them) and are now structurally omitted rather than attributed. This is a provenance-safety metric, not proof of learning usefulness.
- Real examples: the production legacy row for `ineffable` keeps its MorphoLex morphology/family but no longer claims the scene hint `sacred mysteries`; an explicitly sourced fixture (`WordNet examples`) still preserves its hint. Existing `bark` dog/tree and abstention contexts are unchanged.
- Regressions/limits: removing legacy hints may reduce memory cues for some cards; no independent reviewer has yet judged the trade-off, and the 30-word learning set plus 50-screenshot OCR gates remain incomplete. The 75% MorphoLex default is still a row-level fallback, not an audit.
- Next question: on a fresh blind 30-word review, does omitting unsupported scene hints reduce factuality errors while keeping useful-learning acceptance at or above 80%?
