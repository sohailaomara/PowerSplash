using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen story moments: the opening text crawl, the epilogue, and the ending sequence.
/// Space / Enter / click moves to the next line and ESC skips the whole crawl (a cutscene must always be skippable).
/// The flag of the crawl's DialogueData is set when it finishes, even when it was skipped.
/// </summary>
public class CinematicManager : MonoBehaviour
{
    public static CinematicManager Instance { get; private set; }

    /// <summary>A crawl is on screen right now (other story UI waits).</summary>
    public static bool Busy { get; private set; }

    private GameObject canvasGO;
    private Image overlay;
    private Text lineText, hintText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; Busy = false; }

    private void Awake()
    {
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; Busy = false; }
    }

    private void Build()
    {
        Canvas canvas = StoryUI.MakeCanvas("Cinematic Canvas", 180, transform);
        canvasGO = canvas.gameObject;
        overlay = StoryUI.MakeFullscreenImage("Overlay", canvasGO.transform, new Color(0.01f, 0.04f, 0.09f, 0f));
        lineText = StoryUI.MakeText("Crawl Line", canvasGO.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 400f), 46, TextAnchor.MiddleCenter, true);
        hintText = StoryUI.MakeText("Crawl Hint", canvasGO.transform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1000f, 40f), 24, TextAnchor.LowerCenter);
        hintText.text = "[SPACE] next        [ESC] skip";
        hintText.color = new Color(1f, 1f, 1f, 0.55f);
        canvasGO.SetActive(false);
    }

    public void PlayCrawl(DialogueData data, Action onDone)
    {
        Busy = true;   // set immediately so no other story UI starts in the meantime
        StartCoroutine(CrawlRoutine(data, onDone));
    }

    private IEnumerator CrawlRoutine(DialogueData data, Action onDone)
    {
        float prevScale = Time.timeScale > 0.01f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        canvasGO.SetActive(true);
        lineText.text = "";
        SetTextAlpha(0f);
        yield return FadeOverlay(0f, 0.96f, 0.6f);

        bool skipAll = false;
        if (data.lines != null)
        {
            for (int i = 0; i < data.lines.Length && !skipAll; i++)
            {
                string text = data.lines[i].text ?? "";
                lineText.text = text;

                float t = 0f;
                while (t < 0.9f && !skipAll)
                {
                    t += Time.unscaledDeltaTime;
                    SetTextAlpha(t / 0.9f);
                    if (StoryUI.EscapePressed()) skipAll = true;
                    yield return null;
                }
                SetTextAlpha(1f);

                float hold = Mathf.Max(3.2f, text.Length * 0.055f);
                float held = 0f;
                while (held < hold && !skipAll)
                {
                    held += Time.unscaledDeltaTime;
                    if (held > 0.5f && StoryUI.ConfirmPressed()) break;
                    if (StoryUI.EscapePressed()) skipAll = true;
                    yield return null;
                }

                t = 0f;
                while (t < 0.45f)
                {
                    t += Time.unscaledDeltaTime;
                    SetTextAlpha(1f - t / 0.45f);
                    yield return null;
                }
                SetTextAlpha(0f);
            }
        }

        yield return FadeOverlay(0.96f, 0f, 0.6f);
        canvasGO.SetActive(false);
        Time.timeScale = prevScale;
        Busy = false;

        // The crawl counts as seen even if it was skipped
        StoryStateManager.Set(data.flagToSetWhenFinished);
        if (!string.IsNullOrEmpty(data.journalTitle) && StoryStateManager.Instance != null)
            StoryStateManager.Instance.AddJournalFromData(data, true);
        if (data.abilityWhenFinished != AbilityType.None && AbilitySystem.Instance != null)
            AbilitySystem.Instance.Grant(data.abilityWhenFinished);

        if (onDone != null) onDone();
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            Color c = overlay.color;
            c.a = Mathf.Lerp(from, to, t / duration);
            overlay.color = c;
            yield return null;
        }
        Color end = overlay.color;
        end.a = to;
        overlay.color = end;
    }

    private void SetTextAlpha(float a)
    {
        Color c = lineText.color;
        c.a = Mathf.Clamp01(a);
        lineText.color = c;
        Color h = hintText.color;
        h.a = Mathf.Clamp01(a) * 0.55f;
        hintText.color = h;
    }

    // ------------------------------------------------------------------ the ending

    /// <summary>
    /// Called when the Golden Clam is eaten. Plays the ending conversation (which depends on the player's choice),
    /// then the epilogue crawl, and finally the game's win screen.
    /// </summary>
    public static void PlayEnding(string detail)
    {
        bool shortcut = StoryStateManager.Has("tookShortcut");
        DialogueData ending = Resources.Load<DialogueData>("Story/" + (shortcut ? "end_lumi" : "end_pip"));
        DialogueData epilogue = Resources.Load<DialogueData>("Story/" + (shortcut ? "epilogue_lumi" : "epilogue_pip"));

        StoryStateManager.Set("goldenLightTaken");
        StoryAudio.Play(StorySfx.GoldenSwell);

        Action finish = delegate
        {
            StoryStateManager.Set("gameComplete");
            GameHUD.ShowWinScreen(detail);
        };
        Action afterEnding = delegate
        {
            if (epilogue != null && Instance != null) Instance.PlayCrawl(epilogue, finish);
            else finish();
        };

        if (ending != null && DialogueManager.Instance != null) DialogueManager.Instance.StartDialogue(ending, afterEnding);
        else afterEnding();
    }
}
