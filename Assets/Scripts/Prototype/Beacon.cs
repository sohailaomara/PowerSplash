using UnityEngine;

/// <summary>
/// Reef Beacon: a switch the player toggles with R.
/// Changes the world state (coral lights fade on, bubbles stream from the pedestal)
/// and triggers a Cinemachine cinematic zoom on the beacon.
/// </summary>
public class Beacon : Interactable
{
    [Header("Wiring (filled by the scene setup)")]
    public Light[] coralLights;
    public Renderer lamp;

    [Header("Tuning")]
    public float onIntensity = 7f;
    public float fadeSeconds = 1.5f;
    public Color lampOnColor = new Color(0.3f, 1f, 1f);

    private bool on;
    private float amount;
    private Material lampMat;
    private ParticleSystem bubbles;

    private void Start()
    {
        prompt = "Switch reef lights ON";
        if (lamp != null) lampMat = lamp.material;
        foreach (Light l in coralLights) { if (l != null) { l.intensity = 0f; l.enabled = false; } }

        bubbles = Fx.Emitter(transform, Vector3.up * 2.2f, new Color(0.8f, 1f, 1f, 0.8f), 18f, 0.35f, 3f, 0.4f, 1.2f);
        var em = bubbles.emission; em.enabled = false;
        SetLamp(0f);
    }

    public override void Interact()
    {
        on = !on;
        prompt = on ? "Switch reef lights OFF" : "Switch reef lights ON";
        var em = bubbles.emission; em.enabled = on;

        if (on)
        {
            GameHUD.Message("The reef beacon lights up the corals!", 3f);
            Fx.Burst(transform.position + Vector3.up * 2f, lampOnColor, 25, 0.5f, 5f, 1f);
            if (CameraDirector.Instance != null)
                CameraDirector.Instance.PlayCinematic(transform, 3.5f, new Vector3(-9f, 4f, -11f), 10f);
        }
        else
        {
            GameHUD.Message("Reef lights off", 2f);
        }
    }

    private void Update()
    {
        amount = Mathf.MoveTowards(amount, on ? 1f : 0f, Time.deltaTime / Mathf.Max(0.05f, fadeSeconds));
        float shimmer = 1f + 0.12f * Mathf.Sin(Time.time * 3f);
        foreach (Light l in coralLights)
        {
            if (l == null) continue;
            l.enabled = amount > 0.01f;
            l.intensity = onIntensity * amount * shimmer;
        }
        SetLamp(amount);
    }

    private void SetLamp(float a)
    {
        if (lampMat == null) return;
        lampMat.SetColor("_EmissionColor", lampOnColor * (0.25f + 2.5f * a));
    }
}
