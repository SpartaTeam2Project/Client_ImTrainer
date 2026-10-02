using UnityEngine;

/// <summary>
/// 경험치 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "XP WeaponAbility Data", menuName = "WeaponAbility/Passive/XP")]
public class XPWeaponAbilityData : GenericWeaponAbilityData<XPWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.XP;
        isActiveAbility = false;
    }
}

/// <summary>
/// 경험치 배율 한 레벨.
/// </summary>
[System.Serializable]
public class XPWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Min(1f)] private float xpMultiplier = 1f;

    public float XPMultiplier => xpMultiplier;
}
