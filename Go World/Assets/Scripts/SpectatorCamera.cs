// SpectatorCamera.cs
// UdonSharpBehaviour that lets spectators cycle between viewing boards.
// Displays current game state (turn, score, captures) on a TextMeshPro overlay.
//
// No List<T>, no LINQ, no try/catch, no generics, no delegates.
// Local-only camera switching — no sync needed (each client controls their own view).

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SpectatorCamera : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector references
    // -----------------------------------------------------------------------
    [Header("Boards — wire all active boards here")]
    public GoGame[] boards;

    [Header("Camera to reposition for spectating")]
    [Tooltip("Camera that will be moved to view each board in turn.")]
    public Camera spectatorCam;

    [Header("Camera offsets per board — optional override")]
    [Tooltip("World-space Y offset above each board center. Default 2.5m.")]
    public float camHeightOffset = 2.5f;

    [Header("Overlay UI (TextMeshPro)")]
    public TextMeshProUGUI boardNameText;
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI capturesText;
    public TextMeshProUGUI scoreText;

    // -----------------------------------------------------------------------
    // Local state
    // -----------------------------------------------------------------------
    private int _currentBoardIndex = 0;

    // -----------------------------------------------------------------------
    // Start
    // -----------------------------------------------------------------------
    private void Start()
    {
        _SnapToBoard(_currentBoardIndex);
        _UpdateOverlay();
    }

    // -----------------------------------------------------------------------
    // Next / previous board — called by UI button Interact or SendCustomEvent
    // -----------------------------------------------------------------------
    public void NextBoard()
    {
        if (boards == null || boards.Length == 0) return;
        _currentBoardIndex = (_currentBoardIndex + 1) % boards.Length;
        _SnapToBoard(_currentBoardIndex);
        _UpdateOverlay();
    }

    public void PrevBoard()
    {
        if (boards == null || boards.Length == 0) return;
        _currentBoardIndex = (_currentBoardIndex - 1 + boards.Length) % boards.Length;
        _SnapToBoard(_currentBoardIndex);
        _UpdateOverlay();
    }

    // -----------------------------------------------------------------------
    // Update — refresh overlay each frame so it stays current
    // -----------------------------------------------------------------------
    private void Update()
    {
        _UpdateOverlay();
    }

    // -----------------------------------------------------------------------
    // Internal helpers
    // -----------------------------------------------------------------------
    private void _SnapToBoard(int index)
    {
        if (spectatorCam == null) return;
        if (boards == null || index < 0 || index >= boards.Length) return;
        GoGame board = boards[index];
        if (board == null) return;

        Vector3 pos = board.transform.position;
        spectatorCam.transform.position = new Vector3(pos.x, pos.y + camHeightOffset, pos.z);
        spectatorCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void _UpdateOverlay()
    {
        if (boards == null || boards.Length == 0) return;
        if (_currentBoardIndex < 0 || _currentBoardIndex >= boards.Length) return;

        GoGame board = boards[_currentBoardIndex];
        if (board == null) return;

        // Board name
        if (boardNameText != null)
            boardNameText.text = "Board " + (_currentBoardIndex + 1).ToString()
                + " (" + board.boardSize.ToString() + "x" + board.boardSize.ToString() + ")";

        // Turn / game state
        if (turnText != null)
        {
            switch (board.gameState)
            {
                case 0:
                    turnText.text = "Waiting for players";
                    break;
                case 1:
                    turnText.text = board.currentPlayer == 1 ? "Black to play" : "White to play";
                    break;
                case 2:
                    turnText.text = "Scoring";
                    break;
                case 3:
                    if (board.finalBlackScore > board.finalWhiteScore)
                        turnText.text = "Black wins!";
                    else if (board.finalWhiteScore > board.finalBlackScore)
                        turnText.text = "White wins!";
                    else
                        turnText.text = "Draw";
                    break;
                default:
                    turnText.text = "";
                    break;
            }
        }

        // Captures
        if (capturesText != null)
        {
            capturesText.text =
                "Captures  B:" + board.blackCaptures.ToString() +
                "  W:" + board.whiteCaptures.ToString();
        }

        // Score (only when game is finished)
        if (scoreText != null)
        {
            if (board.gameState == 3)
            {
                scoreText.text =
                    "B: " + board.finalBlackScore.ToString("F1") +
                    "  W: " + board.finalWhiteScore.ToString("F1");
            }
            else
            {
                scoreText.text = "";
            }
        }
    }
}
