// GoTutorialDisplay.cs
// Paged tutorial / help system for the Go lounge.
// Displays Go rules on a world-space canvas (TextMeshPro).
// Navigation via Next/Prev page buttons (or interact if placed on an object).
// Local-only — no network sync needed.
//
// Pages are hard-coded strings; no List<T>, no LINQ, no try/catch, no generics.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GoTutorialDisplay : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector references
    // -----------------------------------------------------------------------
    [Header("UI")]
    [Tooltip("Main body text for the current page.")]
    public TextMeshProUGUI bodyText;

    [Tooltip("Small heading / page title text.")]
    public TextMeshProUGUI titleText;

    [Tooltip("Page indicator, e.g. '2 / 5'.")]
    public TextMeshProUGUI pageIndicatorText;

    // -----------------------------------------------------------------------
    // Page data — hard-coded arrays (no List<T>)
    // -----------------------------------------------------------------------
    private readonly string[] PAGE_TITLES = new string[]
    {
        "Basic Rules",
        "Capturing Stones",
        "Ko Rule",
        "Scoring",
        "Etiquette"
    };

    private readonly string[] PAGE_BODIES = new string[]
    {
        // Page 0 — Basic Rules
        "Go is played on a grid of lines.\n\n" +
        "Black and White take turns placing stones\n" +
        "on the intersections.\n\n" +
        "Black plays first.\n\n" +
        "Stones are never moved after they are placed,\n" +
        "unless they are captured.\n\n" +
        "The goal is to surround more territory than\n" +
        "your opponent.",

        // Page 1 — Capturing
        "A stone (or group of connected stones) is\n" +
        "captured when all of its liberties are filled.\n\n" +
        "Liberties are the empty intersections\n" +
        "directly adjacent (horizontally/vertically)\n" +
        "to a stone or group.\n\n" +
        "Captured stones are removed from the board\n" +
        "and count against the captured player's score\n" +
        "(under Japanese rules; not relevant here).\n\n" +
        "You may NOT place a stone that immediately\n" +
        "has zero liberties (suicide), unless doing so\n" +
        "captures opponent stones first.",

        // Page 2 — Ko
        "Ko prevents the board from repeating the\n" +
        "same position twice in a row.\n\n" +
        "If a capture would recreate the exact board\n" +
        "position that existed before your opponent's\n" +
        "last move, that capture is illegal — it is\n" +
        "called a Ko.\n\n" +
        "To win a Ko fight, you must play elsewhere\n" +
        "first (a 'Ko threat'), forcing your opponent\n" +
        "to respond. Then you may recapture.",

        // Page 3 — Scoring
        "This world uses Chinese area scoring.\n\n" +
        "At the end of the game (after both players\n" +
        "pass consecutively), each player's score is:\n\n" +
        "  Stones on the board\n" +
        "+ Empty intersections completely surrounded\n" +
        "  by that player's stones (territory)\n\n" +
        "White receives 6.5 komi to compensate for\n" +
        "Black playing first. The 0.5 prevents draws.\n\n" +
        "The player with the higher score wins.",

        // Page 4 — Etiquette
        "Go has a strong tradition of respectful play.\n\n" +
        "- Say 'onegaishimasu' before the game begins\n" +
        "  (a polite Japanese greeting meaning\n" +
        "  'please treat me well').\n\n" +
        "- Say 'arigatou gozaimashita' after the game\n" +
        "  to thank your opponent.\n\n" +
        "- Resign gracefully when the outcome is clear\n" +
        "  rather than playing to the very end.\n\n" +
        "- Discuss the game afterward — reviewing\n" +
        "  moves together helps both players improve."
    };

    // -----------------------------------------------------------------------
    // Local state
    // -----------------------------------------------------------------------
    private int _currentPage = 0;
    private int _pageCount = 5; // must match the static arrays above

    // -----------------------------------------------------------------------
    // Start
    // -----------------------------------------------------------------------
    private void Start()
    {
        _ShowPage(_currentPage);
    }

    // -----------------------------------------------------------------------
    // Public API — wire to UI buttons or SendCustomEvent
    // -----------------------------------------------------------------------

    /// <summary>Advance to the next page (wraps around).</summary>
    public void NextPage()
    {
        _currentPage = (_currentPage + 1) % _pageCount;
        _ShowPage(_currentPage);
    }

    /// <summary>Go back to the previous page (wraps around).</summary>
    public void PrevPage()
    {
        _currentPage = (_currentPage - 1 + _pageCount) % _pageCount;
        _ShowPage(_currentPage);
    }

    /// <summary>Jump directly to a page index (0-based).</summary>
    public void GoToPage(int index)
    {
        if (index < 0 || index >= _pageCount) return;
        _currentPage = index;
        _ShowPage(_currentPage);
    }

    // Convenience one-parameter-free versions for SendCustomNetworkEvent / button wiring
    public void GoToPage0() { GoToPage(0); }
    public void GoToPage1() { GoToPage(1); }
    public void GoToPage2() { GoToPage(2); }
    public void GoToPage3() { GoToPage(3); }
    public void GoToPage4() { GoToPage(4); }

    // -----------------------------------------------------------------------
    // Interact — tap the tutorial panel to advance
    // -----------------------------------------------------------------------
    public override void Interact()
    {
        NextPage();
    }

    // -----------------------------------------------------------------------
    // Display
    // -----------------------------------------------------------------------
    private void _ShowPage(int index)
    {
        if (titleText != null)
            titleText.text = PAGE_TITLES[index];

        if (bodyText != null)
            bodyText.text = PAGE_BODIES[index];

        if (pageIndicatorText != null)
            pageIndicatorText.text = (index + 1).ToString() + " / " + _pageCount.ToString();
    }
}
