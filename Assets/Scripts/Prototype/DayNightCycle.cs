using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lighting behaviour 1: time-based day / night cycle driven from the Directional Light.
/// Sun colour, intensity and angle, ambient light, fog and the camera background all
/// follow the time of day. T toggles fast-forward.
/// </summary>
[RequireComponent(typeof(Light))]
public class DayNightCycle : MonoBehaviour
{
    public static string TimeString { get; private set; }

    [Header("Timing")]
    [SerializeField] private float cycleSeconds = 120f;
    [SerializeField, Range(0f, 1f)] private float startTime = 0.35f;   // 0 = midnight, 0.5 = noon
    [SerializeField] private float fastForward = 12f;

    [Header("Colours")]
    [SerializeField] private Color dayColor = new Color(1f, 0.97f, 0.85f);
    [SerializeField] private Color duskColor = new Color(1f, 0.5f, 0.25f);
    [SerializeField] private Color nightColor = new Color(0.25f, 0.35f, 0.85f);
    [SerializeField] private Color dayAmbient = new Color(0.25f, 0.45f, 0.55f);
    [SerializeField] private Color nightAmbient = new Color(0.02f, 0.04f, 0.1f);
    [SerializeField] private Color dayFog = new Color(0.1f, 0.45f, 0.6f);
    [SerializeField] private Color nightFog = new Color(0.01f, 0.03f, 0.08f);

    [Header("Look")]
    [SerializeField] private bool underwaterLook = true;   // solid fog-coloured background + fog
    [SerializeField] private float baseFogDensity = 0.007f;

    private Light sun;
    private Camera cam;
    private float t;
    private bool fast;

    private void Awake()
    {
        sun = GetComponent<Light>();
        t = startTime;
        TimeString = Format(t);
    }

    private void Start()
    {
        cam = Camera.main;
        if (underwaterLook)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.tKey.wasPressedThisFrame)
        {
            fast = !fast;
            GameHUD.Message(fast ? "Time fast-forward ON" : "Time fast-forward OFF", 1.5f);
        }

        t = Mathf.Repeat(t + Time.deltaTime / cycleSeconds * (fast ? fastForward : 1f), 1f);
        TimeString = Format(t);

        float sunHeight = Mathf.Sin((t - 0.25f) * Mathf.PI * 2f);           // +1 noon ... -1 midnight
        float day = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((sunHeight + 0.15f) / 0.5f));
        float dusk = Mathf.Clamp01(1f - Mathf.Abs(sunHeight) * 3.5f);
        float dark = LightZone.Darkness;                                    // extra darkness inside The Deep

        Color c = Color.Lerp(nightColor, dayColor, day);
        c = Color.Lerp(c, duskColor, dusk * 0.7f);
        sun.color = c;
        sun.intensity = Mathf.Lerp(0.25f, 2f, day) * (1f - 0.65f * dark);

        float elevation = Mathf.Lerp(18f, 80f, Mathf.Clamp01(sunHeight));
        float yaw = 330f + (t - 0.5f) * 140f;
        transform.rotation = Quaternion.Euler(elevation, yaw, 0f);

        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, day) * (1f - 0.7f * dark);

        if (underwaterLook)
        {
            Color fog = Color.Lerp(nightFog, dayFog, day) * (1f - 0.75f * dark);
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = baseFogDensity + 0.014f * dark;
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fog;
            }
        }
    }

    private static string Format(float time01)
    {
        float hours = time01 * 24f;
        int h = Mathf.FloorToInt(hours) % 24;
        int m = Mathf.FloorToInt((hours - Mathf.Floor(hours)) * 60f);
        return h.ToString("00") + ":" + m.ToString("00");
    }
}
