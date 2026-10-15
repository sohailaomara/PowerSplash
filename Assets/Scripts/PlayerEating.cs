using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private bool leaving;

    private void Start()
    {
        playerData = GetComponent<EntityData>();
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;
        ApplyProgress();
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
            else Die(otherData);
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
            AnnounceTier(wanted);
        }
        else if (extra == "")
        {
            GameHUD.Message("Yum!  +" + Mathf.RoundToInt(other.growthValue) + " growth", 1.5f);
        }

        Fx.Burst(target.transform.position, new Color(0.75f, 0.92f, 1f, 0.9f), 14, 0.3f, 3f, 0.8f);
        // Eaten fish vanish immediately (Destroy only takes effect at the end of the frame)
        foreach (Renderer r in target.GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider c in target.GetComponentsInChildren<Collider>()) c.enabled = false;
        bool finalReward = target.GetComponent<FinalReward>() != null;
        Destroy(target);

        // Eating the final reward (the Golden Clam in Level 3) wins the game
        if (finalReward)
            GameHUD.ShowWinScreen("Final size: Tier " + playerData.sizeTier + "   |   Growth: " + Mathf.FloorToInt(playerData.growthValue));
    }

    // Each size tier lives in its own scene (tier 1 = Level 1, tier 2 = Level 2, tier 3 = Level 3).
    // Growth, tier and the stored ability are carried from scene to scene by PlayerProgress.
    private void ApplyProgress()
    {
        int sceneTier = PlayerProgress.SceneTier();
        float growth;
        int tier;
        AbilityType stored = AbilityType.None;

        if (PlayerProgress.HasData && PlayerProgress.Tier >= sceneTier)
        {
            growth = PlayerProgress.Growth;
            tier = PlayerProgress.Tier;
            stored = PlayerProgress.Stored;
        }
        else
        {
            // Scene opened directly (for example from the editor): start at the minimum for this tier
            tier = sceneTier;
            growth = sceneTier >= 3 ? tier3Threshold : (sceneTier == 2 ? tier2Threshold : 0f);
        }

        playerData.growthValue = growth;
        playerData.sizeTier = tier;
        transform.localScale += Vector3.one * (growth * sizeGrowthScaleMultiplier);
        if (stored != AbilityType.None && AbilitySystem.Instance != null) AbilitySystem.Instance.Restore(stored);
    }

    private void AnnounceTier(int tier)
    {
        int nextIndex = PlayerProgress.SceneIndexForTier(tier);
        bool sceneExists = nextIndex < SceneManager.sceneCountInBuildSettings && nextIndex != SceneManager.GetActiveScene().buildIndex;

        if (tier > PlayerProgress.SceneTier() && sceneExists && !leaving)
        {
            leaving = true;
            GameHUD.Message("Size Tier " + tier + " reached! Swimming on to Level " + tier + "...", 3f);
            StartCoroutine(LoadLevelAfter(nextIndex, tier, 2.5f));
        }
        else
        {
            GameHUD.Message("You reached Size Tier " + tier + "!", 3.5f);
        }
    }

    private IEnumerator LoadLevelAfter(int sceneIndex, int tier, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dead) { leaving = false; yield break; }

        AbilitySystem abilities = AbilitySystem.Instance;
        PlayerProgress.Save(playerData.growthValue, tier, abilities != null ? abilities.Stored : AbilityType.None);
        PlayerInventory.Reset();
        SceneManager.LoadScene(sceneIndex);
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

    private void Die(EntityData killer)
    {
        dead = true;
        Fx.Burst(transform.position, new Color(1f, 0.4f, 0.3f, 0.9f), 30, 0.4f, 6f, 1f);

        // The main fish was eaten: it vanishes and stops responding to input.
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        FishMovement move = GetComponent<FishMovement>();
        if (move != null) move.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        string by = killer != null ? killer.name : "a predator";
        GameHUD.ShowLoseScreen("You were eaten by " + by + "!");
    }

    // Retrying is handled by the lose screen in GameHUD (reloads the level).
}