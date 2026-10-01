using UnityEngine;

/// <summary>
/// 지속 시간 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Duration WeaponAbility Data", menuName = "WeaponAbility/Passive/Duration")]
public class DurationWeaponAbilityData : GenericWeaponAbilityData<DurationWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.Duration;
        isActiveAbility = false;
    }
}

/// <summary>
/// 지속 시간 배율 한 레벨.
/// </summary>
[System.Serializable]
public class DurationWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Min(1f)] private float durationMultiplier = 1f;

    public float DurationMultiplier => durationMultiplier;
}
