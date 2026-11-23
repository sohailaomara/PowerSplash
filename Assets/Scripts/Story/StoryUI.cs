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

    public static Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, bool wrap = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = DefaultFont;
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
/// A vertical list of text buttons with mouse hover/click and keyboard navigation.
/// Used by the main menu and the pause menu. Call Tick() from Update.
/// </summary>
public class MenuList
{
    private readonly List<Text> labels = new List<Text>();
    private readonly List<Action> actions = new List<Action>();
    private readonly string[] names;
    private int selected;
    private Vector2 lastMouse;

    public MenuList(Transform parent, string[] names, Action[] callbacks, Vector2 anchor, Vector2 firstPos, float spacing, Vector2 size, int fontSize)
    {
        this.names = names;
        for (int i = 0; i < names.Length; i++)
        {
            Text t = StoryUI.MakeText("Menu Item " + i, parent, anchor, firstPos + new Vector2(0f, -spacing * i), size, fontSize, TextAnchor.MiddleCenter);
            labels.Add(t);
            actions.Add(callbacks[i]);
        }
        Refresh();
    }

    public void Reset() { selected = 0; Refresh(); }

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
                for (int i = 0; i < labels.Count; i++)
                    if (StoryUI.PointerOver(labels[i].rectTransform)) selected = i;
            }
        }

        if (k != null)
        {
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) selected = (selected + 1) % labels.Count;
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) selected = (selected - 1 + labels.Count) % labels.Count;
        }

        bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                       (m != null && m.leftButton.wasPressedThisFrame && StoryUI.PointerOver(labels[selected].rectTransform));

        Refresh();
        if (confirm && actions[selected] != null) actions[selected]();
    }

    private void Refresh()
    {
        for (int i = 0; i < labels.Count; i++)
        {
            bool on = i == selected;
            labels[i].text = on ? ">  " + names[i] + "  <" : names[i];
            labels[i].color = on ? new Color(1f, 0.9f, 0.4f) : new Color(0.85f, 0.95f, 1f);
        }
    }
}
