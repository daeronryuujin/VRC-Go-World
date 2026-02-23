# Networking Spec (Udon / VRChat)

## Goal
Ensure all players observe identical board/video state with reliable late-join reconstruction.

## Authority Model
- Single authoritative `BoardManager` network owner processes moves.
- Non-owners submit move requests (event with coordinate + client move nonce).
- Owner validates with rules engine and publishes committed state.

## Synced Data
### Go Board
- Synced byte array: flattened board state.
- Synced primitives:
  - `currentPlayer`
  - `moveNumber`
  - `consecutivePasses`
  - `gamePhase` (`Playing | ScoringPending`)
  - optional `lastErrorCode` for local UX only (not required synced)
- Optional compact move history ring buffer for debug replay.

### Video Player
- `videoUrl`
- `playState` (`Stopped | Playing | Paused`)
- `timeAnchorServerMs`
- `mediaTimeAtAnchorSec`
- `controllerPlayerId` (for moderation/auditing)

## Move Request Flow
1. Client clicks intersection.
2. Client sends `RequestMove(x, y, nonce)` to owner.
3. Owner validates turn + legality.
4. If legal:
   - Update authoritative state.
   - `RequestSerialization()`.
   - Broadcast optional feedback event (stone place sound).
5. If illegal:
   - Send rejection event back to requester with error code.

## Conflict Handling
- If two requests race, owner processes in receive order.
- Any request with stale expectations (implicit by changed board/turn) fails legality and is rejected.

## Late Joiners
- On deserialize, reconstruct board visuals from synced array only.
- Do not replay transient VFX/SFX events.
- UI turn indicator always derives from synced `currentPlayer`.

## Ownership Transfer
- On owner leave:
  - VRChat assigns new owner.
  - New owner should not mutate state until first deserialize complete.
  - Use `isStateHydrated` guard before accepting requests.

## Rate Limits
- Move requests: max 4/sec per player.
- Video URL changes: max 1/10 sec globally.
- Seek operations: max 2/5 sec for controller role.

## Serialization Budget Notes
- Prefer compact byte arrays over large string histories.
- Batch updates; avoid serializing on purely cosmetic UI changes.

## Recovery
- Provide `ResyncAll()` admin action to force visual rebuild from authoritative synced data.
- Keep local caches disposable; synced data remains source of truth.
