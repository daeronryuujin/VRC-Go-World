# Autonomous Changes Log

This file lists what I can implement immediately in-repo without requiring your Unity scene access, and what I implemented.

## What I can handle right now
1. **Portable queue logic** that can be copied into UdonSharp with clear behavior.
2. **Rate-limiting and moderation guardrails** for queue + skip controls.
3. **Controller-style integration scaffolding** that maps to Udon events.
4. **Docs/planning updates** so your in-Unity steps stay unblocked.

## Rework in this update (addressing previous feedback)

### 1) Reworked queue state model into a more practical playback queue
- **File:** `scripts/Video/VideoQueueState.cs`
- **What changed:**
  - Switched to `current = queue[0]` style flow so skip means “remove current and advance”.
  - Added URL normalization/validation (`http/https` only).
  - Added input limits for URL and display name lengths.
  - Added result enum (`QueueActionResult`) so UIs can show exact failure reasons.
  - Added restore sanitization to ignore invalid entries from snapshots.

### 2) Improved cooldown helper for better UI feedback
- **File:** `scripts/Video/RateLimiter.cs`
- **What changed:**
  - Added `GetRemainingMs(...)` so you can display “wait Xs” hints on controls.

### 3) Upgraded controller guide with explicit authorization and outcomes
- **File:** `scripts/Video/VideoQueueControllerGuide.cs`
- **What changed:**
  - Request methods now return `QueueActionResult` (not plain bool).
  - Added privilege checks for skip/remove/clear flows.
  - Added helper accessors for “now playing” URL and queue display lines.

## Notes
- These are still scene-agnostic C# building blocks and are intended to be wired into UdonSharp behavior scripts in Unity.
- Next creator step: map request methods to button handlers + synced Udon fields.
