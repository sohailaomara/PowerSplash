using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built HUD (uGUI, legacy Text so no TMP import is needed).
/// Polls the player each frame, so other scripts never need to push UI state.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    private Font font;
    private Text statsText, abilityText, questText, cameraText, lightText, messageText, promptText, helpText;
    private RectTransform growthFill, abilityFill;

    private EntityData playerData;
    private float messageTimer;
    private string prompt = "";

    [SerializeField] private int tier2Threshold = 50;
    [SerializeField] private int tier3Threshold = 120;

    private void Awake()
    {
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public static void Message(string text, float seconds = 3f)
    {
        if (Instance == null) return;
        Instance.messageText.text = text;
        Instance.messageTimer = seconds;
    }

    public static void SetPrompt(string text) { if (Instance != null) Instance.prompt = text ?? ""; }

    private void Update()
    {
        if (playerData == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerData = p.GetComponent<EntityData>();
        }

        // --- Size / growth ---
        if (playerData != null)
        {
            float low = playerData.sizeTier <= 1 ? 0f : (playerData.sizeTier == 2 ? tier2Threshold : tier3Threshold);
            float high = playerData.sizeTier <= 1 ? tier2Threshold : (playerData.sizeTier == 2 ? tier3Threshold : tier3Threshold);
            float p = playerData.sizeTier >= 3 ? 1f : Mathf.Clamp01((playerData.growthValue - low) / Mathf.Max(1f, high - low));
            statsText.text = "SIZE TIER " + playerData.sizeTier + "     GROWTH " + Mathf.FloorToInt(playerData.growthValue);
            growthFill.anchorMax = new Vector2(p, 1f);
        }

        // --- Ability ---
        AbilitySystem ab = AbilitySystem.Instance;
        if (ab != null)
        {
            string line;
            float fill = 0f;
            if (ab.Active != AbilityType.None)
            {
                line = "ACTIVE: " + ab.Active + "  " + ab.ActiveRemaining.ToString("0.0") + "s";
                fill = ab.ActiveRemaining / Mathf.Max(0.01f, ab.ActiveDuration);
            }
            else if (ab.Stored != AbilityType.None)
            {
                float cd = ab.CooldownRemaining(ab.Stored);
                line = cd > 0f ? "STORED: " + ab.Stored + "  (cooldown " + cd.ToString("0.0") + "s)" : "STORED: " + ab.Stored + "  - press F";
                fill = 1f;
            }
            else
            {
                line = "NO ABILITY - eat a glowing creature";
            }
            abilityText.text = line;
            abilityFill.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
        }

        // --- Quest ---
        questText.text = "ANCIENT PEARLS  " + PlayerInventory.Pearls + " / " + PlayerInventory.PearlsRequired +
                         (PlayerInventory.Pearls >= PlayerInventory.PearlsRequired ? "   (gate open!)" : "");

        // --- Camera / lighting ---
        cameraText.text = "CAMERA: " + (CameraDirector.Instance != null ? CameraDirector.Instance.CurrentLabel : "-");
        string zone = LightZone.CurrentZoneName;
        lightText.text = "TIME " + DayNightCycle.TimeString + "   FLASHLIGHT " + (PlayerFlashlight.IsOn ? "ON" : "OFF") + "   ZONE: " + zone;

        // --- Messages / prompt ---
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) messageText.text = "";
        }
        promptText.text = prompt;
    }

    // ------------------------------------------------------------------ UI construction

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("HUD Canvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = canvasGO.transform;

        // Top-left: size + ability
        statsText = MakeText("Stats", root, new Vector2(0, 1), new Vector2(30, -30), new Vector2(620, 40), 30, TextAnchor.UpperLeft);
        growthFill = MakeBar("GrowthBar", root, new Vector2(0, 1), new Vector2(30, -78), new Vector2(420, 18), new Color(0.2f, 0.9f, 0.5f));
        abilityText = MakeText("Ability", root, new Vector2(0, 1), new Vector2(30, -112), new Vector2(760, 36), 26, TextAnchor.UpperLeft);
        abilityFill = MakeBar("AbilityBar", root, new Vector2(0, 1), new Vector2(30, -152), new Vector2(420, 14), new Color(1f, 0.75f, 0.2f));

        // Top-right: quest, camera, lighting
        questText = MakeText("Quest", root, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(760, 36), 26, TextAnchor.UpperRight);
        cameraText = MakeText("Camera", root, new Vector2(1, 1), new Vector2(-30, -68), new Vector2(760, 36), 24, TextAnchor.UpperRight);
        lightText = MakeText("Lighting", root, new Vector2(1, 1), new Vector2(-30, -102), new Vector2(980, 36), 22, TextAnchor.UpperRight);

        // Bottom-centre: message + prompt
        messageText = MakeText("Message", root, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1400, 50), 36, TextAnchor.LowerCenter);
        promptText = MakeText("Prompt", root, new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(1000, 40), 28, TextAnchor.LowerCenter);
        promptText.color = new Color(1f, 0.95f, 0.5f);

        // Bottom-left: controls
        helpText = MakeText("Help", root, new Vector2(0, 0), new Vector2(30, 24), new Vector2(1100, 150), 20, TextAnchor.LowerLeft);
        helpText.text =
            "WASD swim  |  Q/E down/up  |  Shift sprint\n" +
            "F use ability  |  G bubble lure  |  R interact\n" +
            "V first/third-person  |  L flashlight  |  T fast-forward time\n" +
            "1-5 grant ability (debug): Inflate, Ink, Shock, Speed, Shield";
        helpText.color = new Color(1f, 1f, 1f, 0.75f);
    }

    private Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Shadow s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.8f);
        s.effectDistance = new Vector2(2f, -2f);
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }

    private RectTransform MakeBar(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor)
    {
        GameObject bg = new GameObject(name + " BG");
        bg.transform.SetParent(parent, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.55f);
        bgImg.raycastTarget = false;
        RectTransform bgRt = bgImg.rectTransform;
        bgRt.anchorMin = bgRt.anchorMax = anchor;
        bgRt.pivot = anchor;
        bgRt.anchoredPosition = pos;
        bgRt.sizeDelta = size;

        GameObject fill = new GameObject(name + " Fill");
        fill.transform.SetParent(bg.transform, false);
        Image fImg = fill.AddComponent<Image>();
        fImg.color = fillColor;
        fImg.raycastTarget = false;
        RectTransform fRt = fImg.rectTransform;
        fRt.anchorMin = new Vector2(0, 0);
        fRt.anchorMax = new Vector2(0, 1);
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;
        return fRt;
    }
}
