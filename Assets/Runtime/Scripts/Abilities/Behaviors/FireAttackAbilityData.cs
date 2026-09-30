using UnityEngine;

/// <summary>
/// 불꽃 타입 공격으로 쓰는 파이어볼 능력 데이터.
/// </summary>
[CreateAssetMenu(fileName = "Fire Attack Ability Data", menuName = "Ability/Active/Fire Attack")]
public class FireAttackAbilityData : GenericAbilityData<FireAttackAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.FireAttack;
        isActiveAbility = true;
    }
}

/// <summary>
/// 파이어볼 한 레벨의 발사 수치.
/// </summary>
[System.Serializable]
public class FireAttackAbilityLevel : AbilityLevel
{
    [SerializeField] private int projectilesCount = 1;
    [SerializeField] private float timeBetweenFireballs;
    [SerializeField] private float abilityCooldown = 1f;
    [SerializeField] private float fireballLifetime = 1f;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float projectileSize = 1f;
    [SerializeField] private float explosionRadius = 1f;

    public int ProjectilesCount => projectilesCount;

    public float TimeBetweenFireballs => timeBetweenFireballs;

    public float AbilityCooldown => abilityCooldown;

    public float FireballLifetime => fireballLifetime;

    public float Speed => speed;

    public float Damage => damage;

    public float ProjectileSize => projectileSize;

    public float ExplosionRadius => explosionRadius;
}
