using System;
using UnityEngine;

/// <summary>An objective shown in the tracker until its flag is set. The first unfinished rule is the current objective.</summary>
[Serializable]
public class ObjectiveRule
{
    public string completedWhenFlag;
    [TextArea(1, 3)] public string text;
}

/// <summary>A lore item shown in the journal's inventory section once its flag is set (empty flag = always).</summary>
[Serializable]
public class StoryItem
{
    public string requiredFlag;
    public string itemName;
    [TextArea(2, 4)] public string description;
    public bool showPearlCount;
}

/// <summary>
/// Story condition for leaving a level. Growing to the next size tier is not enough:
/// every required flag must also be set. completeFlag is set when the level is left.
/// </summary>
[Serializable]
public class LevelGate
{
    public int fromTier;
    public string[] requiredFlags;
    [TextArea(2, 4)] public string holdMessage;
    public string completeFlag;
    public string recapAsset;
    public string recapAltFlag;
    public string recapAltAsset;
}

/// <summary>Flags and journal entries assumed when a level is opened directly (level select, or pressing Play in a later scene).</summary>
[Serializable]
public class LevelSeed
{
    public int tier;
    public string[] flags;
    public string[] journalAssets;
}

/// <summary>
/// The story's rules as data: objectives, lore items, level gates and level-select seeds.
/// Lives at Resources/Story/StoryTable so the story systems can load it at runtime.
/// </summary>
[CreateAssetMenu(fileName = "StoryTable", menuName = "Dialogue/Story Table")]
public class StoryTable : ScriptableObject
{
    public ObjectiveRule[] objectives;
    public StoryItem[] items;
    public LevelGate[] gates;
    public LevelSeed[] seeds;

    /// <summary>Text of the first objective whose flag is not set yet (empty if there are no objectives).</summary>
    public string CurrentObjective()
    {
        if (objectives == null) return "";
        for (int i = 0; i < objectives.Length; i++)
            if (!StoryStateManager.Has(objectives[i].completedWhenFlag)) return objectives[i].text;
        return "Your story is complete.";
    }

    public LevelGate GateFor(int tier)
    {
        if (gates == null) return null;
        for (int i = 0; i < gates.Length; i++)
            if (gates[i].fromTier == tier) return gates[i];
        return null;
    }
}
