using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EntityData))]
public class PlayerEating : MonoBehaviour
{
    private EntityData playerData;
    private Rigidbody rb;

    [SerializeField] private float sizeGrowthScaleMultiplier = 0.05f;
    [SerializeField] private int tier2Threshold = 50;
    [SerializeField] private int tier3Threshold = 120;
    [SerializeField] private float respawnDelay = 1.5f;

    private readonly HashSet<GameObject> eaten = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, float> lastEvent = new Dictionary<GameObject, float>();
    private Vector3 startPosition;
    private Vector3 startScale;
    private Quaternion startRotation;
    private bool dead;

    private void Start()
    {
        playerData = GetComponent<EntityData>();
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (dead || playerData == null) return;

        EntityData otherData = other.GetComponentInParent<EntityData>();
        if (otherData == null || otherData == playerData) return;

        GameObject target = otherData.gameObject;
        if (eaten.Contains(target)) return;

        // The player has more than one collider, so the same creature can report twice in a frame.
        float last;
        if (lastEvent.TryGetValue(target, out last) && Time.time - last < 0.3f) return;
        lastEvent[target] = Time.time;

        bool isPredator = otherData.creatureType == CreatureType.Predator || target.GetComponent<PredatorFish>() != null;
        bool stunned = Stun.IsStunned(target);

        // Inflate: predators back off instead of being eaten
        if (isPredator && AbilitySystem.Inflated && !stunned) return;

        // EATING LOGIC: player is bigger or equal in size tier, or the target is stunned (Shock)
        if (playerData.sizeTier >= otherData.sizeTier || stunned)
        {
            Eat(otherData);
            return;
        }

        // GAME OVER LOGIC: touched a bigger predator
        if (isPredator && otherData.sizeTier > playerData.sizeTier)
        {
            if (AbilitySystem.ShieldActive) Zap(otherData);
            else Die();
        }
    }

    private void Eat(EntityData other)
    {
        GameObject target = other.gameObject;
        eaten.Add(target);

        // 1. Absorb growth
        playerData.growthValue += other.growthValue;

        // 2. Ability grant (any creature that carries an ability)
        string extra = "";
        if (other.grantedAbility != AbilityType.None && AbilitySystem.Instance != null)
        {
            AbilitySystem.Instance.Grant(other.grantedAbility);
            extra = "  +" + other.grantedAbility;
        }

        // 3. Visually scale the player up
        transform.localScale += Vector3.one * (other.growthValue * sizeGrowthScaleMultiplier);

        // 4. Size tier milestones (an active Inflate adds +1 tier on top of the real tier)
        int wanted = playerData.growthValue >= tier3Threshold ? 3 : (playerData.growthValue >= tier2Threshold ? 2 : 1);
        int current = playerData.sizeTier - (AbilitySystem.Inflated ? 1 : 0);
        if (wanted > current)
        {
            playerData.sizeTier += wanted - current;
            GameHUD.Message("You reached Size Tier " + wanted + "!", 3.5f);
        }
        else if (extra == "")
        {
            GameHUD.Message("Yum!  +" + Mathf.RoundToInt(other.growthValue) + " growth", 1.5f);
        }

        Fx.Burst(target.transform.position, new Color(0.75f, 0.92f, 1f, 0.9f), 14, 0.3f, 3f, 0.8f);
        Destroy(target);
    }

    private void Zap(EntityData predator)
    {
        GameObject target = predator.gameObject;
        Stun.Apply(target, 4f);
        Rigidbody prb = target.GetComponent<Rigidbody>();
        if (prb != null)
            prb.AddForce((target.transform.position - transform.position).normalized * 20f, ForceMode.VelocityChange);
        Fx.Flash(transform.position, new Color(0.4f, 0.9f, 1f), 18f, 10f, 0.4f);
        GameHUD.Message("Electric Defense zapped the " + predator.name + "!", 2.5f);
    }

    private void Die()
    {
        dead = true;
        GameHUD.Message("A predator got you! Respawning...", respawnDelay);
        Fx.Burst(transform.position, new Color(1f, 0.4f, 0.3f, 0.9f), 30, 0.4f, 6f, 1f);
        Invoke(nameof(Respawn), respawnDelay);
    }

    private void Respawn()
    {
        if (AbilitySystem.Instance != null) AbilitySystem.Instance.ResetAll();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        transform.SetPositionAndRotation(startPosition, startRotation);
        transform.localScale = startScale;
        playerData.growthValue = 0f;
        playerData.sizeTier = 1;
        dead = false;
        GameHUD.Message("Back to the shallow reef - try again!", 2.5f);
    }
}