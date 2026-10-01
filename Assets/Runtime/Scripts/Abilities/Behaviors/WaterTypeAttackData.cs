using UnityEngine;

/// <summary>
/// 물 타입 공격. 가까운 적 방향으로 관통 빔을 쏜다.
/// </summary>
[CreateAssetMenu(fileName = "Water Type Attack Data", menuName = "Ability/Active/Water Type Attack")]
public class WaterTypeAttackData : GenericAbilityData<WaterTypeAttackLevel>
{
    private void OnValidate()
    {
        type = AbilityType.WaterAttack;
        isActiveAbility = true;
    }
}

/// <summary>
/// 관통 빔 한 레벨의 수치.
/// </summary>
[System.Serializable]
public class WaterTypeAttackLevel : AbilityLevel
{
    [SerializeField, Min(0.1f)] private float damage = 8f;
    [SerializeField, Min(0f)] private float abilityCooldown = 2.2f;
    [SerializeField, Min(0.1f)] private float beamLength = 5f;
    [SerializeField, Min(0.1f)] private float beamWidth = 0.45f;
    [SerializeField, Min(0.05f)] private float beamDuration = 0.35f;
    [SerializeField, Min(0.02f)] private float damageInterval = 0.12f;

    public float Damage => damage;

    public float AbilityCooldown => abilityCooldown;

    public float BeamLength => beamLength;

    public float BeamWidth => beamWidth;

    public float BeamDuration => beamDuration;

    public float DamageInterval => damageInterval;
}
