// GoBoardVisualizer.cs
// Manages the pre-placed stone GameObjects at every intersection.
// Stone objects are created at build time (one per intersection, all disabled),
// then enabled/colored by this script when board state changes.
//
// No generics, no List<T>, no LINQ, no try/catch.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GoBoardVisualizer : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector references
    // -----------------------------------------------------------------------
    [Header("Stone GameObjects")]
    [Tooltip("Array of pre-placed stone GameObjects, one per intersection, in row-major order (y*size+x).")]
    public GameObject[] stoneObjects;

    [Header("Materials")]
    public Material blackStoneMaterial;
    public Material whiteStoneMaterial;

    [Header("Last Move Indicator")]
    [Tooltip("Optional: small ring/circle renderer to highlight the last played stone.")]
    public GameObject lastMoveIndicator;

    // -----------------------------------------------------------------------
    // Internal state
    // -----------------------------------------------------------------------
    private int _cachedBoardSize = 0;
    private int[] _lastBoardState;

    // -----------------------------------------------------------------------
    // Main refresh — called by GoGame on deserialization or local move
    // -----------------------------------------------------------------------
    /// <summary>
    /// Updates stone visibility and materials to match boardState.
    /// boardSize: 9, 13, or 19.
    /// lastMoveIdx: linear index of last placed stone, or -1.
    /// </summary>
    public void Refresh(int[] boardState, int boardSize, int lastMoveIdx)
    {
        if (boardState == null) return;
        if (stoneObjects == null) return;

        int total = boardSize * boardSize;
        if (stoneObjects.Length < total) return;

        _cachedBoardSize = boardSize;

        for (int i = 0; i < total; i++)
        {
            GameObject stone = stoneObjects[i];
            if (stone == null) continue;

            int cell = boardState[i];
            if (cell == 0)
            {
                stone.SetActive(false);
            }
            else
            {
                stone.SetActive(true);
                Renderer r = stone.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = (cell == 1) ? blackStoneMaterial : whiteStoneMaterial;
                }
            }
        }

        // Position last-move indicator
        if (lastMoveIndicator != null)
        {
            if (lastMoveIdx >= 0 && lastMoveIdx < total && boardState[lastMoveIdx] != 0)
            {
                lastMoveIndicator.SetActive(true);
                lastMoveIndicator.transform.position = stoneObjects[lastMoveIdx].transform.position;
            }
            else
            {
                lastMoveIndicator.SetActive(false);
            }
        }
    }

    /// <summary>Hides all stones and the last-move indicator.</summary>
    public void ClearAll(int boardSize)
    {
        int total = boardSize * boardSize;
        if (stoneObjects == null) return;
        for (int i = 0; i < total && i < stoneObjects.Length; i++)
        {
            if (stoneObjects[i] != null)
                stoneObjects[i].SetActive(false);
        }
        if (lastMoveIndicator != null)
            lastMoveIndicator.SetActive(false);
    }
}
