using System.Collections;
using UnityEngine;

public enum TriggerMode
{
    Zone,          // the player swims into the trigger collider
    OnFlag,        // a story flag gets set (for example all pearls collected)
    OnSceneStart   // shortly after the level opens (arrival scenes, intro)
}

/// <summary>
/// Starts a conversation or crawl when its condition is met. It fires once, and only when its
/// requiresFlag is set and its blockedByFlag is not, so beats never replay and arrive in a sensible order.
/// Zone mode needs a BoxCollider with Is Trigger on the same object.
/// </summary>
public class NarrativeTrigger : MonoBehaviour
{
    public TriggerMode mode = TriggerMode.Zone;
    public DialogueData dialogue;

    [Tooltip("Flag that must already be set (leave empty for none)")]
    public string requiresFlag;
    [Tooltip("Does not fire if this flag is set (leave empty for none)")]
    public string blockedByFlag;
    [Tooltip("Flag set the moment this trigger fires")]
    public string flagToSet;
    [Tooltip("OnFlag mode: the flag that fires this trigger")]
    public string triggerFlag;
    [Tooltip("OnSceneStart mode: seconds to wait after the level opens")]
    public float startDelay = 1f;

    private bool fired;
    private bool subscribed;

    private void Start()
    {
        // Subscribed in Start (not OnEnable) because the story systems are created after the first scene's Awake.
        if (StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.OnFlagSet += HandleFlag;
            subscribed = true;
        }
        if (mode == TriggerMode.OnSceneStart) StartCoroutine(SceneStartRoutine());
    }

    private void OnDestroy()
    {
        if (subscribed && StoryStateManager.Instance != null) StoryStateManager.Instance.OnFlagSet -= HandleFlag;
    }

    private IEnumerator SceneStartRoutine()
    {
        yield return new WaitForSecondsRealtime(startDelay);
        TryFire();
    }

    private void HandleFlag(string flag)
    {
        if (mode == TriggerMode.OnFlag && flag == triggerFlag) TryFire();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (mode != TriggerMode.Zone) return;
        if (other.GetComponentInParent<PlayerEating>() == null) return;
        TryFire();
    }

    private void TryFire()
    {
        if (fired || dialogue == null || DialogueManager.Instance == null) return;
        if (!string.IsNullOrEmpty(requiresFlag) && !StoryStateManager.Has(requiresFlag)) return;
        if (!string.IsNullOrEmpty(blockedByFlag) && StoryStateManager.Has(blockedByFlag)) return;

        fired = true;
        StoryStateManager.Set(flagToSet);
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.45f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
