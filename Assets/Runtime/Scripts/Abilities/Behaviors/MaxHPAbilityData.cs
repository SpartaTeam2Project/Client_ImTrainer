using UnityEngine;

/// <summary>
/// 최대 체력 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Max HP Ability Data", menuName = "Ability/Passive/Max HP")]
public class MaxHPAbilityData : GenericAbilityData<MaxHPAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.MaxHP;
        isActiveAbility = false;
    }
}

/// <summary>
/// 최대 체력 배율 한 레벨.
/// </summary>
[System.Serializable]
public class MaxHPAbilityLevel : AbilityLevel
{
    [SerializeField, Min(1f)] private float maxHPMultiplier = 1f;

    public float MaxHPMultiplier => maxHPMultiplier;
}
