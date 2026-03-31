// GoGameManager.cs
// Scene-level singleton. Holds references to all three boards and provides
// utilities usable by any board (e.g., broadcasting a "game started" message,
// managing spectator camera switching).
//
// Also holds the star-point (hoshi) position data for each board size so
// the Editor setup tool (GoWorldSetup) can query it without duplicating data.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GoGameManager : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Board references
    // -----------------------------------------------------------------------
    [Header("Board References")]
    public GoGame board19x19;
    public GoGame board13x13;
    public GoGame board9x9;

    // -----------------------------------------------------------------------
    // Spectator camera
    // -----------------------------------------------------------------------
    [Header("Spectator Camera")]
    [Tooltip("Optional overhead camera showing the active game.")]
    public Camera spectatorCam;

    [Tooltip("Render texture the spectator cam writes to (for a display panel)")]
    public RenderTexture spectatorRenderTexture;

    // -----------------------------------------------------------------------
    // Star-point data (hoshi positions)
    // Stored as flat int pairs: [x0, y0, x1, y1, ...]
    // -----------------------------------------------------------------------
    // 9x9 — 5 hoshi
    private readonly int[] HOSHI_9 = new int[]
    {
        2,2,  6,2,
        2,6,  6,6,
        4,4
    };
    // 13x13 — 9 hoshi
    private readonly int[] HOSHI_13 = new int[]
    {
        3,3,  9,3,
        3,9,  9,9,
        6,6,
        3,6,  6,3,
        6,9,  9,6
    };
    // 19x19 — 9 hoshi
    private readonly int[] HOSHI_19 = new int[]
    {
        3,3,   9,3,  15,3,
        3,9,   9,9,  15,9,
        3,15,  9,15, 15,15
    };

    // -----------------------------------------------------------------------
    // Public API — returns a copy of the hoshi list for a given board size.
    // Returns flat [x,y,...] pairs; count = returned value * 2 for actual indices.
    // -----------------------------------------------------------------------
    public int[] GetHoshiPositions(int size)
    {
        if (size == 9)  return (int[])HOSHI_9.Clone();
        if (size == 13) return (int[])HOSHI_13.Clone();
        if (size == 19) return (int[])HOSHI_19.Clone();
        return new int[0];
    }

    public int GetHoshiCount(int size)
    {
        if (size == 9)  return HOSHI_9.Length / 2;
        if (size == 13) return HOSHI_13.Length / 2;
        if (size == 19) return HOSHI_19.Length / 2;
        return 0;
    }

    // -----------------------------------------------------------------------
    // Spectator cam switching — point at whichever board is currently active
    // -----------------------------------------------------------------------
    private void Update()
    {
        if (spectatorCam == null) return;

        // Prioritize whichever board is actively being played
        GoGame activeBoard = _FindActiveGame();
        if (activeBoard != null)
        {
            // Position camera above the active board
            Vector3 boardCenter = activeBoard.transform.position;
            spectatorCam.transform.position = boardCenter + Vector3.up * 2.5f;
            spectatorCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private GoGame _FindActiveGame()
    {
        if (board19x19 != null && board19x19.gameState == 1) return board19x19;
        if (board13x13 != null && board13x13.gameState == 1) return board13x13;
        if (board9x9   != null && board9x9.gameState   == 1) return board9x9;
        return null;
    }
}
