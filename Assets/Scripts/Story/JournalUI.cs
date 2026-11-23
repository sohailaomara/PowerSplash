using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// The journal / codex (press J). Left: the inventory, with a short lore description for every item.
/// Right: discoveries recorded from found notes. It pauses the game while open.
/// </summary>
public class JournalUI : MonoBehaviour
{
    private GameObject canvasGO;
    private Text itemsText, notesText;
    private bool open;
    private float prevTimeScale = 1f;

    private void Awake() { Build(); }

    private void Build()
    {
        Canvas canvas = StoryUI.MakeCanvas("Journal Canvas", 160, transform);
        canvasGO = canvas.gameObject;
        Transform root = canvasGO.transform;

        StoryUI.MakeFullscreenImage("Dim", root, new Color(0f, 0.02f, 0.05f, 0.6f));
        Vector2 mid = new Vector2(0.5f, 0.5f);
        Image panel = StoryUI.MakeImage("Journal Panel", root, mid, Vector2.zero, new Vector2(1560f, 880f), new Color(0.03f, 0.09f, 0.16f, 0.97f));
        StoryUI.MakeImage("Accent", panel.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1560f, 6f), new Color(0.35f, 0.85f, 0.95f, 1f));

        Text title = StoryUI.MakeText("Title", panel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(900f, 70f), 56, TextAnchor.UpperCenter);
        title.text = "JOURNAL";
        title.color = new Color(1f, 0.9f, 0.4f);

        Vector2 topLeft = new Vector2(0f, 1f);
        Text itemsHeader = StoryUI.MakeText("Items Header", panel.transform, topLeft, new Vector2(50f, -110f), new Vector2(600f, 40f), 32, TextAnchor.UpperLeft);
        itemsHeader.text = "INVENTORY";
        itemsHeader.color = new Color(0.5f, 0.92f, 1f);
        itemsText = StoryUI.MakeText("Items", panel.transform, topLeft, new Vector2(50f, -160f), new Vector2(660f, 640f), 25, TextAnchor.UpperLeft, true);

        Text notesHeader = StoryUI.MakeText("Notes Header", panel.transform, topLeft, new Vector2(790f, -110f), new Vector2(600f, 40f), 32, TextAnchor.UpperLeft);
        notesHeader.text = "DISCOVERIES";
        notesHeader.color = new Color(0.5f, 0.92f, 1f);
        notesText = StoryUI.MakeText("Notes", panel.transform, topLeft, new Vector2(790f, -160f), new Vector2(720f, 640f), 25, TextAnchor.UpperLeft, true);

        Text hint = StoryUI.MakeText("Hint", panel.transform, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(700f, 30f), 22, TextAnchor.LowerCenter);
        hint.text = "[J] or [ESC] close";
        hint.color = new Color(1f, 1f, 1f, 0.7f);

        canvasGO.SetActive(false);
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;

        if (open)
        {
            if (k.jKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame) Close();
            return;
        }

        if (!StoryStateManager.InLevel) return;
        if (k.jKey.wasPressedThisFrame && Time.timeScale > 0.01f && !DialogueManager.IsBusy && !CinematicManager.Busy && !LevelTransitionManager.Loading)
            Open();
    }

    private void Open()
    {
        open = true;
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Refresh();
        canvasGO.SetActive(true);
    }

    private void Close()
    {
        open = false;
        canvasGO.SetActive(false);
        Time.timeScale = prevTimeScale <= 0.01f ? 1f : prevTimeScale;
    }

    private void Refresh()
    {
        StringBuilder items = new StringBuilder();
        StoryTable table = StoryStateManager.Table;
        if (table != null && table.items != null)
        {
            foreach (StoryItem item in table.items)
            {
                if (!string.IsNullOrEmpty(item.requiredFlag) && !StoryStateManager.Has(item.requiredFlag)) continue;
                items.Append("<b>").Append(item.itemName);
                if (item.showPearlCount) items.Append("  (").Append(PlayerInventory.Pearls).Append(" / ").Append(PlayerInventory.PearlsRequired).Append(")");
                items.Append("</b>\n").Append(item.description).Append("\n\n");
            }
        }
        itemsText.text = items.Length > 0 ? items.ToString() : "Nothing yet.";

        StringBuilder notes = new StringBuilder();
        StoryStateManager s = StoryStateManager.Instance;
        if (s != null)
        {
            foreach (JournalEntry e in s.Journal)
                notes.Append("<b>").Append(e.title).Append("</b>\n").Append(e.body).Append("\n\n");
        }
        notesText.text = notes.Length > 0 ? notes.ToString() : "Nothing found yet.\nLook for glowing message bottles in the water.";
    }
}
