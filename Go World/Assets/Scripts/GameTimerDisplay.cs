// GameTimerDisplay.cs
// Shows elapsed time since the current game started.
// Resets when the board is reset (gameState goes back to 0 or 1 from 3).
// Elapsed time is synced via [UdonSynced] so all clients see the same clock.
//
// The owner writes startTimeUtc (Unix-like float in seconds from Networking.GetServerTimeInSeconds)
// when a game starts, and broadcasts via RequestSerialization.
// All clients compute elapsed = serverNow - startTimeUtc and display it.
//
// No List<T>, no LINQ, no try/catch, no generics.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GameTimerDisplay : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector references
    // -----------------------------------------------------------------------
    [Header("References")]
    [Tooltip("The GoGame this timer tracks.")]
    public GoGame goGame;

    [Tooltip("TextMeshPro label to display elapsed time.")]
    public TextMeshProUGUI timerText;

    // -----------------------------------------------------------------------
    // Synced state
    // -----------------------------------------------------------------------
    [UdonSynced] private double _gameStartServerTime = 0.0;
    [UdonSynced] private bool _timerRunning = false;
    [UdonSynced] private double _finalElapsed = 0.0; // locked in when game ends

    // -----------------------------------------------------------------------
    // Local state
    // -----------------------------------------------------------------------
    private int _lastObservedGameState = -1;

    // -----------------------------------------------------------------------
    // Update — called every frame; only owner tracks transitions
    // -----------------------------------------------------------------------
    private void Update()
    {
        if (goGame == null) return;

        int gs = goGame.gameState;

        if (Networking.IsOwner(gameObject))
        {
            // Game just moved into playing state
            if (gs == 1 && _lastObservedGameState != 1)
            {
                _gameStartServerTime = Networking.GetServerTimeInSeconds();
                _timerRunning = true;
                _finalElapsed = 0.0;
                RequestSerialization();
            }
            // Game ended or reset
            else if ((gs == 3 || gs == 0) && _timerRunning)
            {
                if (gs == 3)
                    _finalElapsed = Networking.GetServerTimeInSeconds() - _gameStartServerTime;
                _timerRunning = false;
                RequestSerialization();
            }
        }

        _lastObservedGameState = gs;

        _UpdateDisplay();
    }

    // -----------------------------------------------------------------------
    // OnDeserialization — remote clients update display from synced data
    // -----------------------------------------------------------------------
    public override void OnDeserialization()
    {
        _UpdateDisplay();
    }

    // -----------------------------------------------------------------------
    // Display helper
    // -----------------------------------------------------------------------
    private void _UpdateDisplay()
    {
        if (timerText == null) return;

        double elapsed;
        if (_timerRunning)
        {
            elapsed = Networking.GetServerTimeInSeconds() - _gameStartServerTime;
            if (elapsed < 0.0) elapsed = 0.0;
        }
        else
        {
            elapsed = _finalElapsed;
        }

        int totalSeconds = (int)elapsed;
        int hours   = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        if (hours > 0)
            timerText.text = hours.ToString() + ":" +
                             _TwoDigit(minutes) + ":" +
                             _TwoDigit(seconds);
        else
            timerText.text = _TwoDigit(minutes) + ":" + _TwoDigit(seconds);
    }

    private string _TwoDigit(int n)
    {
        if (n < 10) return "0" + n.ToString();
        return n.ToString();
    }
}
