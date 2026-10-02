using UnityEngine;

/// <summary>
/// 최대 체력 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Max HP WeaponAbility Data", menuName = "WeaponAbility/Passive/Max HP")]
public class MaxHPWeaponAbilityData : GenericWeaponAbilityData<MaxHPWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.MaxHP;
        isActiveAbility = false;
    }
}

/// <summary>
/// 최대 체력 배율 한 레벨.
/// </summary>
[System.Serializable]
public class MaxHPWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Min(1f)] private float maxHPMultiplier = 1f;

    public float MaxHPMultiplier => maxHPMultiplier;
}
