using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gameplay sound effects for StoryAudio (eating, growing, getting hurt, abilities, pearls, the gate, UI).
/// Like the rest of StoryAudio everything is synthesised in code: layered sweeps, filtered noise and a little
/// echo so the sounds feel "underwater" without needing any audio files.
/// </summary>
public partial class StoryAudio
{
    private AudioSource[] sfxPool;
    private int poolIndex;
    private readonly Dictionary<StorySfx, float> volumes = new Dictionary<StorySfx, float>();
    private readonly Dictionary<StorySfx, float> lastPlayed = new Dictionary<StorySfx, float>();

    private void BuildSfxPool()
    {
        sfxPool = new AudioSource[8];
        for (int i = 0; i < sfxPool.Length; i++) sfxPool[i] = MakeSource("Sfx " + i, false);
    }

    private void PlayInternal(StorySfx sound)
    {
        AudioClip clip;
        if (!sfx.TryGetValue(sound, out clip)) return;

        float now = Time.unscaledTime, last;
        if (lastPlayed.TryGetValue(sound, out last) && now - last < MinGap(sound)) return;
        lastPlayed[sound] = now;

        float volume;
        if (!volumes.TryGetValue(sound, out volume)) volume = 0.6f;

        AudioSource src = sfxPool != null ? sfxPool[poolIndex] : sfxSource;
        if (sfxPool != null) poolIndex = (poolIndex + 1) % sfxPool.Length;

        bool varied = sound == StorySfx.Eat || sound == StorySfx.UIClick || sound == StorySfx.UIHover || sound == StorySfx.LureDrop;
        src.pitch = varied ? Random.Range(0.92f, 1.1f) : 1f;
        src.PlayOneShot(clip, volume);
    }

    private static float MinGap(StorySfx sound)
    {
        switch (sound)
        {
            case StorySfx.Alert: return 3f;
            case StorySfx.Eat: return 0.08f;
            case StorySfx.UIHover: return 0.05f;
            default: return 0.03f;
        }
    }

    private void BuildGameplaySfx()
    {
        Add(StorySfx.Eat, MakeEat(), 0.55f);
        Add(StorySfx.LevelUp, MakeLevelUp(), 0.6f);
        Add(StorySfx.Hurt, MakeHurt(), 0.75f);
        Add(StorySfx.Zap, MakeZap(), 0.6f);
        Add(StorySfx.AbilityGain, MakeAbilityGain(), 0.55f);
        Add(StorySfx.Inflate, MakeInflate(), 0.5f);
        Add(StorySfx.Ink, MakeInk(), 0.5f);
        Add(StorySfx.SpeedBoost, MakeSpeedBoost(), 0.45f);
        Add(StorySfx.Shock, MakeShock(), 0.7f);
        Add(StorySfx.AbilityEnd, MakeAbilityEnd(), 0.35f);
        Add(StorySfx.Pearl, MakePearl(), 0.5f);
        Add(StorySfx.GateRumble, MakeRumble(), 0.7f);
        Add(StorySfx.Beacon, MakeBeacon(), 0.5f);
        Add(StorySfx.LureDrop, MakeLureDrop(), 0.5f);
        Add(StorySfx.Alert, MakeAlert(), 0.45f);
        Add(StorySfx.UIClick, MakeUiClick(), 0.5f);
        Add(StorySfx.UIHover, MakeUiHover(), 0.22f);
    }

    private void Add(StorySfx id, AudioClip clip, float volume)
    {
        sfx[id] = clip;
        volumes[id] = volume;
    }

    // ------------------------------------------------------------------ the sounds

    // a bubbly "pop" up followed by a little gulp down
    private static AudioClip MakeEat()
    {
        float[] d = Buf(0.35f);
        AddSweep(d, 0f, 320f, 980f, 0.10f, 0.8f, 28f, 0.2f);
        AddSweep(d, 0.06f, 260f, 130f, 0.12f, 0.6f, 22f, 0.3f);
        AddNoise(d, 0f, 0.05f, 0.35f, 0.15f, 2.5f, 60f, 3, 0.001f);
        return Finish("Eat", d, 0.7f);
    }

    // a sparkling rising arpeggio with echo
    private static AudioClip MakeLevelUp()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
        float[] d = Buf(1.9f);
        for (int k = 0; k < notes.Length; k++)
        {
            AddSweep(d, 0.09f * k, notes[k], notes[k], 0.7f, 0.5f, 4.5f, 0.3f);
            AddSweep(d, 0.09f * k, notes[k] * 2f, notes[k] * 2f, 0.4f, 0.12f, 8f, 0f);
        }
        AddEcho(d, 0.16f, 0.35f);
        return Finish("Level Up", d, 0.7f);
    }

    // a low descending thud with a detuned wobble: "ouch, you were eaten"
    private static AudioClip MakeHurt()
    {
        float[] d = Buf(1.1f);
        AddSweep(d, 0f, 210f, 48f, 0.7f, 1.0f, 3.2f, 0.5f);
        AddSweep(d, 0f, 222f, 52f, 0.7f, 0.6f, 3.2f, 0.5f);
        AddNoise(d, 0f, 0.4f, 0.2f, 0.03f, 3f, 6f, 7, 0.002f);
        return Finish("Hurt", d, 0.8f);
    }

    private static AudioClip MakeZap()
    {
        float[] d = Buf(0.55f);
        AddCrackle(d, 0f, 0.45f, 0.5f, 9, 5f);
        AddNoise(d, 0f, 0.3f, 0.7f, 0.5f, 0.6f, 10f, 11, 0.001f);
        return Finish("Zap", d, 0.7f);
    }

    private static AudioClip MakeAbilityGain()
    {
        float[] d = Buf(1.2f);
        AddSweep(d, 0f, 400f, 1600f, 0.35f, 0.5f, 3f, 0.2f);
        AddSweep(d, 0.28f, 1318.5f, 1318.5f, 0.6f, 0.45f, 5f, 0.3f);
        AddSweep(d, 0.36f, 1760f, 1760f, 0.6f, 0.35f, 5f, 0.3f);
        AddEcho(d, 0.14f, 0.3f);
        return Finish("Ability Gain", d, 0.7f);
    }

    private static AudioClip MakeInflate()
    {
        float[] d = Buf(0.8f);
        AddSweep(d, 0f, 140f, 520f, 0.5f, 0.7f, 2.5f, 0.4f);
        AddSweep(d, 0.25f, 520f, 300f, 0.3f, 0.5f, 8f, 0.3f);
        AddNoise(d, 0f, 0.5f, 0.05f, 0.25f, 2.5f, 3f, 13, 0.05f);
        return Finish("Inflate", d, 0.7f);
    }

    private static AudioClip MakeInk()
    {
        float[] d = Buf(1.0f);
        AddNoise(d, 0f, 0.9f, 0.12f, 0.02f, 4f, 3.5f, 17, 0.01f);
        AddSweep(d, 0f, 300f, 90f, 0.5f, 0.5f, 5f, 0.2f);
        AddSweep(d, 0.3f, 500f, 900f, 0.08f, 0.3f, 30f, 0f);
        return Finish("Ink", d, 0.7f);
    }

    private static AudioClip MakeSpeedBoost()
    {
        float[] d = Buf(0.7f);
        AddNoise(d, 0f, 0.6f, 0.04f, 0.4f, 2.5f, 3f, 19, 0.08f);
        AddSweep(d, 0f, 280f, 1300f, 0.5f, 0.35f, 3f, 0.2f);
        return Finish("Speed Boost", d, 0.7f);
    }

    private static AudioClip MakeShock()
    {
        float[] d = Buf(1.2f);
        AddSweep(d, 0f, 110f, 34f, 0.9f, 1f, 3f, 0.6f);
        AddSweep(d, 0f, 2800f, 200f, 0.5f, 0.4f, 6f, 0.5f);
        AddCrackle(d, 0f, 0.6f, 0.35f, 21, 4f);
        AddNoise(d, 0f, 0.5f, 0.8f, 0.2f, 0.8f, 6f, 23, 0.001f);
        return Finish("Shock", d, 0.8f);
    }

    private static AudioClip MakeAbilityEnd()
    {
        float[] d = Buf(0.5f);
        AddSweep(d, 0f, 660f, 660f, 0.3f, 0.5f, 9f, 0.2f);
        AddSweep(d, 0.12f, 440f, 440f, 0.35f, 0.5f, 8f, 0.2f);
        return Finish("Ability End", d, 0.6f);
    }

    // a glassy bell with inharmonic partials and a long echo
    private static AudioClip MakePearl()
    {
        float[] d = Buf(2.6f);
        AddSweep(d, 0f, 1318.5f, 1318.5f, 1.6f, 0.5f, 2.6f, 0f);
        AddSweep(d, 0f, 1318.5f * 2.76f, 1318.5f * 2.76f, 0.8f, 0.18f, 5f, 0f);
        AddSweep(d, 0.1f, 1975.5f, 1975.5f, 1.5f, 0.35f, 3f, 0f);
        AddEcho(d, 0.19f, 0.4f);
        return Finish("Pearl", d, 0.7f);
    }

    // a deep trembling rumble for the rock gate
    private static AudioClip MakeRumble()
    {
        float[] d = Buf(2.6f);
        AddNoise(d, 0f, 2.5f, 0.02f, 0.02f, 8f, 0.5f, 29, 0.4f);
        AddSweep(d, 0f, 48f, 38f, 2.4f, 0.6f, 0.8f, 0.3f);
        for (int i = 0; i < d.Length; i++) d[i] *= 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 9f * i / Rate);
        return Finish("Gate Rumble", d, 0.8f);
    }

    private static AudioClip MakeBeacon()
    {
        float[] d = Buf(1.6f);
        AddSweep(d, 0f, 220f, 440f, 0.9f, 0.4f, 1.5f, 0.2f);
        AddSweep(d, 0.2f, 330f, 660f, 0.9f, 0.35f, 1.8f, 0.2f);
        AddSweep(d, 0.5f, 1318.5f, 1318.5f, 0.8f, 0.2f, 4f, 0f);
        AddEcho(d, 0.2f, 0.3f);
        return Finish("Beacon", d, 0.7f);
    }

    private static AudioClip MakeLureDrop()
    {
        float[] d = Buf(0.5f);
        AddSweep(d, 0f, 900f, 500f, 0.08f, 0.8f, 30f, 0f);
        AddSweep(d, 0.1f, 750f, 420f, 0.08f, 0.7f, 30f, 0f);
        AddSweep(d, 0.2f, 600f, 340f, 0.1f, 0.6f, 28f, 0f);
        return Finish("Lure Drop", d, 0.7f);
    }

    // a low, beating two-note sting with a faint sonar ping: a predator noticed you
    private static AudioClip MakeAlert()
    {
        float[] d = Buf(1.3f);
        AddSweep(d, 0f, 110f, 98f, 0.6f, 0.8f, 3.5f, 0.4f);
        AddSweep(d, 0f, 116f, 102f, 0.6f, 0.7f, 3.5f, 0.4f);
        AddSweep(d, 0.35f, 82f, 70f, 0.7f, 0.8f, 3f, 0.4f);
        AddSweep(d, 0f, 1760f, 1760f, 0.5f, 0.08f, 6f, 0f);
        AddEcho(d, 0.3f, 0.4f);
        return Finish("Alert", d, 0.7f);
    }

    private static AudioClip MakeUiClick()
    {
        float[] d = Buf(0.12f);
        AddSweep(d, 0f, 520f, 880f, 0.07f, 0.8f, 45f, 0.2f);
        return Finish("UI Click", d, 0.6f);
    }

    private static AudioClip MakeUiHover()
    {
        float[] d = Buf(0.06f);
        AddSweep(d, 0f, 900f, 1100f, 0.04f, 0.5f, 70f, 0f);
        return Finish("UI Hover", d, 0.5f);
    }

    // ------------------------------------------------------------------ synthesis helpers

    private static float[] Buf(float seconds) { return new float[(int)(Rate * seconds)]; }

    /// <summary>A tone that glides from f0 to f1 (equal values = a steady note) with an exponential decay.</summary>
    private static void AddSweep(float[] d, float startSec, float f0, float f1, float len, float gain, float decay, float harmonic)
    {
        int start = (int)(startSec * Rate);
        int n = (int)(len * Rate);
        float phase = 0f;
        for (int j = 0; j < n && start + j < d.Length; j++)
        {
            float t = j / (float)Rate;
            float f = f0 * Mathf.Pow(f1 / f0, t / len);
            phase += 2f * Mathf.PI * f / Rate;
            float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 800f);
            d[start + j] += gain * env * (Mathf.Sin(phase) + harmonic * Mathf.Sin(2f * phase));
        }
    }

    /// <summary>Low-passed noise (cutoff 0..1 sweeps from c0 to c1) with an attack and exponential decay.</summary>
    private static void AddNoise(float[] d, float startSec, float len, float c0, float c1, float gain, float decay, int seed, float attack)
    {
        System.Random rng = new System.Random(seed);
        int start = (int)(startSec * Rate);
        int n = (int)(len * Rate);
        float y = 0f;
        for (int j = 0; j < n && start + j < d.Length; j++)
        {
            float t = j / (float)Rate;
            float c = Mathf.Lerp(c0, c1, t / len);
            float x = (float)(rng.NextDouble() * 2.0 - 1.0);
            y += c * (x - y);
            float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t / Mathf.Max(attack, 0.0005f));
            d[start + j] += y * gain * env;
        }
    }

    /// <summary>Electric crackle: a buzzy tone that jumps to a new random pitch every few milliseconds.</summary>
    private static void AddCrackle(float[] d, float startSec, float len, float gain, int seed, float decay)
    {
        System.Random rng = new System.Random(seed);
        int start = (int)(startSec * Rate);
        int n = (int)(len * Rate);
        int seg = (int)(Rate * 0.012f);
        float f = 800f, phase = 0f;
        for (int j = 0; j < n && start + j < d.Length; j++)
        {
            if (j % seg == 0) f = Mathf.Lerp(300f, 2200f, (float)rng.NextDouble());
            phase += 2f * Mathf.PI * f / Rate;
            float t = j / (float)Rate;
            float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 500f);
            float s = Mathf.Sin(phase);
            d[start + j] += gain * env * (Mathf.Sign(s) * 0.5f + s * 0.5f);
        }
    }

    private static void AddEcho(float[] d, float delaySec, float gain)
    {
        int delay = (int)(delaySec * Rate);
        for (int i = delay; i < d.Length; i++) d[i] += d[i - delay] * gain;
    }

    private static AudioClip Finish(string name, float[] d, float peak)
    {
        int fade = Mathf.Min(d.Length, (int)(Rate * 0.02f));
        for (int i = 0; i < fade; i++) d[d.Length - 1 - i] *= i / (float)fade;
        Normalize(d, peak);
        return ToClip(name, d);
    }
}
