using UnityEngine;

/// <summary>Quest item. Swim into it to collect it; collecting all pearls opens the Rock Gate.</summary>
public class Pearl : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.6f;
    [SerializeField] private float bobSpeed = 1.5f;
    [SerializeField] private float spinSpeed = 60f;

    private Vector3 basePos;
    private float phase;
    private bool taken;

    private void Start()
    {
        basePos = transform.position;
        phase = Random.value * 6.28f;
    }

    private void Update()
    {
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (taken) return;
        if (other.GetComponentInParent<PlayerEating>() == null) return;

        taken = true;
        PlayerInventory.AddPearl();
        GameHUD.Message("Ancient Pearl collected  (" + PlayerInventory.Pearls + " / " + PlayerInventory.PearlsRequired + ")", 3f);
        Fx.Burst(transform.position, new Color(1f, 0.95f, 0.8f), 30, 0.4f, 6f, 1f);
        Fx.Flash(transform.position, new Color(1f, 0.95f, 0.8f), 14f, 6f, 0.5f);
        Destroy(gameObject);
    }
}
