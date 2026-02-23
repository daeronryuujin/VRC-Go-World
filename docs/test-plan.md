# Deterministic Test Plan

## 1) Rule Logic Cases

| ID | Case | Expected |
|---|---|---|
| R1 | Place on empty intersection | Accepted |
| R2 | Place on occupied intersection | Rejected `E_OCCUPIED` |
| R3 | Single-stone capture center | Captured stone removed |
| R4 | Multi-stone capture edge | Entire group removed |
| R5 | Suicide without capture | Rejected `E_SUICIDE` |
| R6 | Self-atari that captures opponent | Accepted |
| R7 | Immediate ko recapture | Rejected `E_KO` |
| R8 | Two consecutive passes | Transition to `ScoringPending` |
| R9 | Move attempt in `ScoringPending` | Rejected `E_GAME_NOT_ACTIVE` |
| R10 | Reset from active game | Clean initial state |

## 2) Networking Cases

| ID | Case | Expected |
|---|---|---|
| N1 | Two players move simultaneously | One accepted, one rejected/stale |
| N2 | Late join mid-game | Board + turn exactly reconstructed |
| N3 | Owner leaves during active game | New owner continues from synced state |
| N4 | Packet/order jitter simulation | No lasting desync |
| N5 | Forced resync | Visuals match authoritative board |

## 3) Video Sync Cases

| ID | Case | Expected |
|---|---|---|
| V1 | URL set by authorized user | Loads for all clients |
| V2 | Unauthorized URL set attempt | Rejected with UI feedback |
| V3 | Pause/resume by controller | Clients converge on same state |
| V4 | Seek operation | Clients converge near target time |
| V5 | Late join while playing | Joins near current playback time |

## 4) Performance/Robustness
- Verify stable frame time with full 19x19 board populated.
- Confirm no per-frame GC spikes from board updates.
- Spam-protection checks for move/video control rate limits.

## Exit Criteria
- All R* and N* cases pass in at least one multiplayer session.
- No critical desyncs after 30-minute soak test.
- Video controls remain usable after owner transfer.
