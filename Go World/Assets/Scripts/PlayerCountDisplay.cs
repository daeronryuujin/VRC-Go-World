// PlayerCountDisplay.cs
// Displays the current number of players in the instance on a TextMeshPro label.
// Updates when players join or leave via OnPlayerJoined / OnPlayerLeft.
//
// No List<T>, no LINQ, no try/catch, no generics.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class PlayerCountDisplay : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector references
    // -----------------------------------------------------------------------
    [Header("UI")]
    public TextMeshProUGUI playerCountText;

    [Tooltip("Label prefix, e.g. 'Players: '")]
    public string prefix = "Players: ";

    // -----------------------------------------------------------------------
    // Start
    // -----------------------------------------------------------------------
    private void Start()
    {
        _UpdateDisplay();
    }

    // -----------------------------------------------------------------------
    // VRC player events
    // -----------------------------------------------------------------------
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        _UpdateDisplay();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        _UpdateDisplay();
    }

    // -----------------------------------------------------------------------
    // Display
    // -----------------------------------------------------------------------
    private void _UpdateDisplay()
    {
        if (playerCountText == null) return;

        // VRCPlayerApi.GetPlayerCount() returns the number of players in the instance.
        int count = VRCPlayerApi.GetPlayerCount();
        playerCountText.text = prefix + count.ToString();
    }
}
