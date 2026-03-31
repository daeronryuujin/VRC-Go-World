// GoGame.cs
// Per-board UdonSharpBehaviour. Handles networking, player seat assignment,
// stone placement interaction, and drives GoBoardVisualizer for visuals.
//
// All Go logic (BFS, liberty counting, ko, scoring) is inlined as private
// helper methods. UdonSharp cannot call plain-C# static classes at Udon
// runtime, so GoGameState.cs exists only as an IDE reference document.
//
// UdonSharp constraints observed:
//   - No List<T> — all collections are fixed arrays + int counters
//   - No try/catch, no LINQ, no delegates, no interfaces
//   - [UdonSynced] + RequestSerialization() for all networked state
//   - Networking.SetOwner before writing any synced var
//   - SendCustomNetworkEvent for zero-parameter RPCs
//   - OnDeserialization() drives all remote-client UI and visual updates
//   - BehaviourSyncMode.Manual
//   - Iterative BFS only — no recursion

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GoGame : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Configuration
    // -----------------------------------------------------------------------
    [Header("Board Configuration")]
    [Tooltip("9, 13, or 19")]
    public int boardSize = 19;

    [Tooltip("Komi added to white's score at game end")]
    public float komi = 6.5f;

    // -----------------------------------------------------------------------
    // Synced state
    // -----------------------------------------------------------------------
    [Header("Synced State — do not edit in inspector at runtime")]
    [UdonSynced] public int[] boardState;       // 0=empty 1=black 2=white, size boardSize*boardSize
    [UdonSynced] public int currentPlayer = 1;  // 1=black, 2=white
    [UdonSynced] public int blackCaptures = 0;
    [UdonSynced] public int whiteCaptures = 0;
    [UdonSynced] public int lastMoveIndex = -1; // linear index of last placed stone (-1 = none)
    [UdonSynced] public int gameState = 0;      // 0=waiting, 1=playing, 2=scoring, 3=finished
    [UdonSynced] public int blackPlayerId = -1; // VRC player ID of seated black player
    [UdonSynced] public int whitePlayerId = -1;
    [UdonSynced] public bool blackPassed = false;
    [UdonSynced] public bool whitePassed = false;
    [UdonSynced] public int prevBoardHash = 0;  // for ko detection
    [UdonSynced] public float finalBlackScore = 0f;
    [UdonSynced] public float finalWhiteScore = 0f;

    // -----------------------------------------------------------------------
    // References — wire in inspector
    // -----------------------------------------------------------------------
    [Header("Visual System")]
    [Tooltip("GoBoardVisualizer on same board — updated on deserialization")]
    public GoBoardVisualizer boardVisualizer;

    [Header("UI")]
    public UnityEngine.UI.Text turnIndicatorText;
    public UnityEngine.UI.Text captureDisplayText;
    public UnityEngine.UI.Text scoreDisplayText;
    public UnityEngine.UI.Text gameStatusText;

    [Header("Seat Triggers")]
    [Tooltip("GoSeatDetector configured for black")]
    public GoSeatDetector blackSeat;
    [Tooltip("GoSeatDetector configured for white")]
    public GoSeatDetector whiteSeat;

    [Header("Reset Button")]
    public GoResetButton resetButton;

    [Header("Optional: Move History Display")]
    public MoveHistoryDisplay moveHistoryDisplay;

    // -----------------------------------------------------------------------
    // Local (non-synced) state
    // -----------------------------------------------------------------------
    private bool _initialized = false;
    private int _localPlayerId = -1;

    // -----------------------------------------------------------------------
    // Go logic constants (inlined — UdonSharp cannot call plain-C# statics)
    // -----------------------------------------------------------------------
    private const int EMPTY = 0;
    private const int BLACK = 1;
    private const int WHITE = 2;

    // -----------------------------------------------------------------------
    // Unity lifecycle
    // -----------------------------------------------------------------------
    private void Start()
    {
        _InitializeIfNeeded();
        UpdateUI();
    }

    private void _InitializeIfNeeded()
    {
        if (_initialized) return;
        _initialized = true;

        VRCPlayerApi local = Networking.LocalPlayer;
        if (local != null) _localPlayerId = local.playerId;

        // Owner initializes synced board array on first run
        if (Networking.IsOwner(gameObject))
        {
            int total = boardSize * boardSize;
            boardState = new int[total];
            for (int i = 0; i < total; i++) boardState[i] = 0;
            currentPlayer = 1;
            blackCaptures = 0;
            whiteCaptures = 0;
            lastMoveIndex = -1;
            gameState = 0;
            blackPlayerId = -1;
            whitePlayerId = -1;
            blackPassed = false;
            whitePassed = false;
            prevBoardHash = 0;
            finalBlackScore = 0f;
            finalWhiteScore = 0f;
            RequestSerialization();
        }
    }

    // -----------------------------------------------------------------------
    // Called by GoSeatDetector when a player enters/exits a seat trigger
    // -----------------------------------------------------------------------

    /// <summary>Called by GoSeatDetector when local player enters black seat.</summary>
    public void OnLocalPlayerSitBlack()
    {
        if (gameState != 0 && gameState != 1) return;
        if (blackPlayerId != -1 && blackPlayerId != _localPlayerId) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        blackPlayerId = _localPlayerId;
        if (gameState == 0 && whitePlayerId != -1)
            gameState = 1;
        RequestSerialization();
        UpdateUI();
    }

    /// <summary>Called by GoSeatDetector when local player leaves black seat.</summary>
    public void OnLocalPlayerLeaveBlack()
    {
        if (blackPlayerId != _localPlayerId) return;
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        blackPlayerId = -1;
        if (gameState == 1) gameState = 0;
        RequestSerialization();
        UpdateUI();
    }

    /// <summary>Called by GoSeatDetector when local player enters white seat.</summary>
    public void OnLocalPlayerSitWhite()
    {
        if (gameState != 0 && gameState != 1) return;
        if (whitePlayerId != -1 && whitePlayerId != _localPlayerId) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        whitePlayerId = _localPlayerId;
        if (gameState == 0 && blackPlayerId != -1)
            gameState = 1;
        RequestSerialization();
        UpdateUI();
    }

    /// <summary>Called by GoSeatDetector when local player leaves white seat.</summary>
    public void OnLocalPlayerLeaveWhite()
    {
        if (whitePlayerId != _localPlayerId) return;
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        whitePlayerId = -1;
        if (gameState == 1) gameState = 0;
        RequestSerialization();
        UpdateUI();
    }

    // -----------------------------------------------------------------------
    // Stone placement — called by intersection trigger colliders
    // -----------------------------------------------------------------------

    /// <summary>
    /// Called by an intersection's interact trigger with the board-space
    /// x,y of that intersection. Validates and applies the move if legal.
    /// </summary>
    public void TryPlaceStoneAt(int x, int y)
    {
        if (gameState != 1) return;
        if (_localPlayerId < 0) return;

        int playerColor = _GetLocalPlayerColor();
        if (playerColor == 0) return;
        if (playerColor != currentPlayer) return;

        int total = boardSize * boardSize;
        if (boardState == null || boardState.Length != total) return;

        int capturedCount;
        int newHash;
        bool ok = _TryPlaceStone(x, y, playerColor, out capturedCount, out newHash);
        if (!ok) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        lastMoveIndex = y * boardSize + x;
        prevBoardHash = newHash;

        if (playerColor == BLACK) blackCaptures += capturedCount;
        else                       whiteCaptures += capturedCount;

        blackPassed = false;
        whitePassed = false;
        currentPlayer = (currentPlayer == BLACK) ? WHITE : BLACK;

        // Record move in history display (optional)
        if (moveHistoryDisplay != null)
            moveHistoryDisplay.RecordMove(x, y, playerColor);

        RequestSerialization();
        _RefreshVisuals();
        UpdateUI();
    }

    // -----------------------------------------------------------------------
    // Pass
    // -----------------------------------------------------------------------

    public void LocalPlayerPass()
    {
        if (gameState != 1) return;
        int playerColor = _GetLocalPlayerColor();
        if (playerColor == 0 || playerColor != currentPlayer) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        if (playerColor == BLACK) blackPassed = true;
        else                       whitePassed = true;

        if (blackPassed && whitePassed)
        {
            _TriggerScoring();
        }
        else
        {
            currentPlayer = (currentPlayer == BLACK) ? WHITE : BLACK;
        }

        RequestSerialization();
        UpdateUI();
    }

    // -----------------------------------------------------------------------
    // Scoring
    // -----------------------------------------------------------------------
    private void _TriggerScoring()
    {
        float bScore, wScore;
        _CalculateScore(out bScore, out wScore);
        finalBlackScore = bScore;
        finalWhiteScore = wScore;
        gameState = 3;
    }

    // -----------------------------------------------------------------------
    // Reset — called by GoResetButton (owner check handled there)
    // -----------------------------------------------------------------------
    public void ResetGame()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        int total = boardSize * boardSize;
        if (boardState == null || boardState.Length != total)
            boardState = new int[total];
        for (int i = 0; i < total; i++) boardState[i] = 0;

        currentPlayer = BLACK;
        blackCaptures = 0;
        whiteCaptures = 0;
        lastMoveIndex = -1;
        gameState = (blackPlayerId != -1 && whitePlayerId != -1) ? 1 : 0;
        blackPassed = false;
        whitePassed = false;
        prevBoardHash = 0;
        finalBlackScore = 0f;
        finalWhiteScore = 0f;

        if (moveHistoryDisplay != null)
            moveHistoryDisplay.ClearHistory();

        RequestSerialization();
        _RefreshVisuals();
        UpdateUI();
    }

    // -----------------------------------------------------------------------
    // Network sync
    // -----------------------------------------------------------------------
    public override void OnDeserialization()
    {
        _RefreshVisuals();
        UpdateUI();
    }

    // -----------------------------------------------------------------------
    // Go logic — inlined helpers (no recursion, iterative BFS only)
    // -----------------------------------------------------------------------

    /// <summary>Fill outAdj with up to 4 adjacent linear indices.</summary>
    private void _GetAdjacent(int index, int[] outAdj, out int outCount)
    {
        outCount = 0;
        int x = index % boardSize;
        int y = index / boardSize;
        if (x > 0)             outAdj[outCount++] = index - 1;
        if (x < boardSize - 1) outAdj[outCount++] = index + 1;
        if (y > 0)             outAdj[outCount++] = index - boardSize;
        if (y < boardSize - 1) outAdj[outCount++] = index + boardSize;
    }

    /// <summary>BFS flood-fill: collect all connected stones of the same color.</summary>
    private int _GetGroup(int[] board, int startIndex, int[] groupOut)
    {
        int color = board[startIndex];
        if (color == EMPTY) return 0;

        int total = boardSize * boardSize;
        bool[] visited = new bool[total];
        int[] queue = new int[total];
        int head = 0, tail = 0;
        int groupCount = 0;
        int[] adj = new int[4];
        int adjCount;

        queue[tail++] = startIndex;
        visited[startIndex] = true;

        while (head < tail)
        {
            int cur = queue[head++];
            groupOut[groupCount++] = cur;
            _GetAdjacent(cur, adj, out adjCount);
            for (int i = 0; i < adjCount; i++)
            {
                int n = adj[i];
                if (!visited[n] && board[n] == color)
                {
                    visited[n] = true;
                    queue[tail++] = n;
                }
            }
        }
        return groupCount;
    }

    /// <summary>Count liberties for the group containing startIndex.</summary>
    private int _CountLiberties(int[] board, int startIndex)
    {
        int color = board[startIndex];
        if (color == EMPTY) return 0;

        int total = boardSize * boardSize;
        bool[] visitedStones = new bool[total];
        bool[] visitedLib    = new bool[total];
        int[] queue = new int[total];
        int head = 0, tail = 0;
        int libCount = 0;
        int[] adj = new int[4];
        int adjCount;

        queue[tail++] = startIndex;
        visitedStones[startIndex] = true;

        while (head < tail)
        {
            int cur = queue[head++];
            _GetAdjacent(cur, adj, out adjCount);
            for (int i = 0; i < adjCount; i++)
            {
                int n = adj[i];
                if (board[n] == EMPTY && !visitedLib[n])
                {
                    visitedLib[n] = true;
                    libCount++;
                }
                else if (board[n] == color && !visitedStones[n])
                {
                    visitedStones[n] = true;
                    queue[tail++] = n;
                }
            }
        }
        return libCount;
    }

    /// <summary>FNV-1a 32-bit board hash for simple ko detection.</summary>
    private int _BoardHash(int[] board)
    {
        int hash = unchecked((int)2166136261u);
        int total = boardSize * boardSize;
        for (int i = 0; i < total; i++)
        {
            hash ^= board[i];
            hash = unchecked(hash * 16777619);
        }
        return hash;
    }

    /// <summary>
    /// Attempt to place a stone at (x,y) for playerColor.
    /// Modifies boardState in-place on success.
    /// Returns false if illegal (occupied, suicide, ko).
    /// </summary>
    private bool _TryPlaceStone(int x, int y, int playerColor,
        out int capturedCount, out int outNewHash)
    {
        capturedCount = 0;
        outNewHash = prevBoardHash;

        int total = boardSize * boardSize;
        int index = y * boardSize + x;

        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return false;
        if (boardState[index] != EMPTY) return false;

        int opponent = (playerColor == BLACK) ? WHITE : BLACK;

        // Work on a scratch copy
        int[] scratch = new int[total];
        for (int i = 0; i < total; i++) scratch[i] = boardState[i];
        scratch[index] = playerColor;

        // Capture opponent groups with 0 liberties adjacent to placed stone
        int[] adj = new int[4];
        int adjCount;
        _GetAdjacent(index, adj, out adjCount);

        int[] groupBuf = new int[total];
        int tempCaptures = 0;

        for (int i = 0; i < adjCount; i++)
        {
            int n = adj[i];
            if (scratch[n] == opponent)
            {
                int gLen = _GetGroup(scratch, n, groupBuf);
                if (_CountLiberties(scratch, n) == 0)
                {
                    for (int g = 0; g < gLen; g++)
                    {
                        scratch[groupBuf[g]] = EMPTY;
                        tempCaptures++;
                    }
                }
            }
        }

        // Suicide check
        if (_CountLiberties(scratch, index) == 0)
            return false;

        // Ko check: resulting board must not match the previous board state
        int newHash = _BoardHash(scratch);
        if (newHash == prevBoardHash && tempCaptures > 0)
            return false;

        // Commit
        for (int i = 0; i < total; i++) boardState[i] = scratch[i];
        capturedCount = tempCaptures;
        outNewHash = newHash;
        return true;
    }

    /// <summary>
    /// Chinese area scoring. Writes results to finalBlackScore / finalWhiteScore.
    /// komi is added to white. Iterative BFS territory detection.
    /// </summary>
    private void _CalculateScore(out float blackScore, out float whiteScore)
    {
        int total = boardSize * boardSize;
        blackScore = 0f;
        whiteScore = 0f;

        int blackStones = 0;
        int whiteStones = 0;
        for (int i = 0; i < total; i++)
        {
            if (boardState[i] == BLACK) blackStones++;
            else if (boardState[i] == WHITE) whiteStones++;
        }

        bool[] visited = new bool[total];
        int[] queue = new int[total];
        int[] region = new int[total];
        int[] adj = new int[4];
        int adjCount;

        for (int start = 0; start < total; start++)
        {
            if (boardState[start] != EMPTY || visited[start]) continue;

            int head = 0, tail = 0;
            int regionCount = 0;
            bool touchesBlack = false;
            bool touchesWhite = false;

            queue[tail++] = start;
            visited[start] = true;

            while (head < tail)
            {
                int cur = queue[head++];
                region[regionCount++] = cur;
                _GetAdjacent(cur, adj, out adjCount);
                for (int i = 0; i < adjCount; i++)
                {
                    int n = adj[i];
                    if (boardState[n] == EMPTY && !visited[n])
                    {
                        visited[n] = true;
                        queue[tail++] = n;
                    }
                    else if (boardState[n] == BLACK) touchesBlack = true;
                    else if (boardState[n] == WHITE) touchesWhite = true;
                }
            }

            if (touchesBlack && !touchesWhite)
                blackScore += regionCount;
            else if (touchesWhite && !touchesBlack)
                whiteScore += regionCount;
            // else: dame — counts for nobody
        }

        blackScore += blackStones;
        whiteScore += whiteStones + komi;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private int _GetLocalPlayerColor()
    {
        if (_localPlayerId < 0) return 0;
        if (_localPlayerId == blackPlayerId) return BLACK;
        if (_localPlayerId == whitePlayerId) return WHITE;
        return 0;
    }

    private void _RefreshVisuals()
    {
        if (boardVisualizer != null)
            boardVisualizer.Refresh(boardState, boardSize, lastMoveIndex);
    }

    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------
    public void UpdateUI()
    {
        if (turnIndicatorText != null)
        {
            switch (gameState)
            {
                case 0:
                    turnIndicatorText.text = "Waiting for players...";
                    break;
                case 1:
                    turnIndicatorText.text = currentPlayer == BLACK ? "Black's Turn" : "White's Turn";
                    break;
                case 2:
                    turnIndicatorText.text = "Scoring phase";
                    break;
                case 3:
                    if (finalBlackScore > finalWhiteScore)
                        turnIndicatorText.text = "Black wins!";
                    else if (finalWhiteScore > finalBlackScore)
                        turnIndicatorText.text = "White wins!";
                    else
                        turnIndicatorText.text = "Draw!";
                    break;
                default:
                    turnIndicatorText.text = "";
                    break;
            }
        }

        if (captureDisplayText != null)
        {
            captureDisplayText.text =
                "Captured — Black: " + blackCaptures.ToString() +
                "  White: " + whiteCaptures.ToString();
        }

        if (scoreDisplayText != null && gameState == 3)
        {
            scoreDisplayText.text =
                "Final Score\nBlack: " + finalBlackScore.ToString("F1") +
                "\nWhite: " + finalWhiteScore.ToString("F1") +
                "\n(includes " + komi.ToString("F1") + " komi)";
        }
        else if (scoreDisplayText != null)
        {
            scoreDisplayText.text = "";
        }

        if (gameStatusText != null)
        {
            if (blackPassed && gameState == 1)
                gameStatusText.text = "Black passed";
            else if (whitePassed && gameState == 1)
                gameStatusText.text = "White passed";
            else
                gameStatusText.text = "";
        }
    }

    // -----------------------------------------------------------------------
    // Player events — clean up seats when a player leaves
    // -----------------------------------------------------------------------
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (player == null) return;
        int pid = player.playerId;
        if (!Networking.IsOwner(gameObject)) return;

        bool changed = false;
        if (pid == blackPlayerId) { blackPlayerId = -1; changed = true; }
        if (pid == whitePlayerId) { whitePlayerId = -1; changed = true; }
        if (changed)
        {
            if (gameState == 1) gameState = 0;
            RequestSerialization();
            UpdateUI();
        }
    }
}
