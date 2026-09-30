using UnityEngine;

/// <summary>
/// 쿨다운 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Cooldown Ability Data", menuName = "Ability/Passive/Cooldown")]
public class CooldownAbilityData : GenericAbilityData<CooldownAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.Cooldown;
        isActiveAbility = false;
    }
}

/// <summary>
/// 쿨다운 배율 한 레벨.
/// </summary>
[System.Serializable]
public class CooldownAbilityLevel : AbilityLevel
{
    [SerializeField, Range(0.1f, 1f)] private float cooldownMultiplier = 1f;

    public float CooldownMultiplier => cooldownMultiplier;
}
