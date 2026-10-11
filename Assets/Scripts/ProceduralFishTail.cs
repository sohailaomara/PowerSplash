using UnityEngine;

public class ProceduralFishTail : MonoBehaviour
{
    [Header("Wiggle Settings")]
    [SerializeField] private Transform tailMeshOrBone;
    [SerializeField] private float baseWiggleSpeed = 8f;
    [SerializeField] private float baseWiggleAmount = 15f;

    private Rigidbody parentRb;

    private void Start()
    {
        parentRb = GetComponentInParent<Rigidbody>();
    }

    private void Update()
    {
        if (tailMeshOrBone == null) return;

        // Speed-based dynamic wiggle rate
        float currentSpeed = parentRb != null ? parentRb.linearVelocity.magnitude : 1f;
        float dynamicSpeed = baseWiggleSpeed * (1f + currentSpeed * 0.2f);
        float dynamicAmount = baseWiggleAmount * (0.5f + currentSpeed * 0.1f);

        // Sinusoidal tail oscillation
        float yawRotation = Mathf.Sin(Time.time * dynamicSpeed) * dynamicAmount;
        tailMeshOrBone.localRotation = Quaternion.Euler(0f, yawRotation, 0f);
    }
}