using UnityEngine;

/// <summary>
/// 이동 속도 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Move Speed WeaponAbility Data", menuName = "WeaponAbility/Passive/Move Speed")]
public class MoveSpeedWeaponAbilityData : GenericWeaponAbilityData<MoveSpeedWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.MoveSpeed;
        isActiveAbility = false;
    }
}

/// <summary>
/// 이동 속도 배율 한 레벨.
/// </summary>
[System.Serializable]
public class MoveSpeedWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Min(1f)] private float speedMultiplier = 1f;

    public float SpeedMultiplier => speedMultiplier;
}
