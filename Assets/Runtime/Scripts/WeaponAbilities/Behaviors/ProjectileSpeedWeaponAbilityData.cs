using UnityEngine;

/// <summary>
/// 투사체 속도 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Projectile Speed WeaponAbility Data", menuName = "WeaponAbility/Passive/Projectile Speed")]
public class ProjectileSpeedWeaponAbilityData : GenericWeaponAbilityData<ProjectileSpeedWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.ProjectileSpeed;
        isActiveAbility = false;
    }
}

/// <summary>
/// 투사체 속도 배율 한 레벨.
/// </summary>
[System.Serializable]
public class ProjectileSpeedWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Min(1f)] private float projectileSpeedMultiplier = 1f;

    public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
}
