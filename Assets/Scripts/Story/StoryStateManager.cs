using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>A discovery recorded in the journal (found notes, lore).</summary>
[Serializable]
public class JournalEntry
{
    public string title;
    public string body;
}

/// <summary>
/// Holds the story flags (hasMetMentor, foundBottle1, act1Complete ...) and the journal.
/// Triggers and conversations SET flags; doors, NPC dialogue, objectives and level transitions READ them.
/// It lives on a DontDestroyOnLoad object ("Story Systems") that is created automatically when the game
/// starts, so story progress carries from level to level and the scenes need no wiring.
/// </summary>
public class StoryStateManager : MonoBehaviour
{
    public static StoryStateManager Instance { get; private set; }
    public static StoryTable Table { get; private set; }

    /// <summary>True while one of the three level scenes is open (false in the main menu).</summary>
    public static bool InLevel { get; private set; }
    public static int LevelTier { get; private set; }

    private readonly HashSet<string> flags = new HashSet<string>();
    private readonly List<JournalEntry> journal = new List<JournalEntry>();

    public IReadOnlyList<JournalEntry> Journal { get { return journal; } }

    public event Action<string> OnFlagSet;
    public event Action OnStateReset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        Table = null;
        InLevel = false;
        LevelTier = 1;
    }

    /// <summary>Creates the persistent story systems once, whatever scene the game starts in.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("Story Systems");
        DontDestroyOnLoad(go);
        go.AddComponent<StoryStateManager>();   // must come first: the others read it
        go.AddComponent<DialogueManager>();
        go.AddComponent<ObjectiveManager>();
        go.AddComponent<JournalUI>();
        go.AddComponent<CinematicManager>();
        go.AddComponent<LevelTransition>();
        go.AddComponent<PauseMenu>();
        go.AddComponent<StoryAudio>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Table = Resources.Load<StoryTable>("Story/StoryTable");
        SceneManager.sceneLoaded += OnSceneLoaded;
        HandleScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { HandleScene(scene); }

    /// <summary>
    /// A level opened without saved progress is a fresh start (new game, retry, level select, or pressing Play
    /// in the editor): flags are cleared and the story up to that level is assumed.
    /// A level opened with saved progress (a normal level change) keeps every flag.
    /// </summary>
    private void HandleScene(Scene scene)
    {
        int index = scene.buildIndex - PlayerProgress.FirstLevelBuildIndex;
        InLevel = index >= 0 && index < PlayerProgress.MaxTier;
        if (!InLevel) return;

        LevelTier = index + 1;
        if (!PlayerProgress.HasData)
        {
            ResetAll();
            SeedForTier(LevelTier);
            if (OnStateReset != null) OnStateReset();   // refresh objectives after the seeded flags
        }
    }

    private void Update()
    {
        // Collecting every Ancient Pearl of a level sets pearlsNCollected (opens story beats for that level)
        if (InLevel && PlayerInventory.Pearls >= PlayerInventory.PearlsRequired)
            SetFlag("pearls" + LevelTier + "Collected");
    }

    // ------------------------------------------------------------------ flags

    public static bool Has(string flag)
    {
        if (string.IsNullOrEmpty(flag) || Instance == null) return false;
        return Instance.flags.Contains(flag);
    }

    public static bool HasAll(string[] required)
    {
        if (required == null) return true;
        for (int i = 0; i < required.Length; i++)
            if (!Has(required[i])) return false;
        return true;
    }

    public static void Set(string flag)
    {
        if (Instance != null) Instance.SetFlag(flag);
    }

    public void SetFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return;
        if (flags.Add(flag))
        {
            Debug.Log("[Story] flag set: " + flag);
            if (OnFlagSet != null) OnFlagSet(flag);
        }
    }

    public void ResetAll()
    {
        flags.Clear();
        journal.Clear();
        if (OnStateReset != null) OnStateReset();
    }

    // ------------------------------------------------------------------ journal

    public void AddJournalFromData(DialogueData data, bool announce)
    {
        if (data == null || string.IsNullOrEmpty(data.journalTitle)) return;
        for (int i = 0; i < journal.Count; i++)
            if (journal[i].title == data.journalTitle) return;

        journal.Add(new JournalEntry { title = data.journalTitle, body = data.JoinedText() });
        if (announce) GameHUD.Message("Journal updated  -  press J to read it", 3.5f);
    }

    // ------------------------------------------------------------------ level select / direct start

    private void SeedForTier(int tier)
    {
        if (Table == null || Table.seeds == null) return;
        foreach (LevelSeed seed in Table.seeds)
        {
            if (seed.tier > tier) continue;
            if (seed.flags != null)
                foreach (string f in seed.flags) flags.Add(f);
            if (seed.journalAssets != null)
                foreach (string a in seed.journalAssets)
                    AddJournalFromData(Resources.Load<DialogueData>("Story/" + a), false);
        }
    }
}
