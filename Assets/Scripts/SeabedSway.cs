using UnityEngine;

/// <summary>Gentle swaying of kelp and seaweed in the current: a slow rocking around the base, a different rhythm for each plant.</summary>
public class SeabedSway : MonoBehaviour
{
    [SerializeField] private float amplitude = 4.5f;   // degrees
    [SerializeField] private float speed = 0.9f;

    private Quaternion baseRotation;
    private float phase;

    private void Start()
    {
        baseRotation = transform.localRotation;
        phase = Random.value * 6.28f;
        speed *= Random.Range(0.8f, 1.25f);
        amplitude *= Random.Range(0.7f, 1.3f);
    }

    private void Update()
    {
        float t = Time.time * speed + phase;
        transform.localRotation = baseRotation * Quaternion.Euler(Mathf.Sin(t) * amplitude, 0f, Mathf.Sin(t * 0.83f + 1.3f) * amplitude);
    }
}
