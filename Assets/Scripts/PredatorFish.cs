using UnityEngine;

public enum PredatorState { Patrol, Alert, Pursue, Disengage }

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

    private Rigidbody rb;
    private Transform playerTransform;
    private EntityData myData;
    private EntityData playerData;
    private Vector3 spawnPoint;
    private Vector3 patrolDirection;
    private float timer;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2.0f;

        myData = GetComponent<EntityData>();
        spawnPoint = transform.position;
        patrolDirection = Random.onUnitSphere;

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
                    currentState = PredatorState.Alert;
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
        Vector3 targetDir = transform.forward;
        float moveSpeed = patrolSpeed;

        switch (currentState)
        {
            case PredatorState.Patrol:
                timer += Time.fixedDeltaTime;
                if (Vector3.Distance(transform.position, spawnPoint) > 30f)
                    patrolDirection = (spawnPoint - transform.position).normalized;
                else if (timer >= 6f)
                {
                    patrolDirection = Random.onUnitSphere;
                    timer = 0f;
                }
                targetDir = patrolDirection;
                moveSpeed = patrolSpeed;
                break;

            case PredatorState.Alert:
                // Turn towards player slowly, warning them
                targetDir = (playerTransform.position - transform.position).normalized;
                moveSpeed = patrolSpeed * 0.5f;
                break;

            case PredatorState.Pursue:
                // Fast direct chase
                targetDir = (playerTransform.position - transform.position).normalized;
                moveSpeed = pursueSpeed;
                break;

            case PredatorState.Disengage:
                // Swim back to spawn point
                targetDir = (spawnPoint - transform.position).normalized;
                moveSpeed = patrolSpeed;
                break;
        }

        // 3. Movement Application
        if (targetDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * turnSpeed);
        }

        rb.AddForce((transform.forward * moveSpeed) - rb.linearVelocity, ForceMode.Acceleration);
    }
}