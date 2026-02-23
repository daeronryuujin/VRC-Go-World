# Go Rules Technical Specification (VRChat World)

## Scope
Defines the deterministic game-rule layer for board sizes 9x9, 13x13, and 19x19.

## Board Model
- `boardSize`: `9 | 13 | 19`.
- `intersections`: flat array length `boardSize * boardSize`.
- Cell values:
  - `0 = Empty`
  - `1 = Black`
  - `2 = White`
- `currentPlayer`: `1 | 2`.
- `consecutivePasses`: integer.
- `moveNumber`: integer.
- `koHash`: board hash for immediate-ko prevention.
- `historyHashes[]`: optional for superko experimentation (disabled by default).

## Coordinate Conventions
- `index = y * boardSize + x`
- `x` increases left->right from player-facing board orientation.
- `y` increases bottom->top.
- Orthogonal neighbors only (no diagonal liberties).

## Move Validation
A move is legal iff all checks pass in order:
1. Target intersection is inside bounds.
2. Target intersection is empty.
3. Place stone tentatively.
4. Resolve captures on adjacent opponent groups with zero liberties.
5. Evaluate own group liberties post-capture.
   - If own group has zero liberties and no captures occurred, move is illegal (suicide).
6. Compute board hash and reject if equal to `koHash` (basic ko).

If legal:
- Commit board.
- Set `koHash` to previous board hash only when exactly one stone was captured and shape qualifies for immediate recapture prevention.
- `currentPlayer` toggles.
- `consecutivePasses = 0`.
- Increment `moveNumber`.

## Group + Liberty Algorithm
- `CollectGroup(startIndex, color)` via BFS/DFS over orthogonal same-color neighbors.
- `CountLiberties(group)` counts distinct orthogonal empty neighbors.
- Capture any adjacent opponent group with liberties `0` after placement.

## Pass / End-of-Round Flow
- `Pass()` toggles `currentPlayer`, increments `moveNumber`, increments `consecutivePasses`.
- If `consecutivePasses >= 2`:
  - Mark game state `ScoringPending`.
  - Disable placement input until reset/rematch (MVP behavior).

## Scoring Policy (MVP)
MVP does **not** auto-score territory in-world.
- World UX shows: “Two passes reached. Use external scoring agreement or reset/rematch.”
- Future extension: add Japanese/Chinese scoring selectable pre-game.

## Reset Behavior
- Clears board and visuals.
- Resets `currentPlayer = Black`, `consecutivePasses = 0`, `moveNumber = 1`, `koHash = none`.
- Broadcast one authoritative reset event to all clients.

## Determinism Requirements
- No random branches in rule logic.
- Stable neighbor iteration order (N,E,S,W) for reproducible traversal.
- Hash algorithm fixed and versioned (e.g., FNV-1a over board bytes + currentPlayer).

## Error Codes (for UI)
- `E_OUT_OF_BOUNDS`
- `E_OCCUPIED`
- `E_SUICIDE`
- `E_KO`
- `E_NOT_YOUR_TURN`
- `E_GAME_NOT_ACTIVE`
