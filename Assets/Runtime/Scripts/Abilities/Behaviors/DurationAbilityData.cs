using UnityEngine;

/// <summary>
/// 지속 시간 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Duration Ability Data", menuName = "Ability/Passive/Duration")]
public class DurationAbilityData : GenericAbilityData<DurationAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.Duration;
        isActiveAbility = false;
    }
}

/// <summary>
/// 지속 시간 배율 한 레벨.
/// </summary>
[System.Serializable]
public class DurationAbilityLevel : AbilityLevel
{
    [SerializeField, Min(1f)] private float durationMultiplier = 1f;

    public float DurationMultiplier => durationMultiplier;
}
