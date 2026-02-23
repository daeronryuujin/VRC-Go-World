# Implementation Runbook

## Purpose
Map creator-side tasks to objective verification gates.

## Gates

### Gate G1: Project Baseline
- Input tasks: Y1.
- Verify:
  - Correct Unity + SDK versions.
  - No compile errors.

### Gate G2: Scene Baseline
- Input tasks: Y2, Y3.
- Verify:
  - Board and video zones clearly laid out.
  - Materials and interaction distances are readable in VR and desktop.

### Gate G3: Board Interaction
- Input tasks: Y4 + A1/A3 implemented.
- Verify using `docs/test-plan.md` R1–R6.

### Gate G4: Video Sync
- Input tasks: Y5 + A2 implemented.
- Verify using `docs/test-plan.md` V1–V5.

### Gate G5: Moderation Hardening
- Input tasks: Y6 + A5 implemented.
- Verify permission matrix and rate limits.

### Gate G6: Multiplayer Stability
- Input tasks: Y7 + A4.
- Verify N1–N5 and 30-minute soak.

### Gate G7: Publish Ready
- Input tasks: Y8.
- Verify metadata, thumbnail, capacity, and final smoke tests.

## Autonomous Work Execution Status
- A1: Completed (spec added).
- A2: Completed (networking spec added).
- A3: Completed (architecture defined).
- A4: Completed (test matrix authored).
- A5: Completed (moderation presets added).
- A6: Completed (this runbook).
