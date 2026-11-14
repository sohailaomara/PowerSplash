using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Sound effects the story systems can ask for.</summary>
public enum StorySfx { Pickup, ObjectiveUpdate, ChoiceMove, ChoiceConfirm, Whoosh, GoldenSwell }

/// <summary>
/// Audio for the story, generated in code so the project needs no audio files:
///  - a looping underwater ambience for the menu and for each level (bright reef, open blue, dark deep),
///  - a short "voice" blip per character while dialogue types out (Marlo low, Lumi growly, Pip high, ...),
///  - chimes for found notes and objective updates, click sounds for choices, a whoosh for level changes
///    and a golden swell when the Golden Clam's light is taken.
/// All sounds are 2D and independent of Time.timeScale, so they keep playing while the game is paused for dialogue.
/// </summary>
public class StoryAudio : MonoBehaviour
{
    public static StoryAudio Instance { get; private set; }

    private const int Rate = 44100;
    private const float AmbientVolume = 0.3f;

    private AudioSource ambientSource, sfxSource, blipSource;
    private readonly AudioClip[] ambientClips = new AudioClip[4];   // 0 = menu, 1..3 = levels
    private readonly Dictionary<string, AudioClip> blips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<StorySfx, AudioClip> sfx = new Dictionary<StorySfx, AudioClip>();
    private int currentAmbient = -1;
    private Coroutine fadeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    // ------------------------------------------------------------------ public API (all safe to call when no audio exists)

    public static void Play(StorySfx sound)
    {
        if (Instance == null) return;
        AudioClip clip;
        if (Instance.sfx.TryGetValue(sound, out clip)) Instance.sfxSource.PlayOneShot(clip, 0.6f);
    }

    /// <summary>One typewriter blip in the voice of the given speaker.</summary>
    public static void Blip(string speaker)
    {
        if (Instance == null) return;
        string key = BlipKey(speaker);
        AudioClip clip;
        if (!Instance.blips.TryGetValue(key, out clip)) return;
        Instance.blipSource.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
        Instance.blipSource.PlayOneShot(clip, 0.35f);
    }

    // ------------------------------------------------------------------ setup

    private void Awake()
    {
        Instance = this;
        ambientSource = MakeSource("Ambient", true);
        sfxSource = MakeSource("Sfx", false);
        blipSource = MakeSource("Blips", false);
        ambientSource.volume = 0f;

        BuildClips();
        SceneManager.sceneLoaded += OnSceneLoaded;
        ChooseAmbient();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private AudioSource MakeSource(string name, bool loop)
    {
        GameObject go = new GameObject(name + " Source");
        go.transform.SetParent(transform, false);
        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        return s;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { ChooseAmbient(); }

    private void ChooseAmbient()
    {
        int index = StoryStateManager.InLevel ? Mathf.Clamp(StoryStateManager.LevelTier, 1, 3) : 0;
        if (index == currentAmbient) return;
        currentAmbient = index;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(SwapAmbient(ambientClips[index], index == 0 ? AmbientVolume * 0.8f : AmbientVolume));
    }

    private IEnumerator SwapAmbient(AudioClip clip, float volume)
    {
        float start = ambientSource.volume;
        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            ambientSource.volume = Mathf.Lerp(start, 0f, t / 0.6f);
            yield return null;
        }
        ambientSource.volume = 0f;
        ambientSource.clip = clip;
        ambientSource.Play();
        for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime)
        {
            ambientSource.volume = Mathf.Lerp(0f, volume, t / 1.5f);
            yield return null;
        }
        ambientSource.volume = volume;
    }

    // ------------------------------------------------------------------ clip generation

    private void BuildClips()
    {
        // Ambience: a soft chord with slowly pulsing layers plus a few bubbles. All frequencies and LFO rates are
        // whole numbers of cycles over the 8 second clip, so it loops without a click.
        ambientClips[0] = MakeAmbient("Ambient Menu", new float[] { 110f, 165f, 220f, 330f }, new float[] { 0.5f, 0.35f, 0.25f, 0.12f }, 0f, 11);
        ambientClips[1] = MakeAmbient("Ambient Reef", new float[] { 110f, 165f, 220f, 330f, 660f }, new float[] { 0.5f, 0.35f, 0.28f, 0.14f, 0.05f }, 0f, 21);
        ambientClips[2] = MakeAmbient("Ambient Open Water", new float[] { 73.375f, 146.75f, 185f, 220f, 293.5f }, new float[] { 0.5f, 0.35f, 0.25f, 0.2f, 0.1f }, 0f, 31);
        ambientClips[3] = MakeAmbient("Ambient Deep", new float[] { 55f, 73.375f, 110f, 155.5f }, new float[] { 0.55f, 0.4f, 0.28f, 0.14f }, 0.5f, 41);

        blips["marlo"] = MakeBlip(175f, 0.0f);
        blips["lumi"] = MakeBlip(120f, 0.6f);
        blips["pip"] = MakeBlip(560f, 0.0f);
        blips["you"] = MakeBlip(330f, 0.15f);
        blips["note"] = MakeBlip(420f, 0.0f);
        blips["narrator"] = MakeBlip(250f, 0.0f);

        sfx[StorySfx.Pickup] = MakeArpeggio("Pickup", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.07f, 0.4f);
        sfx[StorySfx.ObjectiveUpdate] = MakeArpeggio("Objective", new float[] { 880f, 1318.5f }, 0.12f, 0.45f);
        sfx[StorySfx.ChoiceMove] = MakeArpeggio("Choice Move", new float[] { 700f }, 0f, 0.07f);
        sfx[StorySfx.ChoiceConfirm] = MakeArpeggio("Choice Confirm", new float[] { 523.25f, 784f }, 0.06f, 0.25f);
        sfx[StorySfx.Whoosh] = MakeWhoosh();
        sfx[StorySfx.GoldenSwell] = MakeSwell();
    }

    private static string BlipKey(string speaker)
    {
        if (string.IsNullOrEmpty(speaker)) return "narrator";
        if (speaker.StartsWith("Marlo")) return "marlo";
        if (speaker == "Lumi") return "lumi";
        if (speaker == "Pip") return "pip";
        if (speaker == "You") return "you";
        if (speaker.StartsWith("Message")) return "note";
        return "narrator";
    }

    private static AudioClip MakeAmbient(string name, float[] freqs, float[] amps, float pulseHz, int seed)
    {
        const float length = 8f;
        int n = (int)(Rate * length);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)Rate;
            double s = 0.0;
            for (int k = 0; k < freqs.Length; k++)
            {
                double lfo = 0.65 + 0.35 * Math.Sin(2.0 * Math.PI * (0.125 * (k % 4 + 1)) * t + k);
                s += amps[k] * Math.Sin(2.0 * Math.PI * freqs[k] * t) * lfo;
            }
            if (pulseHz > 0f) s *= 0.8 + 0.2 * Math.Sin(2.0 * Math.PI * pulseHz * t);
            d[i] = (float)s;
        }

        // a handful of bubbles baked into the loop
        System.Random rng = new System.Random(seed);
        for (int b = 0; b < 7; b++)
        {
            int start = (int)((0.3f + (float)rng.NextDouble() * 6.8f) * Rate);
            float f0 = 500f + (float)rng.NextDouble() * 600f;
            int len = (int)(0.09f * Rate);
            for (int j = 0; j < len && start + j < n; j++)
            {
                float tt = j / (float)Rate;
                d[start + j] += 0.14f * Mathf.Sin(2f * Mathf.PI * f0 * (1f + tt * 6f) * tt) * Mathf.Exp(-tt * 40f);
            }
        }

        Normalize(d, 0.8f);
        return ToClip(name, d);
    }

    private static AudioClip MakeBlip(float freq, float grit)
    {
        int n = (int)(Rate * 0.055f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Exp(-t * 55f) * Mathf.Min(1f, t * 400f);
            float s = Mathf.Sin(2f * Mathf.PI * freq * t);
            float square = Mathf.Sign(s);
            d[i] = Mathf.Lerp(s, square * 0.5f, grit) * env;
        }
        return ToClip("Blip", d);
    }

    private static AudioClip MakeArpeggio(string name, float[] notes, float step, float noteLength)
    {
        int n = (int)(Rate * (step * (notes.Length - 1) + noteLength));
        float[] d = new float[n];
        for (int k = 0; k < notes.Length; k++)
        {
            int start = (int)(Rate * step * k);
            int len = (int)(Rate * noteLength);
            for (int j = 0; j < len && start + j < n; j++)
            {
                float t = j / (float)Rate;
                float env = Mathf.Exp(-t * (4f / noteLength)) * Mathf.Min(1f, t * 600f);
                d[start + j] += 0.4f * (Mathf.Sin(2f * Mathf.PI * notes[k] * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * notes[k] * t)) * env;
            }
        }
        Normalize(d, 0.7f);
        return ToClip(name, d);
    }

    private static AudioClip MakeWhoosh()
    {
        const float length = 1.6f;
        int n = (int)(Rate * length);
        float[] d = new float[n];
        System.Random rng = new System.Random(5);
        float y = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate / length;
            float env = Mathf.Pow(Mathf.Sin(Mathf.PI * t), 2f);
            float cutoff = Mathf.Lerp(0.015f, 0.35f, Mathf.Sin(Mathf.PI * t));
            float x = (float)(rng.NextDouble() * 2.0 - 1.0);
            y += cutoff * (x - y);
            d[i] = y * env;
        }
        Normalize(d, 0.8f);
        return ToClip("Whoosh", d);
    }

    private static AudioClip MakeSwell()
    {
        const float length = 3.2f;
        float[] notes = { 261.63f, 329.63f, 392f, 523.25f, 1046.5f };
        float[] gains = { 0.3f, 0.3f, 0.3f, 0.25f, 0.06f };
        int n = (int)(Rate * length);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Min(t / 1.2f, 1f) * Mathf.Clamp01((length - t) / 1.6f);
            float s = 0f;
            for (int k = 0; k < notes.Length; k++) s += gains[k] * Mathf.Sin(2f * Mathf.PI * notes[k] * t);
            d[i] = s * env;
        }
        Normalize(d, 0.7f);
        return ToClip("Golden Swell", d);
    }

    private static void Normalize(float[] d, float peak)
    {
        float max = 0.0001f;
        for (int i = 0; i < d.Length; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
        float g = peak / max;
        for (int i = 0; i < d.Length; i++) d[i] *= g;
    }

    private static AudioClip ToClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
