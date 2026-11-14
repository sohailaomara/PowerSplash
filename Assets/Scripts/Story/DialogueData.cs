using System;
using System.Text;
using UnityEngine;

/// <summary>How a conversation is shown: the bottom dialogue box, or a full-screen text crawl.</summary>
public enum DialoguePresentation { Dialogue, Crawl }

/// <summary>One spoken line: who says it, their portrait and the text.</summary>
[Serializable]
public class DialogueLine
{
    public string speaker;
    public Sprite portrait;
    [TextArea(2, 5)] public string text;
}

/// <summary>A player choice shown after the last line. Picking it sets a story flag and can lead to another dialogue.</summary>
[Serializable]
public class DialogueChoice
{
    public string text;
    public DialogueData next;
    public string flagToSet;
}

/// <summary>
/// Story data kept out of the scripts: one asset per conversation, note or crawl.
/// Create via: Assets > Create > Dialogue > Dialogue Data.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public DialoguePresentation presentation = DialoguePresentation.Dialogue;
    public DialogueLine[] lines;
    public DialogueChoice[] choices;

    [Tooltip("Story flag set once this conversation (and its choice, if any) is finished, e.g. metMentor")]
    public string flagToSetWhenFinished;

    [Tooltip("Optional ability the player receives when this conversation finishes")]
    public AbilityType abilityWhenFinished = AbilityType.None;

    [Tooltip("If set, finishing this conversation records a journal entry with this title")]
    public string journalTitle;

    /// <summary>All line texts joined into one block (used for journal entries and loading-screen recaps).</summary>
    public string JoinedText()
    {
        if (lines == null) return "";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) sb.Append("\n");
            sb.Append(lines[i].text);
        }
        return sb.ToString();
    }
}
