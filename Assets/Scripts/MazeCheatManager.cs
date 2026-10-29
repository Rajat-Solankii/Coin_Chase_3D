using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Maze Guide Arrows Cheat.
/// Press 'M' (or 'C') during gameplay to toggle glowing guide arrows in the maze pointing directly to the win chest.
/// </summary>
public class MazeCheatManager : MonoBehaviour
{
    public static MazeCheatManager instance { get; private set; }

    [Header("Cheat Controls")]
    [Tooltip("Primary key to toggle the maze guide cheat.")]
    public KeyCode cheatKey = KeyCode.M;

    [Tooltip("Alternate key to toggle the maze guide cheat.")]
    public KeyCode alternateCheatKey = KeyCode.C;

    [Tooltip("Is the maze cheat currently active?")]
    public bool isCheatActive = false;

    [Header("Arrow Visuals")]
    public Color arrowColor = new Color(0.1f, 1f, 0.45f, 1f);
    public Color arrowGlowColor = new Color(0.2f, 1.8f, 0.6f, 1f);
    public float arrowSpacing = 1.2f;
    public float hoverHeight = 0.12f;
    public float pulseSpeed = 4f;
    public float floatAmplitude = 0.05f;

    // Internal references
    private Transform playerTransform;
    private GameObject arrowContainer;
    private List<GameObject> activeArrows = new List<GameObject>();
    private List<Vector3> baseArrowPositions = new List<Vector3>();
    private Material arrowMaterial;
    private Mesh arrowMesh;

    // Toast UI notification elements
    private GameObject toastObject;
    private Text toastText;
    private Image toastBackground;
    private CanvasGroup toastCanvasGroup;
    private Coroutine toastCoroutine;

    // Maze reference constants (17x17 grid)
    private static readonly Vector3 grid11 = new Vector3(206.7668f, 9.25f, 140.9994f);
    private static readonly Vector3 rowVec = new Vector3(-2.10447f, 0f, 1.15378f);
    private static readonly Vector3 colVec = new Vector3(1.15378f, 0f, 2.10447f);

    // Pre-calculated verified BFS optimal solution (57 cells from entrance (0,8) to chest (8,8))
    private static readonly Vector2Int[] verifiedPath = new Vector2Int[]
    {
        new Vector2Int(0,8), new Vector2Int(0,7), new Vector2Int(0,6), new Vector2Int(1,6),
        new Vector2Int(2,6), new Vector2Int(3,6), new Vector2Int(3,5), new Vector2Int(2,5),
        new Vector2Int(1,5), new Vector2Int(1,4), new Vector2Int(0,4), new Vector2Int(0,3),
        new Vector2Int(1,3), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(2,1),
        new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0), new Vector2Int(5,0),
        new Vector2Int(6,0), new Vector2Int(7,0), new Vector2Int(8,0), new Vector2Int(9,0),
        new Vector2Int(10,0), new Vector2Int(11,0), new Vector2Int(11,1), new Vector2Int(10,1),
        new Vector2Int(9,1), new Vector2Int(8,1), new Vector2Int(7,1), new Vector2Int(6,1),
        new Vector2Int(5,1), new Vector2Int(4,1), new Vector2Int(3,1), new Vector2Int(3,2),
        new Vector2Int(4,2), new Vector2Int(4,3), new Vector2Int(4,4), new Vector2Int(4,5),
        new Vector2Int(5,5), new Vector2Int(5,6), new Vector2Int(4,6), new Vector2Int(4,7),
        new Vector2Int(4,8), new Vector2Int(5,8), new Vector2Int(6,8), new Vector2Int(6,7),
        new Vector2Int(6,6), new Vector2Int(7,6), new Vector2Int(8,6), new Vector2Int(9,6),
        new Vector2Int(10,6), new Vector2Int(10,7), new Vector2Int(10,8), new Vector2Int(9,8),
        new Vector2Int(8,8)
    };

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureInitialized();
    }

    private void Start()
    {
        EnsureInitialized();
        FindPlayer();
        CreateToastUI();

        // Pre-generate full arrow trail
        GenerateArrows(verifiedPath);
    }

    private void EnsureInitialized()
    {
        if (arrowMesh == null) arrowMesh = CreateArrowMesh();
        if (arrowMaterial == null) arrowMaterial = CreateArrowMaterial();

        if (arrowContainer == null)
        {
            Transform existing = transform.Find("[MazeGuideArrows]");
            if (existing != null)
            {
                arrowContainer = existing.gameObject;
            }
            else
            {
                arrowContainer = new GameObject("[MazeGuideArrows]");
                arrowContainer.transform.SetParent(transform, true);
            }
            arrowContainer.SetActive(isCheatActive);
        }
    }

    private void Update()
    {
        // Toggle cheat on hotkey (compatible with both New Input System and Legacy)
        if (IsCheatKeyPressed())
        {
            ToggleCheat();
        }

        // Animate arrows when active
        if (isCheatActive && activeArrows.Count > 0)
        {
            AnimateArrows();
        }
    }

    private bool IsCheatKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.mKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame) return true;
        }
#endif
        try
        {
            if (Input.GetKeyDown(cheatKey) || Input.GetKeyDown(alternateCheatKey)) return true;
        }
        catch { }
        return false;
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null)
        {
            var cc = FindObjectOfType<CharacterController>();
            if (cc != null) p = cc.gameObject;
        }
        if (p != null) playerTransform = p.transform;
    }

    /// <summary>
    /// Toggles the cheat on or off.
    /// </summary>
    public void ToggleCheat()
    {
        SetCheatEnabled(!isCheatActive);
    }

    /// <summary>
    /// Explicitly sets the cheat state.
    /// </summary>
    public void SetCheatEnabled(bool enabled)
    {
        EnsureInitialized();
        isCheatActive = enabled;

        if (arrowContainer != null)
        {
            arrowContainer.SetActive(isCheatActive);
        }

        // If player is inside the maze and wants dynamic guidance, recalculate
        if (isCheatActive)
        {
            UpdatePathToGoal();
            ShowToast("<color=#30FF60><b>★ MAZE CHEAT ACTIVATED ★</b></color>\nFollow the glowing green arrows to victory!", new Color(0.1f, 0.5f, 0.2f, 0.85f));
            PlayAudioChirp(true);
        }
        else
        {
            ShowToast("<color=#FF9040><b>MAZE CHEAT DEACTIVATED</b></color>\nGuide arrows hidden.", new Color(0.4f, 0.2f, 0.1f, 0.85f));
            PlayAudioChirp(false);
        }

        // Update UI if present
        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateCheatButtonUI(isCheatActive);
        }
    }

    /// <summary>
    /// Calculates the path from the player's current location (or maze entrance) to the chest.
    /// </summary>
    public void UpdatePathToGoal()
    {
        EnsureInitialized();
        if (playerTransform == null) FindPlayer();

        Vector2Int startCell = new Vector2Int(0, 8); // Default to entrance
        bool useDynamic = false;

        if (playerTransform != null)
        {
            Vector2Int currentCell = WorldToCell(playerTransform.position);
            // If the player is within the maze boundary
            if (currentCell.x >= 0 && currentCell.x <= 16 && currentCell.y >= 0 && currentCell.y <= 16)
            {
                startCell = currentCell;
                useDynamic = true;
            }
        }

        List<Vector2Int> path;
        if (useDynamic)
        {
            path = SolveMazeDynamic(startCell, new Vector2Int(8, 8));
            if (path == null || path.Count == 0)
            {
                path = new List<Vector2Int>(verifiedPath);
            }
        }
        else
        {
            path = new List<Vector2Int>(verifiedPath);
        }

        GenerateArrows(path.ToArray());
    }

    private void GenerateArrows(Vector2Int[] path)
    {
        EnsureInitialized();

        // Clear previous arrows
        foreach (var a in activeArrows)
        {
            if (a != null) Destroy(a);
        }
        activeArrows.Clear();
        baseArrowPositions.Clear();

        if (path == null || path.Length < 2) return;

        // Generate arrows along each segment
        for (int i = 0; i < path.Length - 1; i++)
        {
            Vector3 from = CellToWorld(path[i]);
            Vector3 to = CellToWorld(path[i + 1]);
            Vector3 dir = (to - from).normalized;

            if (dir == Vector3.zero) dir = Vector3.forward;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            // Place 2 arrows per grid segment for a dense runway trail
            for (float t = 0.25f; t <= 0.75f; t += 0.5f)
            {
                Vector3 pos = Vector3.Lerp(from, to, t);
                pos = SnapToGround(pos);

                GameObject arrow = new GameObject($"GuideArrow_{i}_{t:F1}");
                arrow.transform.SetParent(arrowContainer.transform, true);
                arrow.transform.position = pos;
                arrow.transform.rotation = rot;
                arrow.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

                MeshFilter mf = arrow.AddComponent<MeshFilter>();
                mf.sharedMesh = arrowMesh;

                MeshRenderer mr = arrow.AddComponent<MeshRenderer>();
                mr.sharedMaterial = arrowMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                activeArrows.Add(arrow);
                baseArrowPositions.Add(pos);
            }
        }

        // Add a final beacon arrow right on the treasure dais pointing into the chest
        Vector3 chestCellPos = CellToWorld(path[path.Length - 1]);
        Vector3 finalPos = SnapToGround(chestCellPos + new Vector3(0f, 0.2f, 0f));

        GameObject finalBeacon = new GameObject("GuideArrow_FinalGoal");
        finalBeacon.transform.SetParent(arrowContainer.transform, true);
        finalBeacon.transform.position = finalPos;
        finalBeacon.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        finalBeacon.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);

        MeshFilter fmf = finalBeacon.AddComponent<MeshFilter>();
        fmf.sharedMesh = arrowMesh;
        MeshRenderer fmr = finalBeacon.AddComponent<MeshRenderer>();
        fmr.sharedMaterial = arrowMaterial;

        activeArrows.Add(finalBeacon);
        baseArrowPositions.Add(finalPos);
    }

    private void AnimateArrows()
    {
        float t = Time.time;
        for (int i = 0; i < activeArrows.Count; i++)
        {
            GameObject a = activeArrows[i];
            if (a == null) continue;

            // Sequential light wave & floating animation
            float wave = Mathf.Sin(t * pulseSpeed - i * 0.25f);
            float yOffset = wave * floatAmplitude;
            a.transform.position = baseArrowPositions[i] + Vector3.up * (hoverHeight + yOffset);
        }
    }

    private Vector3 SnapToGround(Vector3 worldPos)
    {
        RaycastHit hit;
        // Cast down to find the maze floor
        if (Physics.Raycast(worldPos + Vector3.up * 4f, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore))
        {
            return new Vector3(worldPos.x, hit.point.y + hoverHeight, worldPos.z);
        }
        return new Vector3(worldPos.x, 9.35f, worldPos.z);
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return grid11 + (cell.x - 1) * rowVec + (cell.y - 1) * colVec;
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 delta = worldPos - grid11;
        float rDelta = Vector3.Dot(delta, rowVec) / rowVec.sqrMagnitude;
        float cDelta = Vector3.Dot(delta, colVec) / colVec.sqrMagnitude;

        int r = Mathf.RoundToInt(1f + rDelta);
        int c = Mathf.RoundToInt(1f + cDelta);
        return new Vector2Int(r, c);
    }

    /// <summary>
    /// Dynamic BFS maze solver taking into account the actual walls in the scene.
    /// </summary>
    private List<Vector2Int> SolveMazeDynamic(Vector2Int start, Vector2Int goal)
    {
        GameObject maze = GameObject.Find("Level3_Maze");
        Transform walls = maze != null ? maze.transform.Find("Walls") : null;

        bool[,] hBlocked = new bool[16, 17];
        bool[,] vBlocked = new bool[17, 16];

        if (walls != null)
        {
            for (int i = 0; i < walls.childCount; i++)
            {
                string n = walls.GetChild(i).name;
                if (n.StartsWith("HWalls_"))
                {
                    string[] parts = n.Substring(7).Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int r) && int.TryParse(parts[1], out int c))
                    {
                        if (r >= 0 && r < 16 && c >= 0 && c < 17) hBlocked[r, c] = true;
                    }
                }
                else if (n.StartsWith("VWalls_"))
                {
                    string[] parts = n.Substring(7).Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int r) && int.TryParse(parts[1], out int c))
                    {
                        if (r >= 0 && r < 17 && c >= 0 && c < 16) vBlocked[r, c] = true;
                    }
                }
            }
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        bool reached = false;
        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            if (curr == goal)
            {
                reached = true;
                break;
            }

            int r = curr.x;
            int c = curr.y;

            // Up (r - 1)
            if (r > 0 && !hBlocked[r - 1, c])
            {
                Vector2Int nxt = new Vector2Int(r - 1, c);
                if (!visited.Contains(nxt)) { visited.Add(nxt); parent[nxt] = curr; queue.Enqueue(nxt); }
            }
            // Down (r + 1)
            if (r < 16 && !hBlocked[r, c])
            {
                Vector2Int nxt = new Vector2Int(r + 1, c);
                if (!visited.Contains(nxt)) { visited.Add(nxt); parent[nxt] = curr; queue.Enqueue(nxt); }
            }
            // Left (c - 1)
            if (c > 0 && !vBlocked[r, c - 1])
            {
                Vector2Int nxt = new Vector2Int(r, c - 1);
                if (!visited.Contains(nxt)) { visited.Add(nxt); parent[nxt] = curr; queue.Enqueue(nxt); }
            }
            // Right (c + 1)
            if (c < 16 && !vBlocked[r, c])
            {
                Vector2Int nxt = new Vector2Int(r, c + 1);
                if (!visited.Contains(nxt)) { visited.Add(nxt); parent[nxt] = curr; queue.Enqueue(nxt); }
            }
        }

        if (!reached) return null;

        List<Vector2Int> res = new List<Vector2Int>();
        Vector2Int trace = goal;
        while (trace != start)
        {
            res.Add(trace);
            trace = parent[trace];
        }
        res.Add(start);
        res.Reverse();
        return res;
    }

    /// <summary>
    /// Builds a sleek 3D chevron arrow mesh pointing along +Z.
    /// </summary>
    private Mesh CreateArrowMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralChevronArrow";

        // Vertices for top and bottom faces (pointing in +Z)
        Vector3[] top = new Vector3[]
        {
            new Vector3(-0.16f, 0.05f, -0.45f),
            new Vector3( 0.16f, 0.05f, -0.45f),
            new Vector3( 0.16f, 0.05f,  0.10f),
            new Vector3( 0.45f, 0.05f,  0.10f),
            new Vector3( 0.00f, 0.05f,  0.65f),
            new Vector3(-0.45f, 0.05f,  0.10f),
            new Vector3(-0.16f, 0.05f,  0.10f)
        };

        Vector3[] bottom = new Vector3[7];
        for (int i = 0; i < 7; i++)
        {
            bottom[i] = new Vector3(top[i].x, -0.05f, top[i].z);
        }

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        // Top face
        int topStart = verts.Count;
        verts.AddRange(top);
        tris.Add(topStart + 0); tris.Add(topStart + 2); tris.Add(topStart + 1);
        tris.Add(topStart + 0); tris.Add(topStart + 6); tris.Add(topStart + 2);
        tris.Add(topStart + 5); tris.Add(topStart + 4); tris.Add(topStart + 3);

        // Bottom face
        int botStart = verts.Count;
        verts.AddRange(bottom);
        tris.Add(botStart + 0); tris.Add(botStart + 1); tris.Add(botStart + 2);
        tris.Add(botStart + 0); tris.Add(botStart + 2); tris.Add(botStart + 6);
        tris.Add(botStart + 5); tris.Add(botStart + 3); tris.Add(botStart + 4);

        // Side walls
        for (int i = 0; i < 7; i++)
        {
            int next = (i + 1) % 7;
            int s = verts.Count;

            verts.Add(top[i]);
            verts.Add(top[next]);
            verts.Add(bottom[next]);
            verts.Add(bottom[i]);

            tris.Add(s + 0); tris.Add(s + 1); tris.Add(s + 2);
            tris.Add(s + 0); tris.Add(s + 2); tris.Add(s + 3);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Creates a vibrant emissive glowing green material compatible with URP or Standard shaders.
    /// </summary>
    private Material CreateArrowMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.color = arrowColor;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", arrowColor);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", arrowGlowColor * 2.2f);
        }

        return mat;
    }

    /// <summary>
    /// Creates the on-screen toast UI overlay to notify the player when the cheat is toggled.
    /// </summary>
    private void CreateToastUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Transform existingToast = canvas.transform.Find("MazeCheatToast");
        if (existingToast != null)
        {
            toastObject = existingToast.gameObject;
            toastText = toastObject.GetComponentInChildren<Text>();
            toastBackground = toastObject.GetComponent<Image>();
            toastCanvasGroup = toastObject.GetComponent<CanvasGroup>();
            return;
        }

        toastObject = new GameObject("MazeCheatToast");
        toastObject.transform.SetParent(canvas.transform, false);

        RectTransform rt = toastObject.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.85f);
        rt.anchorMax = new Vector2(0.5f, 0.85f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 75f);
        rt.anchoredPosition = Vector2.zero;

        toastBackground = toastObject.AddComponent<Image>();
        toastBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);

        toastCanvasGroup = toastObject.AddComponent<CanvasGroup>();
        toastCanvasGroup.alpha = 0f;
        toastCanvasGroup.blocksRaycasts = false;

        GameObject textGO = new GameObject("ToastText");
        textGO.transform.SetParent(toastObject.transform, false);

        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        toastText = textGO.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        toastText.font = font;
        toastText.fontSize = 20;
        toastText.alignment = TextAnchor.MiddleCenter;
        toastText.color = Color.white;
        toastText.supportRichText = true;

        toastObject.SetActive(false);
    }

    private void ShowToast(string message, Color bgColor)
    {
        if (toastObject == null) CreateToastUI();
        if (toastObject == null) return;

        if (toastText != null) toastText.text = message;
        if (toastBackground != null) toastBackground.color = bgColor;
        toastObject.SetActive(true);

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastFadeRoutine());
    }

    private IEnumerator ToastFadeRoutine()
    {
        if (toastCanvasGroup == null) yield break;

        // Fade in
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (toastCanvasGroup != null) toastCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.2f);
            yield return null;
        }
        if (toastCanvasGroup != null) toastCanvasGroup.alpha = 1f;

        // Display hold
        yield return new WaitForSecondsRealtime(2.2f);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (toastCanvasGroup != null) toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            yield return null;
        }
        if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
        if (toastObject != null) toastObject.SetActive(false);
    }

    private void PlayAudioChirp(bool on)
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // If audio clip exists on collectable counter or coin, play pitch-shifted chirp
        if (CollectableCounter.instance != null && CollectableCounter.instance.collectionSound != null)
        {
            audioSource.pitch = on ? 1.4f : 0.8f;
            audioSource.PlayOneShot(CollectableCounter.instance.collectionSound, 0.7f);
        }
    }
}
