using UnityEngine;

/// <summary>Tiny static inventory for the prototype's quest item (Ancient Pearls).</summary>
public static class PlayerInventory
{
    public const int PearlsRequired = 3;
    public static int Pearls { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Pearls = 0; }

    public static void AddPearl() { Pearls = Mathf.Min(Pearls + 1, PearlsRequired); }
}
