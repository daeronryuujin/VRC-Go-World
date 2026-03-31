# CLAUDE.md — VRC-Go-World

UdonSharp VRChat world project. Unity project root is `Go World/` (note the space).

## Project Layout

```
Go World/           <- Unity project root (open this in Unity Editor)
  Assets/
    Scripts/        <- All UdonSharp runtime scripts
    Editor/         <- Unity Editor-only scripts (not compiled into Udon)
    Scenes/
    Materials/
    Prefabs/
```

## Script Summary

| File | Type | Purpose |
|---|---|---|
| GoGameState.cs | Plain C# static class | Pure Go logic (board, captures, ko, scoring). Not an UdonBehaviour. |
| GoGame.cs | UdonSharpBehaviour | Per-board networked game state. One per physical board. |
| GoBoardVisualizer.cs | UdonSharpBehaviour | Shows/hides pre-placed stone GameObjects from board array. |
| GoSeatDetector.cs | UdonSharpBehaviour | Trigger collider — assigns local player to black/white seat. |
| GoIntersectionTrigger.cs | UdonSharpBehaviour | One per intersection — VRC Interact fires TryPlaceStoneAt. |
| GoResetButton.cs | UdonSharpBehaviour | Interact to reset board. Seated players or master only. |
| GoPassButton.cs | UdonSharpBehaviour | Interact to pass turn. |
| GoGameManager.cs | UdonSharpBehaviour | Scene-level manager. Holds all 3 board refs. Spectator cam logic. |
| Assets/Editor/GoWorldSetup.cs | Editor-only | Menu tool to auto-create intersections/stones/hoshi for a board. |

## UdonSharp Hard Rules

These apply to ALL scripts in Assets/Scripts/:

- Inherit `UdonSharpBehaviour`, NEVER `MonoBehaviour`
- NO generics: no `List<T>`, no `Dictionary<K,V>` — use arrays + int counters
- NO `try/catch`, NO interfaces, NO abstract classes
- NO LINQ (no `.Where`, `.Select`, etc.)
- NO lambdas, NO delegates, NO C# events
- NO `System.IO`, NO `System.Net`
- `[UdonSynced]` on every networked field + `RequestSerialization()` after writing
- `Networking.SetOwner(Networking.LocalPlayer, gameObject)` BEFORE writing any synced var
- `SendCustomNetworkEvent(NetworkEventTarget, "MethodName")` — method must take NO parameters
- `[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]` on all networked behaviours
- `OnDeserialization()` fires on remote clients — update UI and visuals there
- No recursion for flood-fill/BFS — use iterative queue (int[] + head/tail pointers)
- `VRCUrl` cannot be created at runtime — inspector-assigned only
- `% (modulo)` on `long` is not supported — cast to `int` first

## Scene Board Wiring (per board)

Each board root (e.g. `Board_19x19`) needs:
- `GoGame` UdonBehaviour: set `boardSize`, wire `boardVisualizer`, UI texts, seat detectors, reset button
- `GoBoardVisualizer` UdonBehaviour: `stoneObjects[]` array (run Editor setup tool to populate)
- `GoSeatDetector` x2: one `seatColor=1` (black), one `seatColor=2` (white), both pointing to `GoGame`
- `GoIntersectionTrigger` x (size*size): each with correct `boardX/boardY`, pointing to `GoGame`
  - Run **Go World > Setup Board Intersections** from the menu to auto-create these
- `GoResetButton`: wire to `GoGame`
- `GoPassButton` x2 (one per seat side): wire to `GoGame`

## Stone Placement Strategy (Option A — Pre-placed)

All stone GameObjects are created at build time at every intersection, disabled.
`GoBoardVisualizer.Refresh()` enables/disables them and swaps materials based on `boardState[]`.
This avoids `VRCInstantiate` complexity and syncs correctly via `OnDeserialization`.

## Scoring

Chinese area scoring (stones + surrounded empty intersections).
White gets 6.5 komi. Dead stone removal is not implemented — dead stones in enemy territory
hurt the player who owns them, which is correct under Chinese rules in casual play.

## Board Sizes

| Size | Total intersections | Komi |
|---|---|---|
| 9x9  | 81  | 6.5 |
| 13x13 | 169 | 6.5 |
| 19x19 | 361 | 6.5 |

## Tiresias

Run `curl http://localhost:7890/compiler/errors` before and after any script changes.
Run `curl http://localhost:7890/scene/hierarchy` to verify scene objects exist before referencing them.

## Git

Default branch: `master`. Remote: GitHub (daeronryuujin).
