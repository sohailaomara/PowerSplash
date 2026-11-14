using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dialogue LOGIC. Loads a DialogueData, shows one line at a time with a typewriter effect, advances on
/// Space / Enter / click, shows choices after the last line, and applies the results (flags, journal entry,
/// ability reward) when a conversation ends. The DATA lives in DialogueData assets and the DISPLAY in DialogueUI,
/// so each part can be changed without touching the others.
/// The game is paused while a conversation is open.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    /// <summary>A conversation is on screen right now.</summary>
    public static bool IsActive { get { return Instance != null && Instance.active; } }

    /// <summary>A conversation is on screen or waiting to start.</summary>
    public static bool IsBusy { get { return Instance != null && (Instance.active || Instance.queue.Count > 0); } }

    [SerializeField] private float charsPerSecond = 60f;

    private class Request
    {
        public DialogueData data;
        public Action onComplete;
    }

    private readonly Queue<Request> queue = new Queue<Request>();
    private DialogueUI ui;

    private bool active;
    private bool showingChoices;
    private DialogueData current;
    private Action currentCallback;
    private int lineIndex;
    private float revealed;
    private string fullText = "";
    private float lineStartedAt;
    private float choicesShownAt;
    private int lastBlipChar;
    private int choiceIndex;
    private Vector2 lastMouse;
    private AbilityType pendingAbility = AbilityType.None;

    private float prevTimeScale = 1f;
    private CursorLockMode prevLock;
    private bool prevCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake()
    {
        Instance = this;
        ui = gameObject.AddComponent<DialogueUI>();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>Queues a conversation. It starts as soon as nothing else (pause, cutscene, loading screen) is in the way.</summary>
    public void StartDialogue(DialogueData data, Action onComplete = null)
    {
        if (data == null)
        {
            if (onComplete != null) onComplete();
            return;
        }
        queue.Enqueue(new Request { data = data, onComplete = onComplete });
    }

    private void Update()
    {
        if (!active) { TryStartNext(); return; }
        if (showingChoices) UpdateChoices();
        else UpdateTyping();
    }

    private void TryStartNext()
    {
        if (queue.Count == 0) return;
        if (Time.timeScale <= 0.01f || CinematicManager.Busy || LevelTransition.Loading) return;

        Request r = queue.Dequeue();
        if (r.data.presentation == DialoguePresentation.Crawl)
        {
            if (CinematicManager.Instance != null) CinematicManager.Instance.PlayCrawl(r.data, r.onComplete);
            else if (r.onComplete != null) r.onComplete();
            return;
        }
        Begin(r);
    }

    private void Begin(Request r)
    {
        current = r.data;
        currentCallback = r.onComplete;
        active = true;
        showingChoices = false;

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        prevLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ui.SetVisible(true);
        ui.HideChoices();
        lineIndex = 0;
        ShowLine();
    }

    private void ShowLine()
    {
        if (current.lines == null || lineIndex >= current.lines.Length) { AfterLines(); return; }

        DialogueLine line = current.lines[lineIndex];
        fullText = line.text ?? "";
        revealed = 0f;
        lastBlipChar = 0;
        lineStartedAt = Time.unscaledTime;
        ui.SetSpeaker(line.speaker, line.portrait);
        ui.SetBody("");
        ui.SetHint("");
    }

    private void UpdateTyping()
    {
        revealed += charsPerSecond * Time.unscaledDeltaTime;
        int n = Mathf.Min(fullText.Length, Mathf.FloorToInt(revealed));
        ui.SetBody(fullText.Substring(0, n));

        // a little voice blip every couple of letters, pitched per speaker
        if (n >= lastBlipChar + 2 && n > 0)
        {
            if (fullText[n - 1] != ' ') StoryAudio.Blip(current.lines[lineIndex].speaker);
            lastBlipChar = n;
        }

        bool done = n >= fullText.Length;
        bool lastLine = current.lines == null || (lineIndex >= current.lines.Length - 1 && (current.choices == null || current.choices.Length == 0));
        ui.SetHint(done ? (lastLine ? "[SPACE]  close" : "[SPACE]  continue") : "[SPACE]  skip");

        if (Time.unscaledTime - lineStartedAt < 0.15f) return;   // ignore the key that started the line
        if (!StoryUI.ConfirmPressed()) return;

        if (!done) revealed = fullText.Length;
        else { lineIndex++; ShowLine(); }
    }

    private void AfterLines()
    {
        if (current.choices != null && current.choices.Length > 0) EnterChoices();
        else EndCurrent(null);
    }

    // ------------------------------------------------------------------ choices

    private void EnterChoices()
    {
        showingChoices = true;
        choiceIndex = 0;
        choicesShownAt = Time.unscaledTime;
        string[] texts = new string[current.choices.Length];
        for (int i = 0; i < texts.Length; i++) texts[i] = current.choices[i].text;
        ui.ShowChoices(texts, choiceIndex);
        ui.SetHint("[1-" + texts.Length + "] or click to choose");
    }

    private void UpdateChoices()
    {
        if (Time.unscaledTime - choicesShownAt < 0.25f) return;

        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        int count = current.choices.Length;
        int before = choiceIndex;

        if (m != null)
        {
            Vector2 p = m.position.ReadValue();
            if ((p - lastMouse).sqrMagnitude > 1f)
            {
                lastMouse = p;
                int hover = ui.ChoiceUnderMouse();
                if (hover >= 0) choiceIndex = hover;
            }
        }

        if (k != null)
        {
            for (int i = 0; i < count && i < 9; i++)
            {
                if (k[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) { PickChoice(i); return; }
            }
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) choiceIndex = (choiceIndex + 1) % count;
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) choiceIndex = (choiceIndex - 1 + count) % count;
        }

        if (choiceIndex != before)
        {
            ui.SetSelected(choiceIndex);
            StoryAudio.Play(StorySfx.ChoiceMove);
        }

        bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                       (m != null && m.leftButton.wasPressedThisFrame && ui.ChoiceUnderMouse() >= 0);
        if (confirm)
        {
            int hover = ui.ChoiceUnderMouse();
            PickChoice(m != null && m.leftButton.wasPressedThisFrame && hover >= 0 ? hover : choiceIndex);
        }
    }

    private void PickChoice(int index)
    {
        DialogueChoice c = current.choices[index];
        StoryStateManager.Set(c.flagToSet);   // the player's decision, stored as a story flag
        StoryAudio.Play(StorySfx.ChoiceConfirm);
        showingChoices = false;
        ui.HideChoices();
        EndCurrent(c.next);
    }

    // ------------------------------------------------------------------ finishing

    /// <summary>Applies what the finished conversation does, then continues into 'next' or closes the box.</summary>
    private void EndCurrent(DialogueData next)
    {
        StoryStateManager.Set(current.flagToSetWhenFinished);

        if (current.abilityWhenFinished != AbilityType.None) pendingAbility = current.abilityWhenFinished;
        if (!string.IsNullOrEmpty(current.journalTitle) && StoryStateManager.Instance != null)
            StoryStateManager.Instance.AddJournalFromData(current, true);

        if (next != null)
        {
            current = next;
            lineIndex = 0;
            ShowLine();
            return;
        }
        Close();
    }

    private void Close()
    {
        active = false;
        showingChoices = false;
        ui.SetVisible(false);

        Time.timeScale = prevTimeScale <= 0.01f ? 1f : prevTimeScale;
        Cursor.lockState = prevLock;
        Cursor.visible = prevCursorVisible;

        Action callback = currentCallback;
        currentCallback = null;
        current = null;

        if (pendingAbility != AbilityType.None && AbilitySystem.Instance != null)
            AbilitySystem.Instance.Grant(pendingAbility);
        pendingAbility = AbilityType.None;

        if (callback != null) callback();
    }
}
