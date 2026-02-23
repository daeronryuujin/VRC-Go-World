# Video-First Setup Guide (Phase 1)

## Goal
Get the world hangout-ready quickly with synced video playback and queue controls before Go gameplay implementation.

## Scope
- In scope now: URL playback, queueing, skip/clear controls, permission model, sync validation.
- Out of scope now: full in-world YouTube search backend integration.

## Step-by-step
1. Place VRChat video player prefab in `VideoArea`.
2. Add world-space control panel:
   - URL input
   - Add-to-queue
   - Play/Pause
   - Skip
   - Clear queue
3. Define ownership model:
   - One controller/owner is authoritative for queue mutation.
   - Non-owners submit queue requests.
4. Add moderation defaults:
   - Everyone can suggest/add queue entries (optional).
   - Skip/Clear restricted to owner/mod role.
5. Add rate limits:
   - URL add: max 1 per user per 5–10s.
   - Skip: cooldown 3–5s global.
6. Late joiner behavior:
   - Rebuild queue UI from synced state.
   - Sync to current playback timestamp anchor.
7. Validate with 2–4 users:
   - Add/skip race behavior
   - Pause/resume convergence
   - Late join synchronization

## YouTube Search Extension (later)
If you still want search:
- Host external API wrapper service for YouTube search.
- Return sanitized playable URLs only.
- Add moderation + rate limiting + optional domain allowlist.
