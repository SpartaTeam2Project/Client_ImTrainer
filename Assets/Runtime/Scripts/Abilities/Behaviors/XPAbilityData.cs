using UnityEngine;

/// <summary>
/// 경험치 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "XP Ability Data", menuName = "Ability/Passive/XP")]
public class XPAbilityData : GenericAbilityData<XPAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.XP;
        isActiveAbility = false;
    }
}

/// <summary>
/// 경험치 배율 한 레벨.
/// </summary>
[System.Serializable]
public class XPAbilityLevel : AbilityLevel
{
    [SerializeField, Min(1f)] private float xpMultiplier = 1f;

    public float XPMultiplier => xpMultiplier;
}
