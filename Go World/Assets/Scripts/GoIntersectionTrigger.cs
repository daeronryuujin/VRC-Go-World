// GoIntersectionTrigger.cs
// One of these sits on each intersection point of a Go board.
// When the local player interacts with it (VRC Interact), it calls TryPlaceStoneAt
// on the parent GoGame with its board-space coordinates.
//
// Setup: place a small sphere collider + this script at each grid intersection.
// Set boardX / boardY in the inspector (or via the Editor setup tool).

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GoIntersectionTrigger : UdonSharpBehaviour
{
    [Header("Board Coordinates")]
    [Tooltip("Column (0-based from left)")]
    public int boardX = 0;

    [Tooltip("Row (0-based from bottom/near side)")]
    public int boardY = 0;

    [Header("References")]
    public GoGame goGame;

    // -----------------------------------------------------------------------
    // VRC Interact — fired when local player clicks/grabs this collider
    // -----------------------------------------------------------------------
    public override void Interact()
    {
        if (goGame == null) return;
        goGame.TryPlaceStoneAt(boardX, boardY);
    }
}
