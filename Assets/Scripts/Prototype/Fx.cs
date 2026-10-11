using System.Collections;
using UnityEngine;

/// <summary>Small shared helpers for particle bursts and light flashes used by the prototype.</summary>
public static class Fx
{
    private static Material spriteMat;

    public static Material SpriteMaterial
    {
        get
        {
            if (spriteMat == null)
            {
                spriteMat = new Material(Shader.Find("Sprites/Default"));
                spriteMat.name = "FxSprite (runtime)";
            }
            return spriteMat;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { spriteMat = null; }

    /// <summary>One-shot particle burst (bubbles, sparks, ink puffs).</summary>
    public static ParticleSystem Burst(Vector3 position, Color color, int count, float size, float speed, float lifetime, Transform parent = null)
    {
        GameObject go = new GameObject("FX Burst");
        go.transform.position = position;
        if (parent != null) go.transform.SetParent(parent, true);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = SpriteMaterial;
        ps.Play();
        Object.Destroy(go, lifetime + 1.5f);
        return ps;
    }

    /// <summary>Continuous particle emitter (ink cloud, bubble stream). Caller owns the returned object.</summary>
    public static ParticleSystem Emitter(Transform parent, Vector3 localPos, Color color, float rate, float size, float lifetime, float radius, float speed)
    {
        GameObject go = new GameObject("FX Emitter");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = SpriteMaterial;
        ps.Play();
        return ps;
    }

    /// <summary>Short point-light flash that fades out and destroys itself.</summary>
    public static void Flash(Vector3 position, Color color, float range, float intensity, float duration)
    {
        GameObject go = new GameObject("FX Flash");
        go.transform.position = position;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        go.AddComponent<FxLightFader>().Begin(intensity, duration);
    }
}

public class FxLightFader : MonoBehaviour
{
    private float startIntensity;
    private float duration;

    public void Begin(float intensity, float seconds)
    {
        startIntensity = intensity;
        duration = Mathf.Max(0.05f, seconds);
        StartCoroutine(Fade());
    }

    private IEnumerator Fade()
    {
        Light l = GetComponent<Light>();
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            l.intensity = Mathf.Lerp(startIntensity, 0f, t / duration);
            yield return null;
        }
        Destroy(gameObject);
    }
}
