using UnityEngine;

/// <summary>
/// 자석 반경 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Magnet Ability Data", menuName = "Ability/Passive/Magnet")]
public class MagnetAbilityData : GenericAbilityData<MagnetAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.Magnet;
        isActiveAbility = false;
    }
}

/// <summary>
/// 자석 반경 배율 한 레벨.
/// </summary>
[System.Serializable]
public class MagnetAbilityLevel : AbilityLevel
{
    [SerializeField, Min(1f)] private float radiusMultiplier = 1f;

    public float RadiusMultiplier => radiusMultiplier;
}
