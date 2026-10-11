using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Lighting behaviour 2: a player-controlled spotlight on the fish (toggle with L).</summary>
public class PlayerFlashlight : MonoBehaviour
{
    public static bool IsOn { get; private set; }

    public Light spot;
    [SerializeField] private bool startOn = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { IsOn = false; }

    private void Start()
    {
        IsOn = startOn;
        if (spot != null) spot.enabled = IsOn;
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.lKey.wasPressedThisFrame)
        {
            IsOn = !IsOn;
            if (spot != null) spot.enabled = IsOn;
        }
    }
}
