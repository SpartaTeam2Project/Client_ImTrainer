using UnityEngine;

/// <summary>
/// 공격력 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Damage Ability Data", menuName = "Ability/Passive/Damage")]
public class DamageAbilityData : GenericAbilityData<DamageAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.Damage;
        isActiveAbility = false;
    }
}

/// <summary>
/// 공격력 배율 한 레벨.
/// </summary>
[System.Serializable]
public class DamageAbilityLevel : AbilityLevel
{
    [SerializeField, Min(1f)] private float damageMultiplier = 1f;

    public float DamageMultiplier => damageMultiplier;
}
