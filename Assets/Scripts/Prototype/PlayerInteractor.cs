using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Finds the nearest Interactable in range, shows a prompt, and uses it on R.</summary>
public class PlayerInteractor : MonoBehaviour
{
    private void Update()
    {
        Interactable best = null;
        float bestDist = float.MaxValue;
        foreach (Interactable i in Interactable.All)
        {
            if (i == null || !i.CanInteract) continue;
            float d = Vector3.Distance(transform.position, i.transform.position);
            if (d <= i.range && d < bestDist) { best = i; bestDist = d; }
        }

        GameHUD.SetPrompt(best != null ? "[R]  " + best.prompt : "");

        Keyboard k = Keyboard.current;
        if (best != null && k != null && k.rKey.wasPressedThisFrame) best.Interact();
    }
}
