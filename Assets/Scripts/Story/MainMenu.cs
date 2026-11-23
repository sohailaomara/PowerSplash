using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Starting menu scene. START GAME begins the story at Level 1; the level buttons let the player open any level
/// in narrative order (the story up to that level is assumed, see StoryStateManager); QUIT exits.
/// The look follows the game: deep blue water, light shafts, rising bubbles, swaying kelp and the real fish models
/// cruising in the background. The UI is built at runtime, like the in-game HUD.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Background fish (the game's own models, swimming behind the menu)")]
    [SerializeField] private GameObject[] fishPrefabs;
    [SerializeField] private int fishCount = 11;

    private static readonly Color Navy = new Color(0.03f, 0.1f, 0.18f, 1f);
    private static readonly Color Cyan = new Color(0.42f, 0.9f, 1f, 1f);

    private class Bubble
    {
        public RectTransform rt;
        public float speed, baseX, wobbleAmp, wobbleSpeed, phase;
    }

    private class Kelp
    {
        public RectTransform[] segs;
        public float baseX, phase;
    }

    private class MenuButton
    {
        public RectTransform rt;
        public Image glow, border, fill;
        public Text label, sub;
        public bool primary;
        public System.Action action;
        public float hover;

        public void Animate(bool on, float dt, float time)
        {
            hover = Mathf.MoveTowards(hover, on ? 1f : 0f, dt * 7f);
            float e = hover * hover * (3f - 2f * hover);

            Color fillN = primary ? new Color(0.97f, 0.72f, 0.16f, 0.96f) : new Color(0.03f, 0.2f, 0.3f, 0.72f);
            Color fillS = primary ? new Color(1f, 0.88f, 0.38f, 1f) : new Color(0.42f, 0.9f, 1f, 0.97f);
            Color borderN = primary ? new Color(1f, 0.92f, 0.6f, 0.95f) : new Color(0.45f, 0.85f, 0.95f, 0.6f);
            Color textN = primary ? Navy : new Color(0.9f, 0.98f, 1f, 1f);

            fill.color = Color.Lerp(fillN, fillS, e);
            border.color = Color.Lerp(borderN, Color.white, e);
            label.color = Color.Lerp(textN, Navy, e);
            if (sub != null) sub.color = Color.Lerp(new Color(0.6f, 0.9f, 1f, 0.9f), new Color(0.03f, 0.1f, 0.18f, 0.85f), e);

            float pulse = on ? 1f + Mathf.Sin(time * 4f) * 0.015f : 1f;
            rt.localScale = Vector3.one * (Mathf.Lerp(1f, 1.07f, e) * pulse);
            glow.color = new Color(primary ? 1f : 0.42f, primary ? 0.82f : 0.9f, primary ? 0.35f : 1f, e * 0.45f);
        }
    }

    private readonly List<MenuButton> buttons = new List<MenuButton>();
    private readonly List<Bubble> bubbles = new List<Bubble>();
    private readonly List<Kelp> kelp = new List<Kelp>();
    private int[][] rows;
    private int selected;
    private Vector2 lastMouse;

    private RectTransform[] shafts;
    private float[] shaftSpeeds;
    private RectTransform titleRt;
    private Image titleGlow;
    private const float TitleY = 290f;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        BuildWater();
        BuildFish();
        BuildUI();
    }

    // ------------------------------------------------------------------ 3D backdrop

    private void BuildWater()
    {
        Color deep = new Color(0.015f, 0.1f, 0.2f, 1f);
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = deep;
            cam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 220f;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = deep;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.34f, 0.44f, 1f);

        GameObject sun = new GameObject("Menu Light");
        Light l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.65f, 0.92f, 1f, 1f);
        l.intensity = 1.3f;
        sun.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
    }

    private void BuildFish()
    {
        if (fishPrefabs == null || fishPrefabs.Length == 0) return;
        Transform holder = new GameObject("Menu Fish").transform;

        for (int i = 0; i < fishCount; i++)
        {
            GameObject prefab = fishPrefabs[i % fishPrefabs.Length];
            if (prefab == null) continue;
            bool giant = prefab.name.Contains("Dolphin") || prefab.name.Contains("Shark") || prefab.name.Contains("Orca");

            float z = giant ? Random.Range(55f, 85f) : Random.Range(22f, 60f);
            float dir = i % 2 == 0 ? 1f : -1f;
            float y = Random.Range(-0.36f, 0.4f) * z;
            float x = Random.Range(-0.85f, 0.85f) * z;

            GameObject go = Instantiate(prefab, new Vector3(x, y, z), Quaternion.LookRotation(new Vector3(dir, 0f, 0f)), holder);
            go.name = "Menu " + prefab.name;
            foreach (Collider c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            Fit(go, z * (giant ? Random.Range(0.30f, 0.38f) : Random.Range(0.10f, 0.20f)));

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 1.5f;
            rb.angularDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            MenuFish mf = go.AddComponent<MenuFish>();
            mf.dir = dir;
            mf.speed = z * Random.Range(0.06f, 0.12f);
            mf.bound = z * 1.2f + 12f;
        }
    }

    private static void Fit(GameObject go, float targetLength)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        float len = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (len > 0.0001f) go.transform.localScale *= targetLength / len;
    }

    // ------------------------------------------------------------------ UI

    private void BuildUI()
    {
        Canvas canvas = StoryUI.MakeCanvas("Main Menu Canvas", 10, transform);
        Transform root = canvas.transform;
        Vector2 mid = new Vector2(0.5f, 0.5f);
        Vector2 bottom = new Vector2(0.5f, 0f);

        // depth tint: clear near the surface, deeper blue toward the sea floor (the 3D fish show through)
        Image tint = StoryUI.MakeFullscreenImage("Depth Tint", root, new Color(0f, 0.06f, 0.14f, 0.78f));
        tint.sprite = MenuArt.Gradient;
        Image surface = StoryUI.MakeImage("Surface Glow", root, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(2600f, 900f), new Color(0.2f, 0.7f, 0.85f, 0.35f));
        surface.sprite = MenuArt.Glow;

        // soft light shafts drifting through the water
        shafts = new RectTransform[4];
        shaftSpeeds = new float[4];
        for (int i = 0; i < shafts.Length; i++)
        {
            Image s = StoryUI.MakeImage("Light Shaft " + i, root, mid, new Vector2(-700f + i * 480f, 100f), new Vector2(260f + i * 40f, 1900f), new Color(0.7f, 0.95f, 1f, 0.09f));
            s.sprite = MenuArt.Glow;
            s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            shafts[i] = s.rectTransform;
            shaftSpeeds[i] = 6f + i * 3f;
        }

        // rising bubbles
        for (int i = 0; i < 34; i++)
        {
            float size = Random.Range(10f, 46f);
            float x = Random.Range(-980f, 980f);
            Image img = StoryUI.MakeImage("Bubble " + i, root, mid, new Vector2(x, Random.Range(-620f, 620f)), new Vector2(size, size), new Color(0.8f, 0.97f, 1f, Random.Range(0.25f, 0.6f)));
            img.sprite = MenuArt.Circle;
            Bubble b = new Bubble();
            b.rt = img.rectTransform;
            b.baseX = x;
            b.speed = 40f + size * 2.2f;
            b.wobbleAmp = Random.Range(8f, 26f);
            b.wobbleSpeed = Random.Range(0.8f, 2f);
            b.phase = Random.value * 6.28f;
            bubbles.Add(b);
        }

        // sea floor and swaying kelp at the edges of the screen
        Image ground = StoryUI.MakeImage("Sea Floor", root, bottom, Vector2.zero, new Vector2(2600f, 130f), new Color(0.03f, 0.1f, 0.12f, 1f));
        ground.sprite = MenuArt.Gradient;
        float[] kelpX = { -905f, -840f, -775f, -705f, 705f, 775f, 840f, 905f };
        for (int k = 0; k < kelpX.Length; k++)
        {
            Kelp strand = new Kelp();
            int count = Random.Range(6, 10);
            strand.baseX = kelpX[k];
            strand.phase = Random.value * 6.28f;
            strand.segs = new RectTransform[count];
            Color c = new Color(Random.Range(0.01f, 0.05f), Random.Range(0.16f, 0.26f), Random.Range(0.16f, 0.22f), 0.92f);
            for (int i = 0; i < count; i++)
            {
                float w = Mathf.Lerp(34f, 12f, i / (float)count);
                Image seg = StoryUI.MakeImage("Kelp " + k + "." + i, root, bottom, new Vector2(strand.baseX, 40f + i * 54f), new Vector2(w, 74f), c);
                seg.sprite = MenuArt.Rounded;
                seg.type = Image.Type.Sliced;
                strand.segs[i] = seg.rectTransform;
            }
            kelp.Add(strand);
        }

        // darkened edges pull the eye to the middle
        Image vignette = StoryUI.MakeFullscreenImage("Vignette", root, new Color(0f, 0.02f, 0.06f, 0.65f));
        vignette.sprite = MenuArt.Vignette;

        // title
        titleGlow = StoryUI.MakeImage("Title Glow", root, mid, new Vector2(0f, TitleY), new Vector2(1500f, 420f), new Color(1f, 0.8f, 0.3f, 0.16f));
        titleGlow.sprite = MenuArt.Glow;

        Text title = StoryUI.MakeText("Title", root, mid, new Vector2(0f, TitleY), new Vector2(1700f, 220f), 176, TextAnchor.MiddleCenter);
        title.font = StoryUI.ThemeFont;
        title.text = "<color=#FFD54A>POWER</color> <color=#6FE7FF>SPLASH</color>";
        Outline outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.01f, 0.07f, 0.15f, 1f);
        outline.effectDistance = new Vector2(6f, -6f);
        titleRt = title.rectTransform;

        Text subtitle = StoryUI.MakeText("Subtitle", root, mid, new Vector2(0f, 165f), new Vector2(1600f, 80f), 56, TextAnchor.MiddleCenter);
        subtitle.font = StoryUI.ThemeFont;
        subtitle.text = "The Lost Little Fish";
        subtitle.color = Cyan;

        float sw = Mathf.Max(380f, subtitle.preferredWidth);
        for (int side = -1; side <= 1; side += 2)
        {
            StoryUI.MakeImage("Subtitle Line", root, mid, new Vector2(side * (sw * 0.5f + 150f), 165f), new Vector2(220f, 3f), new Color(0.42f, 0.9f, 1f, 0.5f));
            Image dot = StoryUI.MakeImage("Subtitle Bubble", root, mid, new Vector2(side * (sw * 0.5f + 28f), 165f), new Vector2(14f, 14f), new Color(0.8f, 0.97f, 1f, 0.8f));
            dot.sprite = MenuArt.Circle;
        }

        Text tagline = StoryUI.MakeText("Tagline", root, mid, new Vector2(0f, 105f), new Vector2(1600f, 50f), 30, TextAnchor.MiddleCenter);
        tagline.font = StoryUI.ThemeFont;
        tagline.text = "Eat smaller fish. Grow. Find your way home.";
        tagline.color = new Color(1f, 1f, 1f, 0.8f);

        // buttons: one big START, a row of three level cards, then QUIT
        buttons.Add(MakeButton(root, "START GAME", null, new Vector2(0f, -8f), new Vector2(600f, 100f), 58, 0, true, StartGame));
        buttons.Add(MakeButton(root, "LEVEL 1", GameHUD.LocationName(1), new Vector2(-370f, -150f), new Vector2(340f, 112f), 40, 26, false, delegate { OpenLevel(1); }));
        buttons.Add(MakeButton(root, "LEVEL 2", GameHUD.LocationName(2), new Vector2(0f, -150f), new Vector2(340f, 112f), 40, 26, false, delegate { OpenLevel(2); }));
        buttons.Add(MakeButton(root, "LEVEL 3", GameHUD.LocationName(3), new Vector2(370f, -150f), new Vector2(340f, 112f), 40, 26, false, delegate { OpenLevel(3); }));
        buttons.Add(MakeButton(root, "QUIT", null, new Vector2(0f, -285f), new Vector2(280f, 70f), 36, 0, false, QuitGame));
        rows = new int[][] { new[] { 0 }, new[] { 1, 2, 3 }, new[] { 4 } };
        selected = 0;

        Text help = StoryUI.MakeText("Controls", root, bottom, new Vector2(0f, 46f), new Vector2(1800f, 80f), 26, TextAnchor.MiddleCenter);
        help.font = StoryUI.ThemeFont;
        help.text = "WASD swim  |  L flashlight  |  R talk / interact  |  J journal  |  ESC pause\nArrow keys + Enter, or the mouse, to choose";
        help.color = new Color(1f, 1f, 1f, 0.7f);
    }

    private MenuButton MakeButton(Transform root, string label, string sub, Vector2 pos, Vector2 size, int labelSize, int subSize, bool primary, System.Action action)
    {
        Vector2 mid = new Vector2(0.5f, 0.5f);
        MenuButton b = new MenuButton();
        b.primary = primary;
        b.action = action;

        GameObject go = new GameObject("Button - " + label);
        go.transform.SetParent(root, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = mid;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        b.rt = rt;

        b.glow = StoryUI.MakeImage("Glow", go.transform, mid, Vector2.zero, size + new Vector2(120f, 90f), new Color(1f, 0.85f, 0.4f, 0f));
        b.glow.sprite = MenuArt.Glow;
        b.border = StoryUI.MakeImage("Border", go.transform, mid, Vector2.zero, size, Color.white);
        b.border.sprite = MenuArt.Rounded;
        b.border.type = Image.Type.Sliced;
        b.fill = StoryUI.MakeImage("Fill", go.transform, mid, Vector2.zero, size - new Vector2(8f, 8f), Color.white);
        b.fill.sprite = MenuArt.Rounded;
        b.fill.type = Image.Type.Sliced;

        float labelY = sub == null ? 0f : 17f;
        b.label = StoryUI.MakeText("Label", go.transform, mid, new Vector2(0f, labelY), new Vector2(size.x, size.y * 0.6f), labelSize, TextAnchor.MiddleCenter);
        b.label.font = StoryUI.ThemeFont;
        b.label.text = label;
        Shadow ls = b.label.GetComponent<Shadow>();
        if (ls != null) ls.enabled = false;

        if (sub != null)
        {
            b.sub = StoryUI.MakeText("Sub", go.transform, mid, new Vector2(0f, -27f), new Vector2(size.x, 36f), subSize, TextAnchor.MiddleCenter);
            b.sub.font = StoryUI.ThemeFont;
            b.sub.text = sub;
            Shadow ss = b.sub.GetComponent<Shadow>();
            if (ss != null) ss.enabled = false;
        }

        b.Animate(false, 1f, 0f);
        return b;
    }

    // ------------------------------------------------------------------ input and animation

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        HandleInput();
        for (int i = 0; i < buttons.Count; i++) buttons[i].Animate(i == selected, dt, t);

        for (int i = 0; shafts != null && i < shafts.Length; i++)
        {
            Vector2 p = shafts[i].anchoredPosition;
            p.x += shaftSpeeds[i] * dt;
            if (p.x > 1400f) p.x = -1400f;
            shafts[i].anchoredPosition = p;
        }

        for (int i = 0; i < bubbles.Count; i++)
        {
            Bubble b = bubbles[i];
            Vector2 p = b.rt.anchoredPosition;
            p.y += b.speed * dt;
            if (p.y > 640f)
            {
                p.y = -640f - Random.Range(0f, 200f);
                b.baseX = Random.Range(-980f, 980f);
            }
            p.x = b.baseX + Mathf.Sin(t * b.wobbleSpeed + b.phase) * b.wobbleAmp;
            b.rt.anchoredPosition = p;
        }

        for (int k = 0; k < kelp.Count; k++)
        {
            Kelp strand = kelp[k];
            for (int i = 0; i < strand.segs.Length; i++)
            {
                Vector2 p = strand.segs[i].anchoredPosition;
                p.x = strand.baseX + Mathf.Sin(t * 0.8f + strand.phase + i * 0.55f) * (4f + i * 2.4f);
                strand.segs[i].anchoredPosition = p;
            }
        }

        if (titleRt != null) titleRt.anchoredPosition = new Vector2(0f, TitleY + Mathf.Sin(t * 0.9f) * 6f);
        if (titleGlow != null)
        {
            Color c = titleGlow.color;
            c.a = 0.14f + 0.06f * Mathf.Sin(t * 1.3f);
            titleGlow.color = c;
        }
    }

    private void HandleInput()
    {
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;

        if (m != null)
        {
            Vector2 p = m.position.ReadValue();
            if ((p - lastMouse).sqrMagnitude > 1f)
            {
                lastMouse = p;
                for (int i = 0; i < buttons.Count; i++)
                    if (StoryUI.PointerOver(buttons[i].rt)) selected = i;
            }
        }

        if (k != null)
        {
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) MoveRow(1);
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) MoveRow(-1);
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) MoveCol(1);
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) MoveCol(-1);
        }

        bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                       (m != null && m.leftButton.wasPressedThisFrame && StoryUI.PointerOver(buttons[selected].rt));
        if (confirm && buttons[selected].action != null) buttons[selected].action();
    }

    private void FindPos(out int row, out int col)
    {
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                if (rows[r][c] == selected) { row = r; col = c; return; }
        row = 0; col = 0;
    }

    private void MoveRow(int d)
    {
        int row, col;
        FindPos(out row, out col);
        int nr = (row + d + rows.Length) % rows.Length;
        int nc = rows[nr].Length == 1 ? 0 : (rows[row].Length == 1 ? rows[nr].Length / 2 : Mathf.Min(col, rows[nr].Length - 1));
        selected = rows[nr][nc];
    }

    private void MoveCol(int d)
    {
        int row, col;
        FindPos(out row, out col);
        int nc = Mathf.Clamp(col + d, 0, rows[row].Length - 1);
        selected = rows[row][nc];
    }

    private void StartGame() { OpenLevel(1); }

    private void OpenLevel(int tier)
    {
        PlayerProgress.Clear();   // fresh story; the story flags for earlier levels are assumed by StoryStateManager
        PlayerInventory.Reset();
        SceneManager.LoadScene(PlayerProgress.SceneIndexForTier(tier));
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

/// <summary>Small procedurally drawn sprites for the menu, so no image assets are needed.</summary>
internal static class MenuArt
{
    private static Sprite rounded, circle, glow, vignette, gradient;

    public static Sprite Rounded { get { if (rounded == null) rounded = MakeRounded(); return rounded; } }
    public static Sprite Circle { get { if (circle == null) circle = MakeCircle(); return circle; } }
    public static Sprite Glow { get { if (glow == null) glow = MakeGlow(); return glow; } }
    public static Sprite Vignette { get { if (vignette == null) vignette = MakeVignette(); return vignette; } }
    /// <summary>Opaque at the bottom, fading to nothing at the top.</summary>
    public static Sprite Gradient { get { if (gradient == null) gradient = MakeGradient(); return gradient; } }

    private static Texture2D NewTex(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    private static Sprite Finish(Texture2D t, Color32[] px, Vector4 border)
    {
        t.SetPixels32(px);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    private static Sprite MakeRounded()
    {
        const int n = 128;
        const float r = 44f;
        Texture2D t = NewTex(n, n);
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - n * 0.5f) - (n * 0.5f - r), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - n * 0.5f) - (n * 0.5f - r), 0f);
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        return Finish(t, px, new Vector4(r, r, r, r));
    }

    private static Sprite MakeCircle()
    {
        const int n = 64;
        Texture2D t = NewTex(n, n);
        Color32[] px = new Color32[n * n];
        float half = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - half, dy = y + 0.5f - half;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / half;
                float edge = Mathf.Clamp01((1f - d) * half);
                float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 0.95f, d));
                float a = 0.12f + 0.85f * rim;
                float hx = x + 0.5f - n * 0.34f, hy = y + 0.5f - n * 0.68f;
                float h = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) / (n * 0.12f));
                a = Mathf.Clamp01(a + h * 0.7f) * edge;
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        return Finish(t, px, Vector4.zero);
    }

    private static Sprite MakeGlow()
    {
        const int n = 128;
        Texture2D t = NewTex(n, n);
        Color32[] px = new Color32[n * n];
        float half = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - half, dy = y + 0.5f - half;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / half);
                float a = (1f - d) * (1f - d);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        return Finish(t, px, Vector4.zero);
    }

    private static Sprite MakeVignette()
    {
        const int n = 128;
        Texture2D t = NewTex(n, n);
        Color32[] px = new Color32[n * n];
        float half = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1.35f, d));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        return Finish(t, px, Vector4.zero);
    }

    private static Sprite MakeGradient()
    {
        const int w = 4, h = 128;
        Texture2D t = NewTex(w, h);
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float a = 1f - y / (float)(h - 1);
            for (int x = 0; x < w; x++) px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        return Finish(t, px, Vector4.zero);
    }
}
