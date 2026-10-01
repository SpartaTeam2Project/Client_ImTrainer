using UnityEngine;

/// <summary>
/// 풀 타입 공격의 진화. 별이 주위를 계속 돈다.
/// </summary>
[CreateAssetMenu(fileName = "Grass Type Attack Evolution Data", menuName = "Ability/Evolution/Grass Type Attack")]
public class GrassTypeAttackEvolutionData : GenericAbilityData<GrassTypeAttackEvolutionLevel>
{
    private void OnValidate()
    {
        type = AbilityType.GrassAttackEvolution;
        isActiveAbility = true;
    }
}

/// <summary>
/// 계속 도는 별의 수치.
/// </summary>
[System.Serializable]
public class GrassTypeAttackEvolutionLevel : AbilityLevel
{
    [SerializeField, Min(0.1f)] private float damage = 1f;
    [SerializeField, Min(1)] private int projectilesCount = 6;
    [SerializeField, Min(0.1f)] private float radius = 1.5f;
    [SerializeField] private float angularSpeed = -300f;

    public float Damage => damage;

    public int ProjectilesCount => projectilesCount;

    public float Radius => radius;

    public float AngularSpeed => angularSpeed;
}
