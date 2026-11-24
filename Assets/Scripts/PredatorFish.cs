using UnityEngine;

public enum PredatorState { Patrol, Alert, Pursue, Disengage }

/// <summary>
/// Predator AI: patrols, becomes alert when a smaller player comes close, then pursues it.
/// The AI only chooses a direction and speed; FishSwimAnimator turns that into believable swimming.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(EntityData))]
public class PredatorFish : MonoBehaviour
{
    [Header("Detection Ranges")]
    [SerializeField] private float alertRadius = 12f;
    [SerializeField] private float pursueRadius = 8f;
    [SerializeField] private float disengageRadius = 18f;

    [Header("Movement Speeds")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float pursueSpeed = 7.0f; // Faster than player base swim speed
    [SerializeField] private float turnSpeed = 3f;

    [Header("Current State")]
    public PredatorState currentState = PredatorState.Patrol;

    private const float VerticalBand = 8f;

    private Rigidbody rb;
    private FishSwimAnimator swim;
    private Transform playerTransform;
    private EntityData myData;
    private EntityData playerData;
    private Vector3 spawnPoint;
    private Vector3 patrolDirection;
    private float timer;
    private float nextChange = 6f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2.0f;

        swim = FishSwimAnimator.Ensure(gameObject);
        myData = GetComponent<EntityData>();
        spawnPoint = transform.position;
        patrolDirection = swim.Heading;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerData = player.GetComponent<EntityData>();
        }
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 1. Evaluate State Transitions
        // Predator only attacks if Player is SMALLER in Size Tier
        bool playerIsSmaller = (playerData == null || playerData.sizeTier < myData.sizeTier) && !AbilitySystem.PlayerHidden;

        switch (currentState)
        {
            case PredatorState.Patrol:
                if (distToPlayer <= alertRadius && playerIsSmaller)
                    { currentState = PredatorState.Alert; StoryAudio.Play(StorySfx.Alert); }
                break;

            case PredatorState.Alert:
                if (distToPlayer <= pursueRadius)
                    currentState = PredatorState.Pursue;
                else if (distToPlayer > alertRadius)
                    currentState = PredatorState.Patrol;
                break;

            case PredatorState.Pursue:
                if (distToPlayer >= disengageRadius || !playerIsSmaller)
                    currentState = PredatorState.Disengage;
                break;

            case PredatorState.Disengage:
                // Swim back towards spawn area before resuming normal patrol
                if (distToPlayer > disengageRadius)
                    currentState = PredatorState.Patrol;
                break;
        }

        // 2. Execute State Behavior
        Vector3 targetDir = swim.Heading;
        float moveSpeed = patrolSpeed;
        float turnRate = turnSpeed * 22f;      // degrees per second; big hunters turn slowly
        float pitchLimit = -1f;

        switch (currentState)
        {
            case PredatorState.Patrol:
                timer += Time.fixedDeltaTime;
                if (Vector3.Distance(transform.position, spawnPoint) > 30f)
                    patrolDirection = (spawnPoint - transform.position).normalized;
                else if (timer >= nextChange)
                {
                    patrolDirection = FishSwimAnimator.RandomSwimDirection();
                    timer = 0f;
                    nextChange = 6f * Random.Range(0.7f, 1.5f);
                }
                float dy = transform.position.y - spawnPoint.y;
                if (dy > VerticalBand) patrolDirection.y = Mathf.Min(patrolDirection.y, -0.3f);
                else if (dy < -VerticalBand) patrolDirection.y = Mathf.Max(patrolDirection.y, 0.3f);
                targetDir = patrolDirection;
                moveSpeed = patrolSpeed;
                break;

            case PredatorState.Alert:
                // Turn towards player slowly, warning them
                targetDir = (playerTransform.position - transform.position).normalized;
                moveSpeed = patrolSpeed * 0.5f;
                break;

            case PredatorState.Pursue:
                // Fast direct chase: sharper turns, steeper dives allowed
                targetDir = (playerTransform.position - transform.position).normalized;
                moveSpeed = pursueSpeed;
                turnRate = turnSpeed * 40f;
                pitchLimit = 50f;
                break;

            case PredatorState.Disengage:
                // Swim back to spawn point
                targetDir = (spawnPoint - transform.position).normalized;
                moveSpeed = patrolSpeed;
                break;
        }

        // 3. Movement Application
        if (currentState != PredatorState.Pursue) targetDir = swim.AvoidObstacles(targetDir);
        swim.Steer(targetDir, moveSpeed, turnRate, Time.fixedDeltaTime, pitchLimit);
    }
}
