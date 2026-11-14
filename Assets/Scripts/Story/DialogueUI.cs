using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dialogue DISPLAY: a bottom panel with the speaker's portrait, name and text, plus up to four choice rows.
/// It only shows what DialogueManager tells it to show and has no story logic of its own.
/// The character's voice (name + portrait) is what the player reads, so every line carries both.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    private const int MaxChoices = 4;

    private GameObject canvasGO;
    private Image portrait, portraitFrame;
    private Text speakerText, bodyText, hintText;

    private readonly List<Image> choiceBgs = new List<Image>();
    private readonly List<Text> choiceTexts = new List<Text>();
    private int choiceCount;

    private void Awake()
    {
        Build();
        SetVisible(false);
    }

    private void Build()
    {
        Canvas canvas = StoryUI.MakeCanvas("Dialogue Canvas", 150, transform);
        canvasGO = canvas.gameObject;
        Transform root = canvasGO.transform;

        Vector2 bottom = new Vector2(0.5f, 0f);
        Image panel = StoryUI.MakeImage("Dialogue Panel", root, bottom, new Vector2(0f, 40f), new Vector2(1560f, 270f), new Color(0.02f, 0.07f, 0.13f, 0.94f));
        StoryUI.MakeImage("Accent", panel.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1560f, 5f), new Color(0.35f, 0.85f, 0.95f, 1f));

        Vector2 left = new Vector2(0f, 0.5f);
        portraitFrame = StoryUI.MakeImage("Portrait Frame", panel.transform, left, new Vector2(28f, 0f), new Vector2(216f, 216f), new Color(0.35f, 0.85f, 0.95f, 1f));
        portrait = StoryUI.MakeImage("Portrait", panel.transform, left, new Vector2(34f, 0f), new Vector2(204f, 204f), Color.white);
        portrait.preserveAspect = true;

        Vector2 topLeft = new Vector2(0f, 1f);
        speakerText = StoryUI.MakeText("Speaker", panel.transform, topLeft, new Vector2(276f, -22f), new Vector2(1000f, 44f), 38, TextAnchor.UpperLeft);
        speakerText.color = new Color(1f, 0.9f, 0.4f);
        bodyText = StoryUI.MakeText("Body", panel.transform, topLeft, new Vector2(276f, -78f), new Vector2(1240f, 160f), 32, TextAnchor.UpperLeft, true);
        hintText = StoryUI.MakeText("Hint", panel.transform, new Vector2(1f, 0f), new Vector2(-28f, 14f), new Vector2(520f, 30f), 22, TextAnchor.LowerRight);
        hintText.color = new Color(0.75f, 0.95f, 1f, 0.9f);

        for (int i = 0; i < MaxChoices; i++)
        {
            Image bg = StoryUI.MakeImage("Choice " + i, root, bottom, new Vector2(0f, 330f), new Vector2(1200f, 58f), new Color(0.04f, 0.14f, 0.22f, 0.95f));
            Text t = StoryUI.MakeText("Choice Text", bg.transform, left, new Vector2(24f, 0f), new Vector2(1150f, 50f), 30, TextAnchor.MiddleLeft);
            choiceBgs.Add(bg);
            choiceTexts.Add(t);
            bg.gameObject.SetActive(false);
        }
    }

    public void SetVisible(bool visible) { canvasGO.SetActive(visible); }

    public void SetSpeaker(string speaker, Sprite sprite)
    {
        speakerText.text = speaker ?? "";
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        portraitFrame.enabled = sprite != null;
    }

    public void SetBody(string text) { bodyText.text = text; }
    public void SetHint(string text) { hintText.text = text; }

    public void ShowChoices(string[] options, int selected)
    {
        choiceCount = Mathf.Min(options.Length, MaxChoices);
        for (int i = 0; i < MaxChoices; i++)
        {
            bool on = i < choiceCount;
            choiceBgs[i].gameObject.SetActive(on);
            if (!on) continue;
            // first option on top, stacked above the dialogue panel
            choiceBgs[i].rectTransform.anchoredPosition = new Vector2(0f, 330f + (choiceCount - 1 - i) * 68f);
            choiceTexts[i].text = (i + 1) + ".   " + options[i];
        }
        SetSelected(selected);
    }

    public void SetSelected(int selected)
    {
        for (int i = 0; i < choiceCount; i++)
        {
            bool on = i == selected;
            choiceBgs[i].color = on ? new Color(0.12f, 0.45f, 0.6f, 0.97f) : new Color(0.04f, 0.14f, 0.22f, 0.95f);
            choiceTexts[i].color = on ? new Color(1f, 0.95f, 0.5f) : Color.white;
        }
    }

    public void HideChoices()
    {
        choiceCount = 0;
        for (int i = 0; i < MaxChoices; i++) choiceBgs[i].gameObject.SetActive(false);
    }

    /// <summary>Index of the choice row under the mouse, or -1.</summary>
    public int ChoiceUnderMouse()
    {
        for (int i = 0; i < choiceCount; i++)
            if (StoryUI.PointerOver(choiceBgs[i].rectTransform)) return i;
        return -1;
    }
}
