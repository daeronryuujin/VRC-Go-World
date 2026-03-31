// GoPassButton.cs
// Interact button for the seated player to pass their turn.
// Placed near each seat (one for black side, one for white side).

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GoPassButton : UdonSharpBehaviour
{
    [Header("References")]
    public GoGame goGame;

    public override void Interact()
    {
        if (goGame == null) return;
        goGame.LocalPlayerPass();
    }
}
