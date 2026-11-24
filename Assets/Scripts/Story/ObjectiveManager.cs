using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Story-driven objectives. Listens to StoryStateManager.OnFlagSet and updates the objective tracker
/// so it always matches the current story state (the objectives themselves are data in the StoryTable).
/// The tracker is hidden in the main menu, during cutscenes and on the loading screen.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }
    public static string CurrentText { get; private set; }

    private GameObject canvasGO;
    private Text bodyText;
    private Image border;
    private string current = "";
    private float flashTimer;
    private string pendingAnnouncement;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; CurrentText = ""; }

    private void Awake()
    {
        Instance = this;
        Build();
    }

    private void Start()
    {
        StoryStateManager s = StoryStateManager.Instance;
        if (s != null)
        {
            s.OnFlagSet += HandleFlag;
            s.OnStateReset += HandleReset;
        }
        Refresh(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        StoryStateManager s = StoryStateManager.Instance;
        if (s != null)
        {
            s.OnFlagSet -= HandleFlag;
            s.OnStateReset -= HandleReset;
        }
    }

    private void HandleFlag(string flag) { Refresh(true); }
    private void HandleReset() { current = ""; Refresh(false); }

    private void Build()
    {
        Canvas canvas = StoryUI.MakeCanvas("Objective Canvas", 140, transform);
        canvasGO = canvas.gameObject;
        Transform root = canvasGO.transform;

        Vector2 topRight = new Vector2(1f, 1f);
        border = StoryUI.MakeImage("Objective Border", root, topRight, new Vector2(-14f, -150f), new Vector2(566f, 146f), new Color(0.35f, 0.85f, 0.95f, 0.5f));
        border.sprite = MenuArt.Panel;
        border.type = Image.Type.Sliced;
        Image bg = StoryUI.MakeImage("Objective Panel", border.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 140f), new Color(0.02f, 0.08f, 0.14f, 0.86f));
        bg.sprite = MenuArt.Panel;
        bg.type = Image.Type.Sliced;

        Text header = StoryUI.MakeText("Objective Header", bg.transform, new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(400f, 26f), 20, TextAnchor.UpperLeft);
        header.text = "OBJECTIVE";
        header.color = new Color(0.5f, 0.92f, 1f);
        header.font = StoryUI.ThemeFont;

        bodyText = StoryUI.MakeText("Objective Text", bg.transform, new Vector2(0f, 1f), new Vector2(16f, -38f), new Vector2(528f, 70f), 25, TextAnchor.UpperLeft, true);

        Text hint = StoryUI.MakeText("Journal Hint", bg.transform, new Vector2(1f, 0f), new Vector2(-14f, 8f), new Vector2(300f, 22f), 18, TextAnchor.LowerRight);
        hint.text = "[J] Journal    [ESC] Pause";
        hint.color = new Color(1f, 1f, 1f, 0.6f);
    }

    private void Refresh(bool announce)
    {
        string text = StoryStateManager.Table != null ? StoryStateManager.Table.CurrentObjective() : "";
        if (text == current) return;

        bool hadObjective = !string.IsNullOrEmpty(current);
        current = text;
        CurrentText = text;
        bodyText.text = text;

        if (announce && hadObjective && StoryStateManager.InLevel && !string.IsNullOrEmpty(text))
            pendingAnnouncement = text;   // shown once the dialogue / cutscene is over
    }

    private void Update()
    {
        bool show = StoryStateManager.InLevel && !CinematicManager.Busy && !LevelTransitionManager.Loading;
        if (canvasGO.activeSelf != show) canvasGO.SetActive(show);
        if (!show) return;

        if (pendingAnnouncement != null && !DialogueManager.IsBusy && Time.timeScale > 0.01f)
        {
            flashTimer = 2.5f;
            StoryAudio.Play(StorySfx.ObjectiveUpdate);
            GameHUD.Message("New objective: " + pendingAnnouncement, 4f);
            pendingAnnouncement = null;
        }

        // The border pulses briefly whenever the objective changes
        if (flashTimer > 0f)
        {
            flashTimer -= Time.unscaledDeltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
            border.color = new Color(1f, 0.9f, 0.4f, 0.5f + 0.5f * pulse);
        }
        else
        {
            border.color = new Color(0.35f, 0.85f, 0.95f, 0.5f);
        }
    }
}
