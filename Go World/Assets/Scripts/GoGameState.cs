// GoGameState.cs
// Pure logic class (not a MonoBehaviour or UdonSharpBehaviour).
// Embedded inside GoGame.cs via composition — UdonSharp cannot reference
// plain C# classes from separate files in Udon programs, so this file
// is provided for IDE reference but the logic is duplicated inline in GoGame.cs.
//
// All flood-fill operations are iterative (array-based queue) — never recursive.
// No List<T>, no LINQ, no try/catch, no generics.

using UnityEngine;

/// <summary>
/// Self-contained Go game logic. Used as a plain C# helper class
/// compiled into the same assembly as GoGame. Because UdonSharp programs
/// cannot call into plain-C# class instances at runtime, all methods here
/// are static and receive state as parameters, allowing GoGame.cs to inline
/// the logic directly.
/// </summary>
public static class GoGameLogic
{
    // -----------------------------------------------------------------------
    // Constants
    // -----------------------------------------------------------------------
    public const int EMPTY = 0;
    public const int BLACK = 1;
    public const int WHITE = 2;
    public const int MAX_BOARD = 19 * 19; // 361

    // -----------------------------------------------------------------------
    // Adjacency helper
    // -----------------------------------------------------------------------
    /// <summary>Returns up to 4 adjacent linear indices. Fill count in outCount.</summary>
    public static void GetAdjacent(int index, int boardSize, int[] outAdj, out int outCount)
    {
        outCount = 0;
        int x = index % boardSize;
        int y = index / boardSize;

        if (x > 0)            outAdj[outCount++] = index - 1;
        if (x < boardSize - 1) outAdj[outCount++] = index + 1;
        if (y > 0)            outAdj[outCount++] = index - boardSize;
        if (y < boardSize - 1) outAdj[outCount++] = index + boardSize;
    }

    // -----------------------------------------------------------------------
    // Group flood-fill (iterative BFS)
    // Returns number of stones in group; fills groupOut array.
    // -----------------------------------------------------------------------
    public static int GetGroup(int[] board, int boardSize, int startIndex, int[] groupOut)
    {
        int color = board[startIndex];
        if (color == EMPTY) return 0;

        int total = boardSize * boardSize;
        bool[] visited = new bool[total];
        int[] queue = new int[total];
        int head = 0;
        int tail = 0;
        int groupCount = 0;

        queue[tail++] = startIndex;
        visited[startIndex] = true;

        int[] adj = new int[4];
        int adjCount;

        while (head < tail)
        {
            int cur = queue[head++];
            groupOut[groupCount++] = cur;
            GetAdjacent(cur, boardSize, adj, out adjCount);
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

    // -----------------------------------------------------------------------
    // Count liberties for the group that contains startIndex.
    // -----------------------------------------------------------------------
    public static int CountLiberties(int[] board, int boardSize, int startIndex)
    {
        int color = board[startIndex];
        if (color == EMPTY) return 0;

        int total = boardSize * boardSize;
        bool[] visitedStones = new bool[total];
        bool[] visitedLib = new bool[total];
        int[] queue = new int[total];
        int head = 0;
        int tail = 0;
        int libCount = 0;

        queue[tail++] = startIndex;
        visitedStones[startIndex] = true;

        int[] adj = new int[4];
        int adjCount;

        while (head < tail)
        {
            int cur = queue[head++];
            GetAdjacent(cur, boardSize, adj, out adjCount);
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

    // -----------------------------------------------------------------------
    // Compute a fast hash of the board (for simple ko detection).
    // Uses FNV-1a 32-bit.
    // -----------------------------------------------------------------------
    public static int BoardHash(int[] board, int size)
    {
        int hash = unchecked((int)2166136261u);
        int total = size * size;
        for (int i = 0; i < total; i++)
        {
            hash ^= board[i];
            hash = unchecked(hash * 16777619);
        }
        return hash;
    }

    // -----------------------------------------------------------------------
    // Attempt to place a stone. Modifies board in-place.
    // Returns true if the move was legal and applied.
    // capturedCount: number of opponent stones captured.
    // prevBoardHash: hash of board state BEFORE this move (for ko check).
    // outNewHash: hash after placement (for updating prev hash next turn).
    // -----------------------------------------------------------------------
    public static bool TryPlaceStone(
        int[] board, int boardSize,
        int x, int y, int player,
        int prevBoardHash,
        out int capturedCount,
        out int outNewHash)
    {
        capturedCount = 0;
        outNewHash = prevBoardHash;

        int total = boardSize * boardSize;
        int index = y * boardSize + x;

        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return false;
        if (board[index] != EMPTY) return false;

        int opponent = (player == BLACK) ? WHITE : BLACK;

        // --- Tentatively place stone ---
        // We work on a scratch copy for ko/suicide checks.
        int[] scratch = new int[total];
        for (int i = 0; i < total; i++) scratch[i] = board[i];
        scratch[index] = player;

        // --- Capture opponent groups with 0 liberties adjacent to placed stone ---
        int[] adj = new int[4];
        int adjCount;
        GetAdjacent(index, boardSize, adj, out adjCount);

        int[] groupBuf = new int[total];
        int tempCaptures = 0;

        for (int i = 0; i < adjCount; i++)
        {
            int n = adj[i];
            if (scratch[n] == opponent)
            {
                int gLen = GetGroup(scratch, boardSize, n, groupBuf);
                if (CountLiberties(scratch, boardSize, n) == 0)
                {
                    // Remove this group
                    for (int g = 0; g < gLen; g++)
                    {
                        scratch[groupBuf[g]] = EMPTY;
                        tempCaptures++;
                    }
                }
            }
        }

        // --- Suicide check: own group must have > 0 liberties after captures ---
        if (CountLiberties(scratch, boardSize, index) == 0)
        {
            return false; // Suicide
        }

        // --- Ko check: resulting board must not match previous board state ---
        int newHash = BoardHash(scratch, boardSize);
        if (newHash == prevBoardHash && tempCaptures > 0)
        {
            // Simple ko: single-stone capture that recreates prev state
            return false;
        }

        // --- Commit ---
        for (int i = 0; i < total; i++) board[i] = scratch[i];
        capturedCount = tempCaptures;
        outNewHash = newHash;
        return true;
    }

    // -----------------------------------------------------------------------
    // Chinese area scoring. Returns [blackScore, whiteScore].
    // komi is added to white's score (pass 6.5f).
    // Dead stones are not removed (caller handles that separately or ignores).
    // -----------------------------------------------------------------------
    public static void CalculateScore(int[] board, int boardSize, float komi,
        out float blackScore, out float whiteScore)
    {
        int total = boardSize * boardSize;
        blackScore = 0f;
        whiteScore = 0f;

        // Count stones
        int blackStones = 0;
        int whiteStones = 0;
        for (int i = 0; i < total; i++)
        {
            if (board[i] == BLACK) blackStones++;
            else if (board[i] == WHITE) whiteStones++;
        }

        // Count territory: empty intersections completely surrounded by one color.
        // BFS each empty region; if it only touches one color it belongs to that color.
        bool[] visited = new bool[total];
        int[] queue = new int[total];
        int[] adj = new int[4];
        int adjCount;

        for (int start = 0; start < total; start++)
        {
            if (board[start] != EMPTY || visited[start]) continue;

            // BFS this empty region
            int head = 0, tail = 0;
            int regionCount = 0;
            bool touchesBlack = false;
            bool touchesWhite = false;

            queue[tail++] = start;
            visited[start] = true;
            int[] region = new int[total];

            while (head < tail)
            {
                int cur = queue[head++];
                region[regionCount++] = cur;
                GetAdjacent(cur, boardSize, adj, out adjCount);
                for (int i = 0; i < adjCount; i++)
                {
                    int n = adj[i];
                    if (board[n] == EMPTY && !visited[n])
                    {
                        visited[n] = true;
                        queue[tail++] = n;
                    }
                    else if (board[n] == BLACK) touchesBlack = true;
                    else if (board[n] == WHITE) touchesWhite = true;
                }
            }

            // Assign territory
            if (touchesBlack && !touchesWhite)
                blackScore += regionCount;
            else if (touchesWhite && !touchesBlack)
                whiteScore += regionCount;
            // else: dame (neutral) — counts for nobody
        }

        // Add stones (Chinese scoring = stones + territory)
        blackScore += blackStones;
        whiteScore += whiteStones + komi;
    }
}
