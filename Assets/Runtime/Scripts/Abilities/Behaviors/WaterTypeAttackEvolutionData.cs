using UnityEngine;

/// <summary>
/// 물 타입 공격의 진화. 더 길고 강한 관통 빔이다.
/// </summary>
[CreateAssetMenu(fileName = "Water Type Attack Evolution Data", menuName = "Ability/Evolution/Water Type Attack")]
public class WaterTypeAttackEvolutionData : GenericAbilityData<WaterTypeAttackEvolutionLevel>
{
    private void OnValidate()
    {
        type = AbilityType.WaterAttackEvolution;
        isActiveAbility = true;
    }
}

/// <summary>
/// 강화된 관통 빔의 수치.
/// </summary>
[System.Serializable]
public class WaterTypeAttackEvolutionLevel : AbilityLevel
{
    [SerializeField, Min(0.1f)] private float damage = 24f;
    [SerializeField, Min(0f)] private float abilityCooldown = 1.6f;
    [SerializeField, Min(0.1f)] private float beamLength = 9f;
    [SerializeField, Min(0.1f)] private float beamWidth = 0.7f;
    [SerializeField, Min(0.05f)] private float beamDuration = 0.7f;
    [SerializeField, Min(0.02f)] private float damageInterval = 0.08f;

    public float Damage => damage;

    public float AbilityCooldown => abilityCooldown;

    public float BeamLength => beamLength;

    public float BeamWidth => beamWidth;

    public float BeamDuration => beamDuration;

    public float DamageInterval => damageInterval;
}
