using UnityEngine;

/// <summary>
/// 주기적으로 체력을 회복하는 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Restore HP WeaponAbility Data", menuName = "WeaponAbility/Passive/Restore HP")]
public class RestoreHPWeaponAbilityData : GenericWeaponAbilityData<RestoreHPWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.RestoreHP;
        isActiveAbility = false;
    }
}

/// <summary>
/// 회복량과 간격 한 레벨.
/// </summary>
[System.Serializable]
public class RestoreHPWeaponAbilityLevel : WeaponAbilityLevel
{
    private const float MIN_COOLDOWN = 0.1f;

    [SerializeField, Range(0f, 20f)] private int restoredHPPercent = 1;
    [SerializeField] private float cooldown = 1f;

    public int RestoredHPPercent => restoredHPPercent;

    public float Cooldown => Mathf.Max(MIN_COOLDOWN, cooldown);
}
