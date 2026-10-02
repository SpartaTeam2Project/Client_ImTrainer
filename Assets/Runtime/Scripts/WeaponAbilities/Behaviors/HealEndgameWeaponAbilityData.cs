using UnityEngine;

/// <summary>
/// 다른 업그레이드가 없을 때 나오는 회복.
/// </summary>
[CreateAssetMenu(fileName = "Heal Endgame WeaponAbility Data", menuName = "WeaponAbility/Passive/Heal Endgame")]
public class HealEndgameWeaponAbilityData : GenericWeaponAbilityData<HealEndgameWeaponAbilityLevel>
{
    private void OnValidate()
    {
        type = WeaponAbilityType.HealEndgame;
        isActiveAbility = false;
        isEndgameAbility = true;
    }
}

/// <summary>
/// 최대 체력 비율 회복 한 칸.
/// </summary>
[System.Serializable]
public class HealEndgameWeaponAbilityLevel : WeaponAbilityLevel
{
    [SerializeField, Range(0, 100)] private int healPersentage = 40;

    public int HealPersentage => healPersentage;
}
