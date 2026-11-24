using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Small helpers shared by every runtime-built story UI (dialogue, objectives, journal, menus).
/// Uses the same legacy uGUI Text approach as GameHUD, so no extra font or TMP import is needed.
/// All input helpers use unscaled time and the new Input System because the story pauses the game.
/// </summary>
public static class StoryUI
{
    private static Font font;

    public static Font DefaultFont
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    private static Font themeFont;

    /// <summary>Chunky display font used by the main menu (Assets/Resources/Fonts). Falls back to the default font.</summary>
    public static Font ThemeFont
    {
        get
        {
            if (themeFont == null) themeFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            return themeFont != null ? themeFont : DefaultFont;
        }
    }

    private static Font bodyFont;

    /// <summary>Clean rounded font for paragraphs and small text (Assets/Resources/Fonts). Falls back to the default font.</summary>
    public static Font BodyFont
    {
        get
        {
            if (bodyFont == null) bodyFont = Resources.Load<Font>("Fonts/VarelaRound-Regular");
            return bodyFont != null ? bodyFont : DefaultFont;
        }
    }

    /// <summary>Creates a screen-space overlay canvas that scales with a 1920x1080 reference.</summary>
    public static Canvas MakeCanvas(string name, int sortingOrder, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    /// <summary>Image whose anchor, pivot and position all use the same anchor point.</summary>
    public static Image MakeImage(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    public static Image MakeFullscreenImage(string name, Transform parent, Color color)
    {
        Image img = MakeImage(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, color);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return img;
    }

    /// <summary>A rounded glass panel: a coloured border with a translucent fill inside it. Returns the fill (parent children to it).</summary>
    public static Image MakeGlass(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fill, Color border, out Image borderImage)
    {
        borderImage = MakeImage(name + " Border", parent, anchor, pos, size, border);
        borderImage.sprite = MenuArt.Panel;
        borderImage.type = Image.Type.Sliced;
        Image f = MakeImage(name, borderImage.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(6f, 6f), fill);
        f.sprite = MenuArt.Panel;
        f.type = Image.Type.Sliced;
        return f;
    }

    public static Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, bool wrap = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = fontSize >= 34 ? ThemeFont : BodyFont;   // display font for titles, rounded body font for the rest
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Color.white;
        t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Shadow s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.8f);
        s.effectDistance = new Vector2(2f, -2f);
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }

    public static bool PointerOver(RectTransform rt)
    {
        if (rt == null || Mouse.current == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Mouse.current.position.ReadValue(), null);
    }

    /// <summary>Space, Enter or left click (used to advance dialogue and cutscenes).</summary>
    public static bool ConfirmPressed()
    {
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) return true;
        return m != null && m.leftButton.wasPressedThisFrame;
    }

    public static bool EscapePressed()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }
}

/// <summary>
/// A vertical list of pill buttons with mouse hover/click and keyboard navigation, in the same style as the main menu.
/// Used by the pause menu. Call Tick() from Update.
/// </summary>
public class MenuList
{
    private class Item
    {
        public RectTransform rt;
        public Image border, fill;
        public Text label;
        public float hover;
    }

    private readonly List<Item> items = new List<Item>();
    private readonly List<Action> actions = new List<Action>();
    private int selected;
    private Vector2 lastMouse;

    public MenuList(Transform parent, string[] names, Action[] callbacks, Vector2 anchor, Vector2 firstPos, float spacing, Vector2 size, int fontSize)
    {
        Vector2 mid = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < names.Length; i++)
        {
            Item it = new Item();
            it.border = StoryUI.MakeImage("Menu Item " + i, parent, anchor, firstPos + new Vector2(0f, -spacing * i), size, Color.white);
            it.border.sprite = MenuArt.Rounded;
            it.border.type = Image.Type.Sliced;
            it.rt = it.border.rectTransform;
            it.fill = StoryUI.MakeImage("Fill", it.border.transform, mid, Vector2.zero, size - new Vector2(8f, 8f), Color.white);
            it.fill.sprite = MenuArt.Rounded;
            it.fill.type = Image.Type.Sliced;
            it.label = StoryUI.MakeText("Label", it.border.transform, mid, Vector2.zero, size, fontSize, TextAnchor.MiddleCenter);
            it.label.font = StoryUI.ThemeFont;
            it.label.text = names[i];
            Shadow sh = it.label.GetComponent<Shadow>();
            if (sh != null) sh.enabled = false;
            items.Add(it);
            actions.Add(callbacks[i]);
        }
        Refresh(1f);
    }

    private int lastHoverSfx;
    public void Reset() { selected = 0; lastHoverSfx = 0; Refresh(1f); }

    public void Tick()
    {
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;

        if (m != null)
        {
            Vector2 p = m.position.ReadValue();
            if ((p - lastMouse).sqrMagnitude > 1f)
            {
                lastMouse = p;
                for (int i = 0; i < items.Count; i++)
                    if (StoryUI.PointerOver(items[i].rt)) selected = i;
            }
        }

        if (k != null)
        {
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) selected = (selected + 1) % items.Count;
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) selected = (selected - 1 + items.Count) % items.Count;
        }

        bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                       (m != null && m.leftButton.wasPressedThisFrame && StoryUI.PointerOver(items[selected].rt));

        Refresh(Time.unscaledDeltaTime);
        if (selected != lastHoverSfx) { lastHoverSfx = selected; StoryAudio.Play(StorySfx.UIHover); }
        if (confirm && actions[selected] != null) { StoryAudio.Play(StorySfx.UIClick); actions[selected](); }
    }

    private void Refresh(float dt)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Item it = items[i];
            it.hover = Mathf.MoveTowards(it.hover, i == selected ? 1f : 0f, dt * 8f);
            float e = it.hover * it.hover * (3f - 2f * it.hover);
            it.fill.color = Color.Lerp(new Color(0.03f, 0.2f, 0.3f, 0.78f), new Color(0.42f, 0.9f, 1f, 0.97f), e);
            it.border.color = Color.Lerp(new Color(0.45f, 0.85f, 0.95f, 0.6f), Color.white, e);
            it.label.color = Color.Lerp(new Color(0.9f, 0.98f, 1f, 1f), new Color(0.03f, 0.1f, 0.18f, 1f), e);
            it.rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.05f, e);
        }
    }
}
