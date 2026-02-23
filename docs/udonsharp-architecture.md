# UdonSharp Architecture Skeleton

## Components
1. `BoardManager` (authoritative sync + command entrypoint)
2. `RuleEngine` (pure rule checks, no networking)
3. `GroupAnalyzer` (BFS group/liberty utilities)
4. `BoardView` (stone pooling + board render updates)
5. `BoardInput` (raycast/collider mapping to coordinates)
6. `BoardUIController` (turn, errors, pass/reset controls)
7. `VideoSyncController` (URL/state/time sync + permissions)

## Responsibilities
### BoardManager
- Owns synced fields.
- Accepts request events and enforces turn/phase checks.
- Calls `RuleEngine.TryApplyMove(...)`.
- Serializes authoritative state.

### RuleEngine
- Stateless or state-in/state-out APIs.
- Implements occupancy, capture, suicide, ko checks.
- Returns structured result: `{ success, errorCode, capturedIndices[] }`.

### GroupAnalyzer
- `CollectGroup(board, startIndex, color)`
- `CountLiberties(board, groupIndices)`
- Deterministic neighbor ordering.

### BoardView
- Stone object pool separated by color.
- `RebuildFromBoard(byte[] board)` for late join + resync.
- Optional delta updates for owner-side smoothness.

### VideoSyncController
- Applies moderation checks before changing URL/transport state.
- Maintains anchor-time model for join synchronization.

## Scene Wiring
- `WorldRoot`
  - `BoardSystem`
    - `BoardManager`
    - `BoardInput`
    - `BoardView`
    - `BoardUI`
  - `VideoSystem`
    - `VideoSyncController`
    - `VideoPlayerPrefab`

## Suggested Script Stubs
- `Assets/Scripts/Board/BoardManager.cs`
- `Assets/Scripts/Board/RuleEngine.cs`
- `Assets/Scripts/Board/GroupAnalyzer.cs`
- `Assets/Scripts/Board/BoardView.cs`
- `Assets/Scripts/Board/BoardInput.cs`
- `Assets/Scripts/UI/BoardUIController.cs`
- `Assets/Scripts/Video/VideoSyncController.cs`

## MVP Milestone Mapping
- M1: Local board rules + visuals.
- M2: Networked board authority + late join.
- M3: Synced video controls + moderation constraints.
- M4: UX polish and resiliency hooks.
