---
design_type: phase
created_at: 2026-07-11
---

## Intent Contract

intent: Unify every user-visible WPF surface into a compact utility-style interface and publish the project as a safe, conventional public GitHub repository.
constraints: Preserve lookup, OCR, hotkey, archive, and AI behavior; keep the user's idea.md changes uncommitted; do not publish credentials or machine-private data.
success_criteria: All windows share one restrained visual system, build and tests pass, repository health files and Windows CI exist, secret scanning is clean, and the public GitHub repository is reachable.
risk_level: high

## Verification Contract

verify_steps:
  - run tests: dotnet run --project tests/RealtimeTranslate.Tests/RealtimeTranslate.Tests.csproj
  - check: dotnet build RealtimeTranslateCodex.sln and dotnet format RealtimeTranslateCodex.sln --verify-no-changes complete successfully
  - confirm: UI smoke checks pass, repository files contain no credentials, GitHub Actions configuration is valid, and the public remote is reachable

## Governance Contract

approval_gates: The user approved the compact utility visual direction, complete UI scope, MIT license, repository name, and autonomous execution including public publication.
rollback: Revert the focused UI or repository commits independently; change repository visibility or archive the GitHub repository if publication must be withdrawn.
ownership: Codex implements and verifies the changes; the repository owner controls credentials, releases, and future maintenance.

## Scope

| In | Out |
| --- | --- |
| Lookup overlays, explanation panel, manual input, hotkey settings, selection overlays, shared theme | Changes to OCR, dictionary, AI semantics, hotkeys, and persistence formats |
| MIT license, README, contribution and security guidance, issue and PR templates, Windows CI | Packaging installer, signed binaries, automated releases, hosted API services |
| Secret and privacy scan, public GitHub repository creation and push | Rewriting historical commits or publishing local archive data |

## Decisions

| # | Decision | Choice | Rejected alternatives |
| --- | --- | --- | --- |
| 1 | UI architecture | Evolve the existing shared theme and reusable builders | Full XAML migration; third-party UI framework |
| 2 | Visual direction | Compact, restrained desktop utility | Decorative Fluent showcase; large reading-card treatment |
| 3 | License | MIT | Apache-2.0; no license |
| 4 | Publication | Public `realtime-translate-codex` repository under the authenticated GitHub account | Private repository; unrelated repository name |
| 5 | Commit structure | Separate UI, repository metadata, and publication commits | One broad release commit |

## Surface

APIs: No public runtime API contracts change. Shared UI construction helpers may expand inside the WPF app.

Storage: Existing local settings, lookup archives, screenshots, dictionaries, and AI configuration remain unchanged and outside version control.

Components: The theme owns visual tokens and reusable control styles. Each window keeps responsibility for layout and interaction. GitHub metadata owns contribution workflows independently of runtime code.

Files touched: WPF presentation, selection, manual input, and settings surfaces; root repository health files; `.github` workflow and templates; project documentation.

## Risks & Open Questions

- Public publication is externally visible and can expose committed history, so credentials and machine-private paths must be scanned before push.
- Programmatically built WPF UI has limited snapshot-test support; deterministic theme tests and a live smoke run reduce but do not eliminate visual regression risk.
- The initial public repository will document source builds; installer and binary release automation remain future work.
