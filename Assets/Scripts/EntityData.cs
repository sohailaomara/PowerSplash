using UnityEngine;

public enum CreatureType { Prey, Predator, AbilityCreature }
public enum AbilityType { None, Inflate, InkCloud, Shock, SpeedBoost, Shield }

public class EntityData : MonoBehaviour
{
    [Header("Creature Stats")]
    public CreatureType creatureType = CreatureType.Prey;
    public AbilityType grantedAbility = AbilityType.None;
    public int sizeTier = 1; // 1 = Small, 2 = Medium, 3 = Large, etc.
    public float growthValue = 10f; // Size points added to player on eat
}