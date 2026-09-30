using UnityEngine;

/// <summary>
/// 풀 타입 공격으로 쓰는 포스필드 능력 데이터.
/// </summary>
[CreateAssetMenu(fileName = "Grass Attack Ability Data", menuName = "Ability/Active/Grass Attack")]
public class GrassAttackAbilityData : GenericAbilityData<GrassAttackAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.GrassAttack;
        isActiveAbility = true;
    }
}

/// <summary>
/// 포스필드 한 레벨의 범위와 피해.
/// </summary>
[System.Serializable]
public class GrassAttackAbilityLevel : AbilityLevel
{
    [SerializeField, Min(0.1f)] private float fieldRadius = 0.5f;
    [SerializeField, Min(0.1f)] private float damage = 1f;
    [SerializeField, Min(0f)] private float damageCooldown;

    public float FieldRadius => fieldRadius;

    public float Damage => damage;

    public float DamageCooldown => damageCooldown;
}
