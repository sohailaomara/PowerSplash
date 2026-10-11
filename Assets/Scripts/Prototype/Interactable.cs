using System.Collections.Generic;
using UnityEngine;

/// <summary>Base class for things the player can use with R when close enough.</summary>
public abstract class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> All = new List<Interactable>();

    public string prompt = "Interact";
    public float range = 12f;

    public virtual bool CanInteract { get { return true; } }
    public abstract void Interact();

    protected virtual void OnEnable() { All.Add(this); }
    protected virtual void OnDisable() { All.Remove(this); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { All.Clear(); }
}
