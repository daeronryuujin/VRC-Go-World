// GoResetButton.cs
// Interact button that resets the Go game. Any seated player or the instance
// master can trigger a reset.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GoResetButton : UdonSharpBehaviour
{
    [Header("References")]
    public GoGame goGame;

    [Tooltip("If true, only the instance master can reset. If false, seated players also can.")]
    public bool masterOnlyReset = false;

    public override void Interact()
    {
        if (goGame == null) return;

        VRCPlayerApi local = Networking.LocalPlayer;
        if (local == null) return;

        // Permission check
        bool isMaster = local.isMaster;
        bool isSeated = (local.playerId == goGame.blackPlayerId ||
                         local.playerId == goGame.whitePlayerId);

        if (!isMaster && !isSeated) return;
        if (masterOnlyReset && !isMaster) return;

        goGame.ResetGame();
    }
}
