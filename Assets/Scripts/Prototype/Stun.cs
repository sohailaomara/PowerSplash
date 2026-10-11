using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporarily freezes a creature's AI (PreyFish / PredatorFish) and tints it.
/// Used by the Shock ability and by the Electric Defense shield.
/// </summary>
public class Stun : MonoBehaviour
{
    private readonly List<Behaviour> disabled = new List<Behaviour>();
    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private float remaining;
    private bool active;

    public bool IsActive { get { return active; } }

    public static bool IsStunned(GameObject go)
    {
        if (go == null) return false;
        Stun s = go.GetComponent<Stun>();
        return s != null && s.active;
    }

    public static void Apply(GameObject go, float seconds)
    {
        if (go == null) return;
        Stun s = go.GetComponent<Stun>();
        if (s == null) s = go.AddComponent<Stun>();
        s.Begin(seconds);
    }

    private void Begin(float seconds)
    {
        remaining = Mathf.Max(remaining, seconds);
        if (active) return;
        active = true;

        PreyFish prey = GetComponent<PreyFish>();
        PredatorFish pred = GetComponent<PredatorFish>();
        if (prey != null && prey.enabled) { prey.enabled = false; disabled.Add(prey); }
        if (pred != null && pred.enabled) { pred.enabled = false; disabled.Add(pred); }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        Fx.Burst(transform.position, new Color(1f, 0.95f, 0.3f), 18, 0.35f, 4f, 0.6f, transform);
    }

    private void Update()
    {
        if (!active) return;
        remaining -= Time.deltaTime;

        // flicker yellow while stunned
        if (renderers != null)
        {
            bool on = Mathf.PingPong(Time.time * 8f, 1f) > 0.5f;
            Color c = on ? new Color(1f, 0.9f, 0.2f) : Color.white;
            foreach (Renderer r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", c);
                block.SetColor("_Color", c);
                r.SetPropertyBlock(block);
            }
        }

        if (remaining <= 0f) End();
    }

    private void End()
    {
        active = false;
        foreach (Behaviour b in disabled) if (b != null) b.enabled = true;
        disabled.Clear();
        if (renderers != null)
        {
            foreach (Renderer r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                r.SetPropertyBlock(null);
            }
        }
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (active) { foreach (Behaviour b in disabled) if (b != null) b.enabled = true; }
    }
}
