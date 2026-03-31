# VRC Go World — Progress

## v0.2.0 — Lounge Scripts, Spectator, Tutorial (current)

### Core Go Logic (v0.1 baseline)

All scripts follow UdonSharp hard rules: no generics, no List<T>, no LINQ,
no try/catch, no delegates. Board state synced via [UdonSynced] arrays +
RequestSerialization(). BFS is iterative (array queue with head/tail pointers).

| Script | Type | Status |
|---|---|---|
| GoGameState.cs | Plain C# static class | Complete. Pure Go logic: adjacency, BFS flood-fill, liberty counting, TryPlaceStone, Chinese scoring. |
| GoGame.cs | UdonSharpBehaviour (Manual sync) | Complete. Networked per-board game state, stone placement, pass, scoring, seat assignment, player-left cleanup. |
| GoBoardVisualizer.cs | UdonSharpBehaviour (None) | Complete. Enables/disables pre-placed stone GameObjects; swaps materials; highlights last move. |
| GoSeatDetector.cs | UdonSharpBehaviour (None) | Complete. Trigger collider notifies GoGame when local player sits/stands. |
| GoIntersectionTrigger.cs | UdonSharpBehaviour (None) | Complete. One per intersection; Interact() calls TryPlaceStoneAt(x,y). |
| GoResetButton.cs | UdonSharpBehaviour (None) | Complete. Seated players or master can reset. |
| GoPassButton.cs | UdonSharpBehaviour (None) | Complete. Passes the local player's turn. |
| GoGameManager.cs | UdonSharpBehaviour (None) | Complete. Scene-level singleton; holds 3 board refs; spectator cam auto-follows active game; hoshi position data. |
| GoWorldSetup.cs | Editor-only (no Udon) | Complete. Menu tool: creates intersection triggers, pre-placed stones, hoshi dots for any board size. |

### Ko Rule

Simple ko is implemented via FNV-1a 32-bit board hash comparison:
- Before committing a move, the post-capture board hash is compared to `prevBoardHash`.
- If they match AND at least one stone was captured, the move is rejected.
- `prevBoardHash` is synced and updated after every legal placement.

Superko (positional or situational) is NOT implemented. This is intentional for
casual VRChat play — simple ko covers the vast majority of situations.

### Scoring

Chinese area scoring: stones on board + surrounded empty intersections.
White komi: 6.5 (prevents draws). Dead stone removal not implemented.

### v0.2.0 New Scripts

| Script | Type | Purpose |
|---|---|---|
| SpectatorCamera.cs | UdonSharpBehaviour (None) | Spectators cycle between boards with next/prev. TextMeshPro overlay shows turn, captures, score. Local-only. |
| GameTimerDisplay.cs | UdonSharpBehaviour (Manual) | Synced elapsed timer per board. Uses Networking.GetServerTimeInSeconds. Resets on new game. |
| MoveHistoryDisplay.cs | UdonSharpBehaviour (Manual) | Last 10 moves stored as synced int arrays. Displays in Go column notation (A-T, skip I) + 1-based row numbers. |
| AmbientMusicPlayer.cs | UdonSharpBehaviour (None) | Local background music. VolumeUp/VolumeDown/ToggleMute/Pause/Resume API. Interact toggles mute. |
| PlayerCountDisplay.cs | UdonSharpBehaviour (None) | Displays VRCPlayerApi.GetPlayerCount() on TextMeshPro. Updates on join/leave. |
| GoTutorialDisplay.cs | UdonSharpBehaviour (None) | 5-page tutorial (Basic Rules, Capturing, Ko, Scoring, Etiquette). Hard-coded string arrays. Local-only. |

### Meta Files

All .cs files under Assets/Scripts/ and Assets/Editor/ now have matching .meta
files with unique GUIDs. Folder-level .meta files (Scripts.meta, Editor.meta)
also created.

---

## Wiring Checklist (scene setup still needed in Unity Editor)

### Per Board (Board_19x19, Board_13x13, Board_9x9)

- [ ] Run "Go World > Setup Board Intersections" to create triggers + stones + hoshi
- [ ] Assign GoGame fields: boardSize, komi, boardVisualizer, UI texts, blackSeat, whiteSeat, resetButton
- [ ] Wire GoSeatDetector x2 (seatColor 1 and 2) to GoGame
- [ ] Wire GoResetButton to GoGame
- [ ] Wire GoPassButton x2 to GoGame
- [ ] Wire MoveHistoryDisplay to GoGame (call RecordMove/ClearHistory from GoGame)
- [ ] Wire GameTimerDisplay to GoGame

### Scene Level

- [ ] GoGameManager: wire board19x19, board13x13, board9x9 refs
- [ ] SpectatorCamera: wire boards[], spectatorCam, TextMeshPro overlay labels
- [ ] AmbientMusicPlayer: assign AudioSource with ambient clip
- [ ] PlayerCountDisplay: assign TextMeshPro label
- [ ] GoTutorialDisplay: assign titleText, bodyText, pageIndicatorText; wire next/prev buttons

---

## What Remains

- [ ] Scene geometry: board mesh, lounge environment, seating areas
- [ ] Materials: black/white stone materials, board surface, hoshi dot material
- [ ] Audio: ambient music clip assignment
- [ ] UI canvas setup: all TextMeshPro labels in-world
- [ ] Dead stone marking during scoring phase (advanced feature)
- [ ] Superko detection (advanced feature)
- [ ] Time controls / byo-yomi (advanced feature)
- [ ] Review mode (step back through move history) (advanced feature)
