# VRChat Go World — Kanban Board

This board is intended to keep creator-side and autonomous tasks visible and unblocked.

## Columns
- **Backlog**: known work not yet selected.
- **Ready**: fully defined and unblocked.
- **In Progress**: currently being worked.
- **Blocked**: cannot proceed due to an explicit blocker.
- **Review/Validate**: implemented, pending checks/playtests.
- **Done**: accepted as complete.

## WIP Limits (recommended)
- In Progress: max **3** cards.
- Review/Validate: max **5** cards.

## Card Format
`[ID] Title (Owner: You|Agent, Depends: IDs or -, Parallel: Yes|No)`

---

## Backlog
- [Y5] Add synced video player prefab + controls (Owner: You, Depends: Y2, Parallel: No)
- [Y5Q] Add queue UX: now playing/up next/skip/clear (Owner: You, Depends: Y5, Parallel: No)
- [Y7V] Validate multiplayer video sync + late join (Owner: You, Depends: Y5Q, Parallel: No)
- [Y3] Import assets/materials for board + ambience (Owner: You, Depends: Y2, Parallel: Yes)
- [Y4] Place/wire board interaction colliders/raycast input (Owner: You, Depends: Y3,Y7V, Parallel: No)
- [Y6] Configure moderation defaults (Owner: You, Depends: Y5Q,Y4, Parallel: No)
- [Y7] Full multiplayer validation (board + video) (Owner: You, Depends: Y6, Parallel: No)
- [Y8] Final polish + publish metadata (Owner: You, Depends: Y7, Parallel: No)

## Ready
- (none)

## In Progress
- (none)

## Blocked
- (none)

## Review/Validate
- (none)

## Done
- [Y1] VCC project + Unity baseline (completed by creator)
- [A1] Go rules specification (`docs/go-rules-spec.md`)
- [A2] Networking specification (`docs/networking-spec.md`)
- [A3] UdonSharp architecture skeleton (`docs/udonsharp-architecture.md`)
- [A4] Deterministic test matrix (`docs/test-plan.md`)
- [A5] Moderation presets (`docs/moderation-presets.md`)
- [A6] Implementation runbook (`docs/implementation-runbook.md`)
- [A7] Kanban workflow + board artifact (`KANBAN.md`)
- [A8] Video-first setup guide (`docs/video-first-setup.md`)
- [A9] Video queue code + wireframe assets + change log (`scripts/Video/*`, `assets/ui/*`, `AUTONOMOUS_CHANGES.md`)

---

## Daily Update Protocol
1. Move cards across columns before and after each work session.
2. For blocked cards, append `Blocker:` reason and owner needed.
3. Keep dependencies explicit; do not move dependent cards to In Progress prematurely.
4. If WIP exceeds limits, pause new work and finish/review existing cards first.
