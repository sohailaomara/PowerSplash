using UnityEngine;

/// <summary>
/// Screenshake for important moments (a pearl ruin waking, a big story choice, the Shock ability, the Golden Light).
/// Call ScreenShake.Shake(strength, seconds) from anywhere. The shake is added AFTER the Cinemachine Brain has
/// placed the camera (high script execution order), so it works with all three virtual cameras and never
/// fights with the blends. Each frame the Brain sets the camera position again, so the offset never builds up.
/// Some beats shake automatically when their story flag is set.
/// </summary>
[DefaultExecutionOrder(10000)]
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    private float strength;
    private float remaining;
    private float total = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake() { Instance = this; }

    private void Start()
    {
        if (StoryStateManager.Instance != null) StoryStateManager.Instance.OnFlagSet += OnFlag;
    }

    private void OnDestroy()
    {
        if (StoryStateManager.Instance != null) StoryStateManager.Instance.OnFlagSet -= OnFlag;
        if (Instance == this) Instance = null;
    }

    /// <summary>Starts a shake. A new shake only replaces the current one if it is stronger.</summary>
    public static void Shake(float strength, float seconds)
    {
        if (Instance == null) return;
        if (strength < Instance.strength * (Instance.remaining / Instance.total)) return;
        Instance.strength = strength;
        Instance.total = Mathf.Max(0.05f, seconds);
        Instance.remaining = seconds;
    }

    /// <summary>Story beats that deserve a shake: ruins waking, the big choice, the Golden Light.</summary>
    private void OnFlag(string flag)
    {
        if (flag == "pearls1Collected" || flag == "pearls2Collected" || flag == "pearls3Collected") Shake(0.3f, 0.8f);
        else if (flag == "choiceMade") Shake(0.2f, 0.5f);
        else if (flag == "goldenLightTaken") Shake(0.6f, 2f);
    }

    private void LateUpdate()
    {
        if (remaining <= 0f) return;
        if (Time.timeScale < 0.01f) return;   // paused or in a conversation: hold the shake

        remaining -= Time.unscaledDeltaTime;
        Camera cam = Camera.main;
        if (cam == null) return;

        float fade = Mathf.Clamp01(remaining / total);   // strongest at the start, smooth stop
        cam.transform.position += Random.insideUnitSphere * strength * fade;
    }
}
