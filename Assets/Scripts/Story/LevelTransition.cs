using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Story-gated level changes. PlayerEating asks for the next level when the fish reaches a new size tier,
/// but the level only opens when the story flags for the current level are set (see LevelGate in the StoryTable).
/// If something is missing the player gets a hint and the change happens as soon as the missing beat is done.
/// The loading screen shows a recap of the story so far and the next objective, then the next scene loads.
/// </summary>
public class LevelTransition : MonoBehaviour
{
    public static LevelTransition Instance { get; private set; }

    /// <summary>The loading screen is up (other story UI waits).</summary>
    public static bool Loading { get; private set; }

    [SerializeField] private int tier2Threshold = 50;
    [SerializeField] private int tier3Threshold = 120;
    [SerializeField] private float minShowSeconds = 4f;
    [SerializeField] private float autoContinueSeconds = 9f;

    private int pendingTier;

    private GameObject canvasGO;
    private CanvasGroup group;
    private Text titleText, recapText, objectiveText, hintText;
    private RectTransform barFill;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; Loading = false; }

    private void Awake()
    {
        Instance = this;
        BuildOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) { Instance = null; Loading = false; }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pendingTier = 0;
        // The fish may arrive already big enough for the next level while a story beat is still missing
        if (StoryStateManager.InLevel && PlayerProgress.HasData && PlayerProgress.Tier > PlayerProgress.SceneTier())
            pendingTier = PlayerProgress.Tier;
    }

    /// <summary>Called by PlayerEating when the fish reaches a new size tier.</summary>
    public void RequestTier(int tier)
    {
        if (tier <= PlayerProgress.SceneTier()) return;
        pendingTier = Mathf.Max(pendingTier, Mathf.Min(tier, PlayerProgress.MaxTier));
        TryProceed(true);
    }

    private void Update()
    {
        if (pendingTier > PlayerProgress.SceneTier() && !Loading && Time.timeScale > 0.01f)
            TryProceed(false);
    }

    private void TryProceed(bool announce)
    {
        int cur = PlayerProgress.SceneTier();
        if (Loading || pendingTier <= cur || cur >= PlayerProgress.MaxTier) return;
        if (DialogueManager.IsBusy || CinematicManager.Busy) return;

        LevelGate gate = StoryStateManager.Table != null ? StoryStateManager.Table.GateFor(cur) : null;
        if (gate != null && !StoryStateManager.HasAll(gate.requiredFlags))
        {
            if (announce) GameHUD.Message(gate.holdMessage, 6f);
            return;
        }

        if (announce) GameHUD.Message("Size Tier " + (cur + 1) + " reached!", 2f);
        StartCoroutine(LoadRoutine(cur + 1, gate));
    }

    private IEnumerator LoadRoutine(int toTier, LevelGate gate)
    {
        Loading = true;
        StoryAudio.Play(StorySfx.Whoosh);

        // 1. Story bookkeeping: leaving the level completes its act
        if (gate != null) StoryStateManager.Set(gate.completeFlag);

        // 2. Carry the fish into the next level (same data PlayerEating used before)
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        EntityData data = p != null ? p.GetComponent<EntityData>() : null;
        float growth = data != null ? data.growthValue : (toTier >= 3 ? tier3Threshold : tier2Threshold);
        int tier = growth >= tier3Threshold ? 3 : (growth >= tier2Threshold ? 2 : 1);
        tier = Mathf.Max(tier, toTier);
        AbilityType stored = AbilitySystem.Instance != null ? AbilitySystem.Instance.Stored : AbilityType.None;
        PlayerProgress.Save(growth, tier, stored);
        PlayerInventory.Reset();

        // 3. Loading screen with a recap that reflects the player's choices
        titleText.text = "LEVEL " + toTier + "\n" + GameHUD.LocationName(toTier).ToUpper();
        recapText.text = BuildRecap(gate);
        string next = StoryStateManager.Table != null ? StoryStateManager.Table.CurrentObjective() : "";
        objectiveText.text = string.IsNullOrEmpty(next) ? "" : "NEXT OBJECTIVE:  " + next;
        hintText.text = "Loading...";
        barFill.anchorMax = new Vector2(0f, 1f);

        // Not exactly 0: Unity stalls asynchronous scene loading while Time.timeScale is 0
        Time.timeScale = 0.001f;
        canvasGO.SetActive(true);
        yield return FadeGroup(0f, 1f, 0.6f);

        AsyncOperation op = SceneManager.LoadSceneAsync(PlayerProgress.SceneIndexForTier(toTier));
        op.allowSceneActivation = false;

        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float loaded = Mathf.Clamp01(op.progress / 0.9f);
            float shown = Mathf.Min(loaded, elapsed / minShowSeconds);
            barFill.anchorMax = new Vector2(shown, 1f);

            bool ready = loaded >= 1f && elapsed >= minShowSeconds;
            if (ready)
            {
                hintText.text = "Press SPACE to continue";
                if (StoryUI.ConfirmPressed() || elapsed >= autoContinueSeconds) break;
            }
            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // The new level is up: reveal it
        Time.timeScale = 1f;
        yield return FadeGroup(1f, 0f, 0.9f);
        canvasGO.SetActive(false);
        Loading = false;
    }

    private string BuildRecap(LevelGate gate)
    {
        if (gate == null) return "";
        string asset = gate.recapAsset;
        if (!string.IsNullOrEmpty(gate.recapAltFlag) && StoryStateManager.Has(gate.recapAltFlag)) asset = gate.recapAltAsset;
        DialogueData recap = Resources.Load<DialogueData>("Story/" + asset);
        return recap != null ? recap.JoinedText() : "";
    }

    private IEnumerator FadeGroup(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private void BuildOverlay()
    {
        Canvas canvas = StoryUI.MakeCanvas("Loading Canvas", 190, transform);
        canvasGO = canvas.gameObject;
        group = canvasGO.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        Transform root = canvasGO.transform;

        StoryUI.MakeFullscreenImage("Background", root, new Color(0.01f, 0.05f, 0.1f, 1f));
        Vector2 mid = new Vector2(0.5f, 0.5f);

        titleText = StoryUI.MakeText("Title", root, mid, new Vector2(0f, 270f), new Vector2(1600f, 220f), 80, TextAnchor.MiddleCenter);
        titleText.color = new Color(1f, 0.9f, 0.4f);

        Text recapHeader = StoryUI.MakeText("Recap Header", root, mid, new Vector2(0f, 120f), new Vector2(1200f, 40f), 26, TextAnchor.MiddleCenter);
        recapHeader.text = "THE STORY SO FAR";
        recapHeader.color = new Color(0.5f, 0.92f, 1f);

        recapText = StoryUI.MakeText("Recap", root, mid, new Vector2(0f, -30f), new Vector2(1250f, 260f), 34, TextAnchor.UpperCenter, true);
        recapText.rectTransform.pivot = new Vector2(0.5f, 1f);
        recapText.rectTransform.anchoredPosition = new Vector2(0f, 90f);

        objectiveText = StoryUI.MakeText("Next Objective", root, mid, new Vector2(0f, -250f), new Vector2(1400f, 80f), 30, TextAnchor.MiddleCenter, true);
        objectiveText.color = new Color(0.5f, 0.92f, 1f);

        Image barBg = StoryUI.MakeImage("Bar BG", root, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(800f, 14f), new Color(1f, 1f, 1f, 0.15f));
        Image fill = StoryUI.MakeImage("Bar Fill", barBg.transform, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.35f, 0.85f, 0.95f, 1f));
        barFill = fill.rectTransform;
        barFill.anchorMin = new Vector2(0f, 0f);
        barFill.anchorMax = new Vector2(0f, 1f);
        barFill.offsetMin = Vector2.zero;
        barFill.offsetMax = Vector2.zero;

        hintText = StoryUI.MakeText("Hint", root, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(900f, 40f), 28, TextAnchor.LowerCenter);
        hintText.color = new Color(1f, 1f, 1f, 0.8f);

        canvasGO.SetActive(false);
    }
}
