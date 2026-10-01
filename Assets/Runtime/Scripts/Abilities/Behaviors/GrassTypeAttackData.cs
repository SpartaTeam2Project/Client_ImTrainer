using UnityEngine;

/// <summary>
/// 풀 타입 공격. 플레이어 주위를 잠시 도는 별이다.
/// </summary>
[CreateAssetMenu(fileName = "Grass Type Attack Data", menuName = "Ability/Active/Grass Type Attack")]
public class GrassTypeAttackData : GenericAbilityData<GrassTypeAttackLevel>
{
    private void OnValidate()
    {
        type = AbilityType.GrassAttack;
        isActiveAbility = true;
    }
}

/// <summary>
/// 도는 별 한 레벨의 수치.
/// </summary>
[System.Serializable]
public class GrassTypeAttackLevel : AbilityLevel
{
    [SerializeField, Min(0.1f)] private float damage = 1f;
    [SerializeField, Min(0f)] private float abilityCooldown = 6f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 3f;
    [SerializeField, Min(1)] private int projectilesCount = 2;
    [SerializeField, Min(0.1f)] private float radius = 1.5f;
    [SerializeField] private float angularSpeed = -200f;

    public float Damage => damage;

    public float AbilityCooldown => abilityCooldown;

    public float ProjectileLifetime => projectileLifetime;

    public int ProjectilesCount => projectilesCount;

    public float Radius => radius;

    public float AngularSpeed => angularSpeed;
}
