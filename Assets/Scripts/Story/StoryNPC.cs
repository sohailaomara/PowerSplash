using System;
using UnityEngine;

/// <summary>
/// A character the player can talk to with R (it reuses the game's Interactable / PlayerInteractor system).
/// Each NPC has an ordered list of conversations; the first one whose flag conditions match is used, so the
/// character's dialogue changes as the story advances and after the player's choice.
/// </summary>
public class StoryNPC : Interactable
{
    [Serializable]
    public class Conversation
    {
        [Tooltip("Flag that must be set for this conversation (empty = none)")]
        public string requiresFlag;
        [Tooltip("Conversation is skipped once this flag is set (empty = none)")]
        public string blockedByFlag;
        public DialogueData dialogue;
    }

    public string npcName = "Fish";
    public Conversation[] conversations;
    [Tooltip("If set, this NPC only exists once the flag is set (for example Pip only appears in Level 3 if he was saved)")]
    public string existsOnlyWhenFlag;
    public float labelHeight = 5f;
    public bool bob = true;
    [Tooltip("If above 0 the character swims slow loops of this radius around its spot instead of bobbing in place")]
    public float swimRadius = 0f;
    public float swimSpeed = 1.6f;
    public bool sway = true;

    private Vector3 basePos;
    private Quaternion baseRot;
    private float phase;
    private Transform label;
    private float angle, swimDir = 1f;
    private bool started;

    public override bool CanInteract
    {
        get
        {
            return Time.timeScale > 0.01f && !DialogueManager.IsBusy && !CinematicManager.Busy && !LevelTransitionManager.Loading && Pick() != null;
        }
    }

    private void Awake()
    {
        prompt = "Talk to " + npcName;
        range = 16f;
        phase = UnityEngine.Random.value * 6.28f;
        BuildLabel();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(existsOnlyWhenFlag) && !StoryStateManager.Has(existsOnlyWhenFlag))
        {
            gameObject.SetActive(false);
            return;
        }
        basePos = transform.position;
        baseRot = transform.rotation;
        started = true;
        angle = UnityEngine.Random.value * 6.28f;
        swimDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
    }

    private Conversation Pick()
    {
        if (conversations == null) return null;
        foreach (Conversation c in conversations)
        {
            if (c.dialogue == null) continue;
            if (!string.IsNullOrEmpty(c.requiresFlag) && !StoryStateManager.Has(c.requiresFlag)) continue;
            if (!string.IsNullOrEmpty(c.blockedByFlag) && StoryStateManager.Has(c.blockedByFlag)) continue;
            return c;
        }
        return null;
    }

    public override void Interact()
    {
        Conversation c = Pick();
        if (c != null && DialogueManager.Instance != null) DialogueManager.Instance.StartDialogue(c.dialogue);
    }

    private void BuildLabel()
    {
        // Floating name tag that always faces the camera
        GameObject go = new GameObject("Name Label");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * labelHeight;
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = npcName;
        tm.font = StoryUI.DefaultFont;
        tm.fontSize = 64;
        tm.characterSize = 0.2f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.75f, 0.95f, 1f);
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        label = go.transform;
    }

    private void LateUpdate()
    {
        if (started && swimRadius > 0f)
        {
            // swim a slow loop: forward along the circle, nose along the path, leaning into the turn, tail wagging
            angle += swimDir * (swimSpeed / swimRadius) * Time.deltaTime;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * swimRadius;
            transform.position = basePos + offset + Vector3.up * Mathf.Sin(Time.time * 0.5f + phase) * 0.8f;
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * swimDir;
            Quaternion look = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, 0f, -swimDir * 8f);
            transform.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * 10.5f + phase) * 6f, Vector3.up) * look;
        }
        else if (started)
        {
            if (bob) transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 1.2f + phase) * 0.35f;
            // slow sway plus a quick tail-beat wag so the character looks like it is swimming in place
            if (sway) transform.rotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.6f + phase) * 10f + Mathf.Sin(Time.time * 10.5f + phase) * 5f, 0f);
        }
        if (label != null && Camera.main != null) label.rotation = Camera.main.transform.rotation;
    }
}
