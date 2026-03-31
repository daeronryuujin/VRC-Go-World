// MoveHistoryDisplay.cs
// Tracks the last 10 moves as int arrays and displays them in standard Go
// coordinate notation (columns A-T skipping I; rows 1-19 from bottom).
//
// Move data is synced via [UdonSynced] int arrays:
//   _moveX[n], _moveY[n] : board coordinates (0-based)
//   _moveColor[n]         : 1=black, 2=white
// _moveCount tracks how many of the 10 slots are populated.
//
// GoGame calls RecordMove(x, y, color) after each successful stone placement.
// GoGame calls ClearHistory() on reset.
//
// No List<T>, no LINQ, no try/catch, no generics.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MoveHistoryDisplay : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Constants
    // -----------------------------------------------------------------------
    private const int MAX_HISTORY = 10;

    // -----------------------------------------------------------------------
    // Inspector references
    // -----------------------------------------------------------------------
    [Header("References")]
    [Tooltip("One TextMeshPro label per history slot (newest first). Assign up to 10.")]
    public TextMeshProUGUI[] moveLabels; // array of up to MAX_HISTORY labels

    // -----------------------------------------------------------------------
    // Synced state (flat int arrays, fixed size MAX_HISTORY)
    // -----------------------------------------------------------------------
    [UdonSynced] private int[] _moveX     = new int[MAX_HISTORY];
    [UdonSynced] private int[] _moveY     = new int[MAX_HISTORY];
    [UdonSynced] private int[] _moveColor = new int[MAX_HISTORY]; // 1=B, 2=W
    [UdonSynced] private int   _moveCount = 0;

    // Column letters A-T, skipping 'I' (standard Go notation)
    private readonly string COL_LETTERS = "ABCDEFGHJKLMNOPQRST";

    // -----------------------------------------------------------------------
    // Public API — called by GoGame (owner only)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Record a new move. Caller must already be the network owner of GoGame,
    /// but this behaviour's ownership is taken here.
    /// </summary>
    public void RecordMove(int x, int y, int color)
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        // Shift history down to make room at index 0 (newest first)
        int newCount = _moveCount < MAX_HISTORY ? _moveCount + 1 : MAX_HISTORY;
        for (int i = newCount - 1; i > 0; i--)
        {
            _moveX[i]     = _moveX[i - 1];
            _moveY[i]     = _moveY[i - 1];
            _moveColor[i] = _moveColor[i - 1];
        }
        _moveX[0]     = x;
        _moveY[0]     = y;
        _moveColor[0] = color;
        _moveCount    = newCount;

        RequestSerialization();
        _UpdateDisplay();
    }

    /// <summary>Clear history on board reset.</summary>
    public void ClearHistory()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _moveCount = 0;
        for (int i = 0; i < MAX_HISTORY; i++)
        {
            _moveX[i]     = 0;
            _moveY[i]     = 0;
            _moveColor[i] = 0;
        }
        RequestSerialization();
        _UpdateDisplay();
    }

    // -----------------------------------------------------------------------
    // Network sync
    // -----------------------------------------------------------------------
    public override void OnDeserialization()
    {
        _UpdateDisplay();
    }

    // -----------------------------------------------------------------------
    // Start
    // -----------------------------------------------------------------------
    private void Start()
    {
        _UpdateDisplay();
    }

    // -----------------------------------------------------------------------
    // Display update
    // -----------------------------------------------------------------------
    private void _UpdateDisplay()
    {
        if (moveLabels == null) return;

        int labelCount = moveLabels.Length;

        for (int i = 0; i < labelCount; i++)
        {
            if (moveLabels[i] == null) continue;

            if (i < _moveCount)
            {
                int x     = _moveX[i];
                int y     = _moveY[i];
                int color = _moveColor[i];

                string colorLabel = (color == 1) ? "B" : "W";
                string colLetter  = _ColLetter(x);
                int    rowNumber  = y + 1; // 1-based from bottom

                moveLabels[i].text = colorLabel + ": " + colLetter + rowNumber.ToString();
            }
            else
            {
                moveLabels[i].text = "";
            }
        }
    }

    private string _ColLetter(int x)
    {
        if (x >= 0 && x < COL_LETTERS.Length)
            return COL_LETTERS.Substring(x, 1);
        return "?";
    }
}
