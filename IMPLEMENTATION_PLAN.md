# VRChat Go World Implementation Plan

This plan is split into:
- **Tasks for you**: work that requires Unity/VRChat account access, scene editing, and manual validation.
- **Tasks I can do autonomously**: work I can produce in this repository now (documentation, scripts, UdonSharp scaffolding, checklists).

Each task includes a **Parallel** field:
- **Yes** = can be started now in parallel with unrelated tasks.
- **No** = depends on previous tasks and should be sequenced.

---

## Priority change: Video-first
Per latest direction, implementation order is now:
1. Base world setup (already started by you).
2. **Video player integration first** (hangout-ready).
3. Video-focused multiplayer validation.
4. Go board implementation after video foundation is stable.

---

## Tasks for you (creator-side)

| ID | Task | Deliverable | Parallel | Depends on |
|---|---|---|---|---|
| Y1 | Create/upgrade the VRChat world project in VCC (correct Unity + SDK versions). | Working Unity project that opens without SDK errors. | No | — |
| Y2 | Build base scene blockout (table, board area, video screen area, lighting, spawn points). | Playable scene layout with navigation and clear focal areas. | No | Y1 |
| Y5 | Add VRChat video player prefab and hook world-space controls. | URL input + transport controls visible and usable in-world. | No | Y2 |
| Y5Q | Add synced queue UX (now playing, up next, skip, clear). | Usable queue-based hangout flow. | No | Y5 |
| Y7V | Multiplayer validation focused on video sync + late join behavior. | Test notes for sync/perms/usability before Go work starts. | No | Y5Q |
| Y3 | Import world assets/materials (board wood, stones, UI fonts/icons, ambience). | Clean asset folders and assigned materials. | Yes | Y2 |
| Y4 | Place and wire board interaction colliders/raycast input in scene. | Reliable click/interaction on each board intersection. | No | Y3, Y7V |
| Y6 | Configure permissions/moderation defaults (reset rights, video URL/queue control). | Stable public-instance behavior with anti-grief constraints. | No | Y5Q, Y4 |
| Y7 | Full multiplayer validation (board + video, 2–8 players, late joiners). | Test notes + bug list for desyncs/usability issues. | No | Y6 |
| Y8 | Final polish and publish metadata (thumbnail, description, capacity, comfort). | Publish-ready world and release notes. | No | Y7 |

---

## Tasks I can do autonomously (repo-side)

| ID | Task | Deliverable | Parallel | Depends on |
|---|---|---|---|---|
| A1 | Write technical specification for Go rules (capture, suicide, ko, pass/end flow). | `docs/go-rules-spec.md` | Yes | — |
| A2 | Define network sync protocol (authoritative owner flow, state serialization, late-join rebuild). | `docs/networking-spec.md` | Yes | — |
| A3 | Provide UdonSharp architecture skeleton (BoardManager, GroupAnalyzer, UIController, VideoSyncController). | `docs/udonsharp-architecture.md` | Yes | A1, A2 |
| A4 | Create deterministic test matrix/checklists for board logic and sync edge cases. | `docs/test-plan.md` | Yes | A1, A2 |
| A5 | Draft moderation policy presets (private club / semi-public / public instance profiles). | `docs/moderation-presets.md` | Yes | — |
| A6 | Write implementation runbook mapping creator tasks to verification gates. | `docs/implementation-runbook.md` | No | A1–A5 |
| A8 | Add video-first integration guide (queue-first, search constraints, moderation model). | `docs/video-first-setup.md` | Yes | A2, A5 |

---

## Recommended parallel execution map

### Phase 1 (start now)
- **You**: Complete Y2 baseline, then Y5 -> Y5Q -> Y7V.
- **Me**: Ensure all autonomous deliverables are ready for immediate use in this phase.
  - Already complete: A1, A2, A3, A4, A5, A6.
  - New in this phase: A8 (video-first setup guide).

### Phase 2 (after video stabilization)
- **You**: Y3 in parallel with remaining video polish.
- **You**: Y4 (Go interaction layer) once video hangout flow is stable.

### Phase 3 (converge + release)
- **You**: Y6 then Y7 then Y8.
- **Me**: Keep docs/Kanban synchronized with test outcomes.

---

## YouTube search and queue note
- **Queue support** is practical in-world and is now in scope for immediate build (Y5Q).
- **In-world YouTube search** generally requires an external backend/API and moderation controls; this is treated as a later extension after queue-first stability.

---

## Next best actions (immediate)
1. **You**: Finish Y2 scene baseline for comfortable viewing/interactions.
2. **You**: Execute Y5 then Y5Q for hangout-ready video playback.
3. **Me**: Deliver A8 and align Kanban statuses to the video-first path.
4. **You + Me**: Track execution state in `KANBAN.md` so blocked/in-progress work stays visible.

---

## Autonomous execution update

The following autonomous tasks are completed in this repository:
- A1 → `docs/go-rules-spec.md`
- A2 → `docs/networking-spec.md`
- A3 → `docs/udonsharp-architecture.md`
- A4 → `docs/test-plan.md`
- A5 → `docs/moderation-presets.md`
- A6 → `docs/implementation-runbook.md`
