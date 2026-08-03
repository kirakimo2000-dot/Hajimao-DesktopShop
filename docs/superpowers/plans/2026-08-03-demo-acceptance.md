# Complete Playable Demo Acceptance Plan

**Goal:** Promote the verified first-phase Hajimao Demo to 1.0.0 and produce a clean Windows x64 release artifact.

### Task 1: Gameplay and stability gates

- [x] Add a complete new-game restock → price → customer → checkout → profit acceptance test.
- [x] Run 1,800 deterministic ticks (30 game minutes) under continuous customer pressure.
- [x] Reduce desktop-only refresh cadence to 0.5 Hz; retain 4 Hz while management is open.
- [x] Measure a live Release process for responsiveness, CPU time and working set.

### Task 2: Release documentation and packaging

- [x] Replace stale bootstrap README and reconcile the roadmap with delivered versions.
- [x] Promote version to 1.0.0 and publish self-contained Windows x64 output.
- [x] Create release archive and verify required executable, config and native runtime entries.

### Task 3: Final acceptance

- [x] Run all 1.0 tests, build, vulnerability audit and workspace hygiene checks.
- [x] Launch the packaged executable and perform final visual smoke testing.
- [x] Complete changelog, final progress report and project plan.
