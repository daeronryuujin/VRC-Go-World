// GoSeatDetector.cs
// Trigger collider component that detects when the local player enters/exits
// the seat zone for black or white, and notifies GoGame accordingly.
//
// Place this on a GameObject with a trigger collider covering the seat area.
// The GoGame callbacks use direct method calls (same-client only) — no network
// events needed here because we're only notifying the game about the LOCAL player.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GoSeatDetector : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Configuration
    // -----------------------------------------------------------------------
    [Header("Configuration")]
    [Tooltip("The GoGame this seat belongs to.")]
    public GoGame goGame;

    [Tooltip("1 = black seat, 2 = white seat")]
    public int seatColor = 1;

    // -----------------------------------------------------------------------
    // Trigger events — only fire for local player; other players are ignored.
    // -----------------------------------------------------------------------
    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        if (goGame == null) return;

        if (seatColor == 1)
            goGame.OnLocalPlayerSitBlack();
        else
            goGame.OnLocalPlayerSitWhite();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        if (goGame == null) return;

        if (seatColor == 1)
            goGame.OnLocalPlayerLeaveBlack();
        else
            goGame.OnLocalPlayerLeaveWhite();
    }
}
