using UnityEngine;

/// <summary>
/// 쿨다운 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Cooldown WeaponAbility Data", menuName = "WeaponAbility/Passive/Cooldown")]
public class CooldownWeaponAbilityData : GenericWeaponAbilityData<CooldownWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.Cooldown;
        isActiveAbility = false;
    }
}

/// <summary>
/// 쿨다운 배율 한 레벨.
/// </summary>
[System.Serializable]
public class CooldownWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Range(0.1f, 1f)] private float cooldownMultiplier = 1f;

    public float CooldownMultiplier => cooldownMultiplier;
}
