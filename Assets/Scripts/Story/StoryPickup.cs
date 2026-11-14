using UnityEngine;

/// <summary>
/// A found document (message in a bottle). Swim into it to collect it: it sets a story flag, shows the note
/// in the dialogue box and records it in the journal. It needs a trigger collider, like the Ancient Pearls.
/// </summary>
public class StoryPickup : MonoBehaviour
{
    public DialogueData note;
    public string flagToSet;
    public float bobHeight = 0.6f;
    public float bobSpeed = 1.4f;
    public float spinSpeed = 35f;

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
        StoryStateManager.Set(flagToSet);
        StoryAudio.Play(StorySfx.Pickup);
        Fx.Burst(transform.position, new Color(0.8f, 1f, 0.9f), 24, 0.4f, 5f, 1f);
        Fx.Flash(transform.position, new Color(0.8f, 1f, 0.9f), 12f, 5f, 0.5f);
        if (DialogueManager.Instance != null) DialogueManager.Instance.StartDialogue(note);
        Destroy(gameObject);
    }
}
