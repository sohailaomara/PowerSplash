using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Carries the player's progress between level scenes.
/// Each size tier lives in its own scene, in Build Settings order:
/// tier 1 = Level 1 (index 0), tier 2 = Level 2 (index 1), tier 3 = Level 3 (index 2).
/// The values saved here are the player's state when a level is ENTERED, so dying and
/// retrying a level restores that state instead of starting over from tier 1.
/// </summary>
public static class PlayerProgress
{
    public const int MaxTier = 3;

    public static bool HasData { get; private set; }
    public static float Growth { get; private set; }
    public static int Tier { get; private set; }
    public static AbilityType Stored { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Clear(); }

    public static void Clear()
    {
        HasData = false;
        Growth = 0f;
        Tier = 1;
        Stored = AbilityType.None;
    }

    public static void Save(float growth, int tier, AbilityType stored)
    {
        HasData = true;
        Growth = growth;
        Tier = tier;
        Stored = stored;
    }

    /// <summary>The size tier the currently open scene belongs to (1 to 3).</summary>
    public static int SceneTier()
    {
        int index = SceneManager.GetActiveScene().buildIndex;
        if (index < 0) return 1;
        return Mathf.Clamp(index + 1, 1, MaxTier);
    }

    /// <summary>Build Settings index of the scene that holds the given size tier.</summary>
    public static int SceneIndexForTier(int tier)
    {
        return Mathf.Clamp(tier, 1, MaxTier) - 1;
    }
}
