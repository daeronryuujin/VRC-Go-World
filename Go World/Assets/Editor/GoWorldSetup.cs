// GoWorldSetup.cs — EDITOR ONLY
// Menu: Go World → Setup Board
//
// Automates the tedious work of:
//   1. Creating the grid of intersection trigger GameObjects for a board
//   2. Assigning boardX/boardY on each GoIntersectionTrigger
//   3. Creating pre-placed stone GameObjects at each intersection
//   4. Assigning all stone objects to GoBoardVisualizer.stoneObjects[]
//   5. Placing hoshi (star point) dot meshes on the board surface
//
// Select the board root GameObject (e.g. "Board_19x19") before running.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using UdonSharpEditor;

public class GoWorldSetup : EditorWindow
{
    // -----------------------------------------------------------------------
    // Window state
    // -----------------------------------------------------------------------
    private int _boardSize = 19;
    private float _boardWidth = 0.76f;   // physical board width in metres (19x19 standard)
    private float _boardHeight = 0.76f;  // physical board depth in metres
    private float _stoneRadius = 0.011f; // stone radius in metres
    private float _stoneThickness = 0.009f; // half-height of stone (flattened sphere Y)

    private GameObject _boardRoot;
    private GameObject _blackStonePrefab;
    private GameObject _whiteStonePrefab;
    private Material _blackMaterial;
    private Material _whiteMaterial;
    private Material _hoshiMaterial;

    // -----------------------------------------------------------------------
    // Menu entry
    // -----------------------------------------------------------------------
    [MenuItem("Go World/Setup Board Intersections")]
    public static void ShowWindow()
    {
        GetWindow<GoWorldSetup>("Go Board Setup");
    }

    // -----------------------------------------------------------------------
    // GUI
    // -----------------------------------------------------------------------
    private void OnGUI()
    {
        GUILayout.Label("Go Board Intersection Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _boardRoot = (GameObject)EditorGUILayout.ObjectField(
            "Board Root GameObject", _boardRoot, typeof(GameObject), true);

        _boardSize = EditorGUILayout.IntSlider("Board Size", _boardSize, 9, 19);
        if (_boardSize != 9 && _boardSize != 13 && _boardSize != 19)
        {
            EditorGUILayout.HelpBox("Board size should be 9, 13, or 19.", MessageType.Warning);
        }

        _boardWidth  = EditorGUILayout.FloatField("Board Width (m)",  _boardWidth);
        _boardHeight = EditorGUILayout.FloatField("Board Height (m)", _boardHeight);
        _stoneRadius = EditorGUILayout.FloatField("Stone Radius (m)", _stoneRadius);
        _stoneThickness = EditorGUILayout.FloatField("Stone Thickness (m)", _stoneThickness);

        EditorGUILayout.Space();
        GUILayout.Label("Materials (optional — auto-assigns to stones)", EditorStyles.miniLabel);
        _blackMaterial = (Material)EditorGUILayout.ObjectField("Black Stone Material", _blackMaterial, typeof(Material), false);
        _whiteMaterial = (Material)EditorGUILayout.ObjectField("White Stone Material", _whiteMaterial, typeof(Material), false);
        _hoshiMaterial = (Material)EditorGUILayout.ObjectField("Hoshi Dot Material",   _hoshiMaterial, typeof(Material), false);

        EditorGUILayout.Space();

        bool ready = _boardRoot != null;
        EditorGUI.BeginDisabledGroup(!ready);

        if (GUILayout.Button("1. Create Intersection Triggers"))
            _CreateIntersectionTriggers();

        if (GUILayout.Button("2. Create Pre-placed Stones"))
            _CreatePreplacedStones();

        if (GUILayout.Button("3. Place Hoshi Dots"))
            _PlaceHoshi();

        if (GUILayout.Button("Run All Steps (1+2+3)"))
        {
            _CreateIntersectionTriggers();
            _CreatePreplacedStones();
            _PlaceHoshi();
        }

        EditorGUI.EndDisabledGroup();

        if (!ready)
            EditorGUILayout.HelpBox("Select a Board Root GameObject first.", MessageType.Info);
    }

    // -----------------------------------------------------------------------
    // Step 1 — Intersection Triggers
    // -----------------------------------------------------------------------
    private void _CreateIntersectionTriggers()
    {
        if (_boardRoot == null) return;

        // Find or create parent container
        Transform triggerParent = _boardRoot.transform.Find("IntersectionTriggers");
        if (triggerParent != null)
        {
            if (!EditorUtility.DisplayDialog("Confirm",
                "IntersectionTriggers already exists. Replace?", "Yes", "Cancel"))
                return;
            DestroyImmediate(triggerParent.gameObject);
        }

        GameObject container = new GameObject("IntersectionTriggers");
        container.transform.SetParent(_boardRoot.transform, false);
        triggerParent = container.transform;

        // Find GoGame on board root (or children)
        GoGame goGame = _boardRoot.GetComponentInChildren<GoGame>();

        float spacingX = _boardWidth  / (_boardSize - 1);
        float spacingZ = _boardHeight / (_boardSize - 1);
        float originX  = -_boardWidth  * 0.5f;
        float originZ  = -_boardHeight * 0.5f;
        float interactY = 0.005f; // just above board surface

        for (int y = 0; y < _boardSize; y++)
        {
            for (int x = 0; x < _boardSize; x++)
            {
                string name = string.Format("Intersection_{0:00}_{1:00}", x, y);
                GameObject obj = new GameObject(name);
                obj.transform.SetParent(triggerParent, false);
                obj.transform.localPosition = new Vector3(
                    originX + x * spacingX,
                    interactY,
                    originZ + y * spacingZ);

                // Sphere collider — trigger
                SphereCollider col = obj.AddComponent<SphereCollider>();
                col.radius = Mathf.Min(spacingX, spacingZ) * 0.45f;
                col.isTrigger = true;

                // UdonSharp behaviour for GoIntersectionTrigger
                // We add an UdonBehaviour and let UdonSharp handle the proxy
                var udon = UdonSharpEditorUtility.AddUdonSharpComponent<GoIntersectionTrigger>(obj);
                udon.boardX = x;
                udon.boardY = y;
                if (goGame != null) udon.goGame = goGame;

                Undo.RegisterCreatedObjectUndo(obj, "Create Intersection");
            }
        }

        EditorUtility.SetDirty(_boardRoot);
        Debug.Log(string.Format("[GoWorldSetup] Created {0} intersection triggers for {1}x{1} board.",
            _boardSize * _boardSize, _boardSize));
    }

    // -----------------------------------------------------------------------
    // Step 2 — Pre-placed Stones
    // -----------------------------------------------------------------------
    private void _CreatePreplacedStones()
    {
        if (_boardRoot == null) return;

        Transform stonesParent = _boardRoot.transform.Find("Stones");
        if (stonesParent != null)
        {
            if (!EditorUtility.DisplayDialog("Confirm",
                "Stones container already exists. Replace?", "Yes", "Cancel"))
                return;
            DestroyImmediate(stonesParent.gameObject);
        }

        GameObject container = new GameObject("Stones");
        container.transform.SetParent(_boardRoot.transform, false);
        stonesParent = container.transform;

        int total = _boardSize * _boardSize;
        GameObject[] stoneObjs = new GameObject[total];

        float spacingX = _boardWidth  / (_boardSize - 1);
        float spacingZ = _boardHeight / (_boardSize - 1);
        float originX  = -_boardWidth  * 0.5f;
        float originZ  = -_boardHeight * 0.5f;
        float stoneY   = _stoneThickness; // sit on board surface

        for (int y = 0; y < _boardSize; y++)
        {
            for (int x = 0; x < _boardSize; x++)
            {
                int idx = y * _boardSize + x;
                string name = string.Format("Stone_{0:00}_{1:00}", x, y);

                GameObject stone;
                if (_blackStonePrefab != null)
                {
                    stone = (GameObject)PrefabUtility.InstantiatePrefab(_blackStonePrefab, stonesParent);
                    stone.name = name;
                }
                else
                {
                    stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    stone.name = name;
                    stone.transform.SetParent(stonesParent, false);
                }

                // Remove auto-added collider from primitive (triggers handle interaction)
                Collider stoneCol = stone.GetComponent<Collider>();
                if (stoneCol != null) DestroyImmediate(stoneCol);

                stone.transform.localPosition = new Vector3(
                    originX + x * spacingX,
                    stoneY,
                    originZ + y * spacingZ);

                stone.transform.localScale = new Vector3(
                    _stoneRadius * 2f,
                    _stoneThickness,
                    _stoneRadius * 2f);

                // Assign black material by default
                Renderer r = stone.GetComponent<Renderer>();
                if (r != null && _blackMaterial != null)
                    r.sharedMaterial = _blackMaterial;

                // Start disabled
                stone.SetActive(false);

                stoneObjs[idx] = stone;
                Undo.RegisterCreatedObjectUndo(stone, "Create Stone");
            }
        }

        // Wire up GoBoardVisualizer
        GoBoardVisualizer vis = _boardRoot.GetComponentInChildren<GoBoardVisualizer>();
        if (vis != null)
        {
            vis.stoneObjects = stoneObjs;
            if (_blackMaterial != null) vis.blackStoneMaterial = _blackMaterial;
            if (_whiteMaterial != null) vis.whiteStoneMaterial = _whiteMaterial;
            EditorUtility.SetDirty(vis);
        }
        else
        {
            Debug.LogWarning("[GoWorldSetup] No GoBoardVisualizer found on board — stones created but not wired. Add GoBoardVisualizer to the board root.");
        }

        EditorUtility.SetDirty(_boardRoot);
        Debug.Log(string.Format("[GoWorldSetup] Created {0} stone objects.", total));
    }

    // -----------------------------------------------------------------------
    // Step 3 — Hoshi dots
    // -----------------------------------------------------------------------
    private static readonly int[] HOSHI_9  = { 2,2, 6,2, 2,6, 6,6, 4,4 };
    private static readonly int[] HOSHI_13 = { 3,3, 9,3, 3,9, 9,9, 6,6, 3,6, 6,3, 6,9, 9,6 };
    private static readonly int[] HOSHI_19 = { 3,3, 9,3, 15,3, 3,9, 9,9, 15,9, 3,15, 9,15, 15,15 };

    private void _PlaceHoshi()
    {
        if (_boardRoot == null) return;

        int[] hoshi;
        if (_boardSize == 9)       hoshi = HOSHI_9;
        else if (_boardSize == 13) hoshi = HOSHI_13;
        else if (_boardSize == 19) hoshi = HOSHI_19;
        else { Debug.LogWarning("[GoWorldSetup] No hoshi data for board size " + _boardSize); return; }

        Transform hoshiParent = _boardRoot.transform.Find("HoshiDots");
        if (hoshiParent != null) DestroyImmediate(hoshiParent.gameObject);

        GameObject container = new GameObject("HoshiDots");
        container.transform.SetParent(_boardRoot.transform, false);
        hoshiParent = container.transform;

        float spacingX = _boardWidth  / (_boardSize - 1);
        float spacingZ = _boardHeight / (_boardSize - 1);
        float originX  = -_boardWidth  * 0.5f;
        float originZ  = -_boardHeight * 0.5f;
        float dotY     = 0.001f; // just above board surface, below stones

        float dotRadius = Mathf.Min(spacingX, spacingZ) * 0.18f;

        for (int i = 0; i < hoshi.Length; i += 2)
        {
            int hx = hoshi[i];
            int hy = hoshi[i + 1];

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dot.name = string.Format("Hoshi_{0}_{1}", hx, hy);
            dot.transform.SetParent(hoshiParent, false);
            dot.transform.localPosition = new Vector3(
                originX + hx * spacingX,
                dotY,
                originZ + hy * spacingZ);
            dot.transform.localScale = new Vector3(dotRadius * 2f, 0.001f, dotRadius * 2f);

            Collider c = dot.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);

            Renderer r = dot.GetComponent<Renderer>();
            if (r != null && _hoshiMaterial != null)
                r.sharedMaterial = _hoshiMaterial;

            Undo.RegisterCreatedObjectUndo(dot, "Create Hoshi");
        }

        EditorUtility.SetDirty(_boardRoot);
        Debug.Log(string.Format("[GoWorldSetup] Placed {0} hoshi dots.", hoshi.Length / 2));
    }
}
#endif
