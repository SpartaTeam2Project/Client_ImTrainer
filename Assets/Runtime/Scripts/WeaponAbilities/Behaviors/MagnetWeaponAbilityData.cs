using UnityEngine;

/// <summary>
/// 자석 반경 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Magnet WeaponAbility Data", menuName = "WeaponAbility/Passive/Magnet")]
public class MagnetWeaponAbilityData : GenericWeaponAbilityData<MagnetWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.Magnet;
        isActiveAbility = false;
    }
}

/// <summary>
/// 자석 반경 배율 한 레벨.
/// </summary>
[System.Serializable]
public class MagnetWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Min(1f)] private float radiusMultiplier = 1f;

    public float RadiusMultiplier => radiusMultiplier;
}
