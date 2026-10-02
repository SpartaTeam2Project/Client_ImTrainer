using UnityEngine;

/// <summary>
/// 능력 크기 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Size WeaponAbility Data", menuName = "WeaponAbility/Passive/Size")]
public class SizeWeaponAbilityData : GenericWeaponAbilityData<SizeWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.Size;
        isActiveAbility = false;
    }
}

/// <summary>
/// 크기 배율 한 레벨.
/// </summary>
[System.Serializable]
public class SizeWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Min(1f)] private float sizeMultiplier = 1f;

    public float SizeMultiplier => sizeMultiplier;
}
