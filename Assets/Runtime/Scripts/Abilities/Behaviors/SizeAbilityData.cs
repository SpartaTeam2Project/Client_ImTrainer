using UnityEngine;

/// <summary>
/// 능력 크기 배율 패시브.
/// </summary>
[CreateAssetMenu(fileName = "Size Ability Data", menuName = "Ability/Passive/Size")]
public class SizeAbilityData : GenericAbilityData<SizeAbilityLevel>
{
    private void OnValidate()
    {
        type = AbilityType.Size;
        isActiveAbility = false;
    }
}

/// <summary>
/// 크기 배율 한 레벨.
/// </summary>
[System.Serializable]
public class SizeAbilityLevel : AbilityLevel
{
    [SerializeField, Min(1f)] private float sizeMultiplier = 1f;

    public float SizeMultiplier => sizeMultiplier;
}
