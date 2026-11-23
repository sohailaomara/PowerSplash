using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Accessibility options. Press F2 to switch the game to a colorblind-safe palette (Okabe-Ito colors).
/// The default palette uses red for danger and green for success, which many players cannot tell apart,
/// so in colorblind-safe mode "danger" becomes vermillion orange and "success" becomes sky blue.
/// Colors are never the only signal: the gate state is also written on the HUD ("gate open!"), the lose and win
/// screens carry big titles, and dialogue choices are numbered.
/// The choice is stored in PlayerPrefs, so it is remembered the next time the game starts.
/// </summary>
public class AccessibilityOptions : MonoBehaviour
{
    private const string PrefKey = "ps_colorblind_safe";

    /// <summary>True while the colorblind-safe palette is active.</summary>
    public static bool ColorblindSafe { get; private set; }

    /// <summary>Raised whenever the palette is switched.</summary>
    public static event Action OnChanged;

    // ------------------------------------------------------------------ palette (read these instead of hard-coding colors)

    /// <summary>Danger / sealed / predator color: red by default, vermillion in colorblind-safe mode.</summary>
    public static Color Danger { get { return ColorblindSafe ? new Color(0.84f, 0.37f, 0.0f) : new Color(1f, 0.25f, 0.15f); } }

    /// <summary>Success / open / safe color: green by default, sky blue in colorblind-safe mode.</summary>
    public static Color Safe { get { return ColorblindSafe ? new Color(0.34f, 0.71f, 0.91f) : new Color(0.3f, 1f, 0.4f); } }

    /// <summary>Growth bar color.</summary>
    public static Color Growth { get { return ColorblindSafe ? new Color(0.34f, 0.71f, 0.91f) : new Color(0.2f, 0.9f, 0.5f); } }

    /// <summary>Ability timer bar color.</summary>
    public static Color Ability { get { return ColorblindSafe ? new Color(0.94f, 0.89f, 0.26f) : new Color(1f, 0.75f, 0.2f); } }

    /// <summary>Background of the lose screen.</summary>
    public static Color LoseBackground { get { return ColorblindSafe ? new Color(0.35f, 0.14f, 0.0f, 0.82f) : new Color(0.45f, 0.02f, 0.02f, 0.78f); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { ColorblindSafe = false; OnChanged = null; }

    private void Awake()
    {
        ColorblindSafe = PlayerPrefs.GetInt(PrefKey, 0) == 1;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnChanged += ApplyToHud;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        OnChanged -= ApplyToHud;
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null || !k.f2Key.wasPressedThisFrame) return;

        ColorblindSafe = !ColorblindSafe;
        PlayerPrefs.SetInt(PrefKey, ColorblindSafe ? 1 : 0);
        PlayerPrefs.Save();
        GameHUD.Message("Colorblind-safe colors: " + (ColorblindSafe ? "ON" : "OFF") + "   (F2 to switch)", 3f);
        if (OnChanged != null) OnChanged();
    }

    /// <summary>The HUD is built shortly after a level opens, so recolor it once it exists.</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (ColorblindSafe) StartCoroutine(ApplyLater());
    }

    private IEnumerator ApplyLater()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        ApplyToHud();
    }

    /// <summary>Recolors the HUD bars and the lose screen (found by name, including inactive objects).</summary>
    private void ApplyToHud()
    {
        Image[] images = Resources.FindObjectsOfTypeAll<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (!img.gameObject.scene.IsValid()) continue;   // skip assets, keep only scene objects

            string n = img.gameObject.name;
            if (n == "GrowthBar") img.color = Growth;
            else if (n == "AbilityBar") img.color = Ability;
            else if (n == "Lose Screen") img.color = LoseBackground;
        }
    }
}
