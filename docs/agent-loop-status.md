# Agent Loop Status

Updated: 2026-08-31 02:02:00

- Run: made single-sense cards use the same sentence-level primary line as disambiguated cards: `本句中，<word>作<词性>，指“<释义>”。`.
- Hypothesis: even when no ranking is needed, an explicit part-of-speech and in-sentence meaning helps the player continue reading instead of interpreting a bare dictionary gloss.
- Evidence: Debug build completed with 0 warnings and 0 errors; executable suite passed 19/19. A real replay of index `700` completed with `ocr_exact=1/1`, `context_top1_sense=1/1`, `replay_failures=0`, and no timeout. The production card primary line was `本句中，calamity作名词，指“灾难、不幸事件”。`; evidence is in `artifacts/single-sense-700-20260831.json`.
- Guard evidence: a separate real replay of the two deliberate no-context cases `1122,1123` completed with `ocr_exact=2/2`, `abstained=2/2`, `replay_failures=0`, and no timeouts; `shelter` and `bark` still show `无法确定：` with only two candidates. Evidence is in `artifacts/abstention-guard-20260831.json`.
- Before/after quality evidence: before this change `calamity` displayed the bare primary definition `灾难、不幸事件`; afterwards the first line states both grammatical role and sentence meaning. Existing masked-frame improvements remain unchanged. No full-corpus improvement is claimed from this single-sample run.
- Remaining blocker: `thrust` and `attendant` still lack sufficiently reliable context and should remain explicit abstentions unless a new evidence source is added. The required blind human review and two additional ambiguity examples are also not recorded.
- Next: collect/add two real ambiguous game contexts, then run and record the blind first-screen review across the 20-sample corpus; use that evidence to decide whether `thrust`/`attendant` need another content-quality change. Do not change `docs/agent-loop.accepted`.
