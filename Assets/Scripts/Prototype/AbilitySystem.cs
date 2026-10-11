using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player ability system (Power Splash's core hook).
/// Eating a glowing creature STORES its ability; pressing F activates it.
/// Only one ability can be active at a time; each has a duration and a cooldown.
/// Keys 1-5 grant an ability directly (prototype/debug shortcut).
/// </summary>
[RequireComponent(typeof(EntityData))]
public class AbilitySystem : MonoBehaviour
{
    [System.Serializable]
    public class AbilityDef
    {
        public AbilityType type;
        public float duration = 6f;
        public float cooldown = 10f;
    }

    public static AbilitySystem Instance { get; private set; }
    public static bool PlayerHidden { get; private set; }   // Ink Cloud: predators lose track
    public static bool ShieldActive { get; private set; }   // Electric Defense
    public static bool Inflated { get; private set; }       // Inflate: predators back off

    [Header("Ability tuning (durations / cooldowns are placeholders pending playtests)")]
    public AbilityDef[] defs = new AbilityDef[]
    {
        new AbilityDef { type = AbilityType.Inflate,    duration = 8f, cooldown = 10f },
        new AbilityDef { type = AbilityType.InkCloud,   duration = 6f, cooldown = 10f },
        new AbilityDef { type = AbilityType.Shock,      duration = 4f, cooldown = 10f },
        new AbilityDef { type = AbilityType.SpeedBoost, duration = 6f, cooldown = 8f  },
        new AbilityDef { type = AbilityType.Shield,     duration = 8f, cooldown = 12f },
    };

    [SerializeField] private float inflateFactor = 1.6f;
    [SerializeField] private float shockRadius = 18f;
    [SerializeField] private float shockStunSeconds = 4f;
    [SerializeField] private float speedMultiplier = 1.8f;

    public AbilityType Stored { get; private set; }
    public AbilityType Active { get; private set; }
    public float ActiveRemaining { get; private set; }
    public float ActiveDuration { get; private set; }

    private readonly float[] cooldowns = new float[8];
    private readonly Dictionary<string, float> speedOriginals = new Dictionary<string, float>();
    private EntityData data;
    private float currentInflate = 1f;
    private ParticleSystem inkCloud;
    private ParticleSystem speedTrail;
    private GameObject shieldVisual;
    private Light shieldLight;
    private Component moveComp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null; PlayerHidden = false; ShieldActive = false; Inflated = false;
    }

    private void Awake()
    {
        Instance = this;
        data = GetComponent<EntityData>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        PlayerHidden = false; ShieldActive = false; Inflated = false;
    }

    public float CooldownRemaining(AbilityType t) { return cooldowns[(int)t]; }

    private AbilityDef GetDef(AbilityType t)
    {
        foreach (AbilityDef d in defs) if (d.type == t) return d;
        return new AbilityDef { type = t };
    }

    public void Grant(AbilityType t)
    {
        if (t == AbilityType.None) return;
        Stored = t;
        GameHUD.Message("Ability gained: " + t + "  -  press F to use it!", 3.5f);
        Fx.Flash(transform.position, new Color(1f, 0.85f, 0.3f), 14f, 6f, 0.5f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Keyboard k = Keyboard.current;
        if (k != null)
        {
            if (k.fKey.wasPressedThisFrame) TryActivate();
            if (k.digit1Key.wasPressedThisFrame) Grant(AbilityType.Inflate);
            if (k.digit2Key.wasPressedThisFrame) Grant(AbilityType.InkCloud);
            if (k.digit3Key.wasPressedThisFrame) Grant(AbilityType.Shock);
            if (k.digit4Key.wasPressedThisFrame) Grant(AbilityType.SpeedBoost);
            if (k.digit5Key.wasPressedThisFrame) Grant(AbilityType.Shield);
        }

        for (int i = 0; i < cooldowns.Length; i++)
            if (cooldowns[i] > 0f) cooldowns[i] = Mathf.Max(0f, cooldowns[i] - dt);

        if (Active != AbilityType.None)
        {
            ActiveRemaining -= dt;
            if (ActiveRemaining <= 0f) EndActive();
        }

        // Smoothly puff up / shrink back (ratio based, so eating while inflated still works)
        float target = Active == AbilityType.Inflate ? inflateFactor : 1f;
        if (!Mathf.Approximately(currentInflate, target))
        {
            float next = Mathf.MoveTowards(currentInflate, target, dt * 2f);
            transform.localScale *= next / currentInflate;
            currentInflate = next;
        }

        if (shieldLight != null) shieldLight.intensity = 5f + Mathf.PingPong(Time.time * 6f, 3f);
    }

    private void TryActivate()
    {
        if (Stored == AbilityType.None) { GameHUD.Message("No ability stored - eat a glowing creature!", 2.5f); return; }
        if (Active != AbilityType.None) { GameHUD.Message("Another ability is still active", 2f); return; }
        float cd = cooldowns[(int)Stored];
        if (cd > 0f) { GameHUD.Message(Stored + " is recharging (" + cd.ToString("0.0") + "s)", 2f); return; }

        AbilityType t = Stored;
        Stored = AbilityType.None;
        Activate(t);
    }

    private void Activate(AbilityType t)
    {
        AbilityDef d = GetDef(t);
        Active = t;
        ActiveDuration = ActiveRemaining = d.duration;
        Vector3 pos = transform.position;

        switch (t)
        {
            case AbilityType.Inflate:
                Inflated = true;
                data.sizeTier += 1;
                Fx.Burst(pos, new Color(0.8f, 0.95f, 1f, 0.9f), 30, 0.5f, 5f, 1f);
                GameHUD.Message("INFLATE! Predators won't dare attack you", 3f);
                break;

            case AbilityType.InkCloud:
                PlayerHidden = true;
                inkCloud = Fx.Emitter(transform, Vector3.zero, new Color(0.2f, 0.05f, 0.3f, 0.9f), 60f, 3.5f, 3f, 1.5f, 0.6f);
                Fx.Burst(pos, new Color(0.15f, 0.05f, 0.25f, 0.95f), 40, 4f, 6f, 1.5f);
                Fx.Flash(pos, new Color(0.5f, 0.2f, 0.9f), 16f, 5f, 0.4f);
                GameHUD.Message("INK CLOUD! Predators lose track of you", 3f);
                break;

            case AbilityType.Shock:
                DoShock();
                break;

            case AbilityType.SpeedBoost:
                ApplySpeed(true);
                speedTrail = Fx.Emitter(transform, Vector3.zero, new Color(0.7f, 0.95f, 1f, 0.9f), 50f, 0.6f, 1.2f, 0.4f, 0.2f);
                GameHUD.Message("SPEED BOOST!", 2.5f);
                break;

            case AbilityType.Shield:
                ShieldActive = true;
                BuildShield();
                GameHUD.Message("ELECTRIC DEFENSE! Predators that touch you get zapped", 3f);
                break;
        }
    }

    private void DoShock()
    {
        Vector3 pos = transform.position;
        int stunned = 0;
        foreach (EntityData e in FindObjectsByType<EntityData>(FindObjectsSortMode.None))
        {
            if (e == data) continue;
            if (Vector3.Distance(e.transform.position, pos) > shockRadius) continue;
            Stun.Apply(e.gameObject, shockStunSeconds);
            stunned++;
        }
        Fx.Burst(pos, new Color(0.5f, 0.9f, 1f, 1f), 60, 0.5f, 18f, 0.8f);
        Fx.Flash(pos, new Color(0.5f, 0.9f, 1f), 40f, 14f, 0.7f);
        GameHUD.Message("SHOCK! " + stunned + " creature(s) stunned - stunned fish can be eaten!", 3.5f);
    }

    private void BuildShield()
    {
        shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldVisual.name = "Electric Shield (runtime)";
        Collider c = shieldVisual.GetComponent<Collider>();
        if (c != null) Destroy(c);
        shieldVisual.transform.SetParent(transform, false);
        shieldVisual.transform.localScale = Vector3.one * 1.3f;
        Renderer r = shieldVisual.GetComponent<Renderer>();
        Material m = new Material(Shader.Find("Sprites/Default"));
        m.color = new Color(0.3f, 0.9f, 1f, 0.22f);
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        shieldLight = shieldVisual.AddComponent<Light>();
        shieldLight.type = LightType.Point;
        shieldLight.color = new Color(0.4f, 0.9f, 1f);
        shieldLight.range = 12f;
        shieldLight.intensity = 6f;
        shieldLight.shadows = LightShadows.None;
    }

    private void ApplySpeed(bool on)
    {
        if (moveComp == null)
        {
            foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name.StartsWith("FishMovement")) { moveComp = mb; break; }
            }
        }
        if (moveComp == null) return;

        string[] names = { "normalSpeed", "sprintSpeed", "swimSpeed" };
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (string n in names)
        {
            FieldInfo f = moveComp.GetType().GetField(n, flags);
            if (f == null || f.FieldType != typeof(float)) continue;
            if (on)
            {
                float original = (float)f.GetValue(moveComp);
                speedOriginals[n] = original;
                f.SetValue(moveComp, original * speedMultiplier);
            }
            else if (speedOriginals.ContainsKey(n))
            {
                f.SetValue(moveComp, speedOriginals[n]);
            }
        }
        if (!on) speedOriginals.Clear();
    }

    private void EndActive()
    {
        AbilityType ended = Active;
        switch (ended)
        {
            case AbilityType.Inflate:
                Inflated = false;
                data.sizeTier = Mathf.Max(1, data.sizeTier - 1);
                break;
            case AbilityType.InkCloud:
                PlayerHidden = false;
                StopEmitter(ref inkCloud);
                break;
            case AbilityType.SpeedBoost:
                ApplySpeed(false);
                StopEmitter(ref speedTrail);
                break;
            case AbilityType.Shield:
                ShieldActive = false;
                if (shieldVisual != null) Destroy(shieldVisual);
                shieldVisual = null; shieldLight = null;
                break;
        }
        cooldowns[(int)ended] = GetDef(ended).cooldown;
        Active = AbilityType.None;
        ActiveRemaining = 0f;
        GameHUD.Message(ended + " ended", 1.5f);
    }

    private void StopEmitter(ref ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(ps.gameObject, 4f);
        ps = null;
    }

    /// <summary>Called when the player respawns after being eaten.</summary>
    public void ResetAll()
    {
        if (Active != AbilityType.None) EndActive();
        Stored = AbilityType.None;
        for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = 0f;
        currentInflate = 1f;
    }
}
