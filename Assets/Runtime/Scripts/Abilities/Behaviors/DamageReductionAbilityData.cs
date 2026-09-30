using UnityEngine;

/// <summary>
/// 피해 감소 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Damage Reduction Ability Data", menuName = "Ability/Passive/Damage Reduction")]
public class DamageReductionAbilityData : GenericAbilityData<DamageReductionAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.DamageReduction;
        isActiveAbility = false;
    }
}

/// <summary>
/// 피해 감소 퍼센트 한 레벨.
/// </summary>
[System.Serializable]
public class DamageReductionAbilityLevel : AbilityLevel
{
    [SerializeField, Range(1, 99)] private int damageReductionPercent = 10;

    public int DamageReductionPercent => damageReductionPercent;
}
