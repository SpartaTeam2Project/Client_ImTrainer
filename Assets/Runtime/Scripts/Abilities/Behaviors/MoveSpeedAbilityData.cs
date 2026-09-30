using UnityEngine;

/// <summary>
/// 이동 속도 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Move Speed Ability Data", menuName = "Ability/Passive/Move Speed")]
public class MoveSpeedAbilityData : GenericAbilityData<MoveSpeedAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.MoveSpeed;
        isActiveAbility = false;
    }
}

/// <summary>
/// 이동 속도 배율 한 레벨.
/// </summary>
[System.Serializable]
public class MoveSpeedAbilityLevel : AbilityLevel
{
    [SerializeField, Min(1f)] private float speedMultiplier = 1f;

    public float SpeedMultiplier => speedMultiplier;
}
