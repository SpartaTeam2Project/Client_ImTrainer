using UnityEngine;

/// <summary>
/// 공격력 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Damage WeaponAbility Data", menuName = "WeaponAbility/Passive/Damage")]
public class DamageWeaponAbilityData : GenericWeaponAbilityData<DamageWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.Damage;
        isActiveAbility = false;
    }
}

/// <summary>
/// 공격력 배율 한 레벨.
/// </summary>
[System.Serializable]
public class DamageWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Min(1f)] private float damageMultiplier = 1f;

    public float DamageMultiplier => damageMultiplier;
}
