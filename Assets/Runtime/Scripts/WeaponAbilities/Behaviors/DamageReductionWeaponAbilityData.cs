using UnityEngine;

/// <summary>
/// 피해 감소 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Damage Reduction WeaponAbility Data", menuName = "WeaponAbility/Passive/Damage Reduction")]
public class DamageReductionWeaponAbilityData : GenericWeaponAbilityData<DamageReductionWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.DamageReduction;
        isActiveAbility = false;
    }
}

/// <summary>
/// 피해 감소 퍼센트 한 레벨.
/// </summary>
[System.Serializable]
public class DamageReductionWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Range(1, 99)] private int damageReductionPercent = 10;

    public int DamageReductionPercent => damageReductionPercent;
}
