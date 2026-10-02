/// <summary>
/// 포켓몬 6세대 이후 18타입. 값이 상성표의 행·열 인덱스이므로 순서를 바꾸지 않는다.
/// </summary>
public enum MonsterType
{
    Normal = 0,
    Fire = 1,
    Water = 2,
    Grass = 3,
    Electric = 4,
    Ice = 5,
    Fighting = 6,
    Poison = 7,
    Ground = 8,
    Flying = 9,
    Psychic = 10,
    Bug = 11,
    Rock = 12,
    Ghost = 13,
    Dragon = 14,
    Dark = 15,
    Steel = 16,
    Fairy = 17
}

/// <summary>
/// 공격 타입과 방어 타입의 배율. 표에 없는 조합은 1이다.
/// </summary>
public static class TypeChart
{
    private const float HALF = 0.5f;
    private const float STAB_MULTIPLIER = 1.5f;
    private const float NEUTRAL_MULTIPLIER = 1f;

    // [공격 타입, 방어 타입]. 열 순서는 MonsterType 값과 같다.
    private static readonly float[,] CHART =
    {
        //       노말 불꽃 물 풀 전기 얼음 격투 독 땅 비행 에스퍼 벌레 바위 고스트 드래곤 악 강철 페어리
        /*노말*/ { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, HALF, 0, 1, 1, HALF, 1 },
        /*불꽃*/ { 1, HALF, HALF, 2, 1, 2, 1, 1, 1, 1, 1, 2, HALF, 1, HALF, 1, 2, 1 },
        /*물*/   { 1, 2, HALF, HALF, 1, 1, 1, 1, 2, 1, 1, 1, 2, 1, HALF, 1, 1, 1 },
        /*풀*/   { 1, HALF, 2, HALF, 1, 1, 1, HALF, 2, HALF, 1, HALF, 2, 1, HALF, 1, HALF, 1 },
        /*전기*/ { 1, 1, 2, HALF, HALF, 1, 1, 1, 0, 2, 1, 1, 1, 1, HALF, 1, 1, 1 },
        /*얼음*/ { 1, HALF, HALF, 2, 1, HALF, 1, 1, 2, 2, 1, 1, 1, 1, 2, 1, HALF, 1 },
        /*격투*/ { 2, 1, 1, 1, 1, 2, 1, HALF, 1, HALF, HALF, HALF, 2, 0, 1, 2, 2, HALF },
        /*독*/   { 1, 1, 1, 2, 1, 1, 1, HALF, HALF, 1, 1, 1, HALF, HALF, 1, 1, 0, 2 },
        /*땅*/   { 1, 2, 1, HALF, 2, 1, 1, 2, 1, 0, 1, HALF, 2, 1, 1, 1, 2, 1 },
        /*비행*/ { 1, 1, 1, 2, HALF, 1, 2, 1, 1, 1, 1, 2, HALF, 1, 1, 1, HALF, 1 },
        /*에스퍼*/ { 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, HALF, 1, 1, 1, 1, 0, HALF, 1 },
        /*벌레*/ { 1, HALF, 1, 2, 1, 1, HALF, HALF, 1, HALF, 2, 1, 1, HALF, 1, 2, HALF, HALF },
        /*바위*/ { 1, 2, 1, 1, 1, 2, HALF, 1, HALF, 2, 1, 2, 1, 1, 1, 1, HALF, 1 },
        /*고스트*/ { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 2, 1, HALF, 1, 1 },
        /*드래곤*/ { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, HALF, 0 },
        /*악*/   { 1, 1, 1, 1, 1, 1, HALF, 1, 1, 1, 2, 1, 1, 2, 1, HALF, 1, HALF },
        /*강철*/ { 1, HALF, HALF, 1, HALF, 2, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, HALF, 2 },
        /*페어리*/ { 1, HALF, 1, 1, 1, 1, 2, HALF, 1, 1, 1, 1, 1, 1, 2, 2, HALF, 1 },
    };

    // WeaponAbilityType 공격(0~17) 순서. MonsterType 값 순서와 다르다.
    private static readonly MonsterType[] ABILITY_ATTACK_TYPES =
    {
        MonsterType.Normal,
        MonsterType.Fighting,
        MonsterType.Flying,
        MonsterType.Poison,
        MonsterType.Ground,
        MonsterType.Rock,
        MonsterType.Bug,
        MonsterType.Ghost,
        MonsterType.Steel,
        MonsterType.Fire,
        MonsterType.Water,
        MonsterType.Grass,
        MonsterType.Electric,
        MonsterType.Psychic,
        MonsterType.Ice,
        MonsterType.Dragon,
        MonsterType.Dark,
        MonsterType.Fairy
    };

    /// <summary>
    /// 공격 능력과 그 진화의 상성 타입. 패시브면 false.
    /// </summary>
    public static bool TryGetAttackType(WeaponAbilityType abilityType, out MonsterType attackType)
    {
        var index = (int)abilityType;
        var evolutionStart = (int)WeaponAbilityType.NormalAttackEvolution;
        if (index >= evolutionStart && index <= (int)WeaponAbilityType.FairyAttackEvolution)
        {
            index -= evolutionStart;
        }

        if (index < 0 || index >= ABILITY_ATTACK_TYPES.Length)
        {
            attackType = MonsterType.Normal;
            return false;
        }

        attackType = ABILITY_ATTACK_TYPES[index];
        return true;
    }

    /// <summary>
    /// 공격 타입 하나가 방어 타입 하나에 주는 배율을 돌려준다.
    /// </summary>
    public static float GetMultiplier(MonsterType attack, MonsterType defense)
    {
        return CHART[(int)attack, (int)defense];
    }

    /// <summary>
    /// 방어 타입 1~2개의 배율을 모두 곱한다. 타입이 없으면 1이다.
    /// </summary>
    public static float GetMultiplier(MonsterType attack, MonsterType[] defenderTypes)
    {
        var result = NEUTRAL_MULTIPLIER;
        if (defenderTypes == null)
        {
            return result;
        }

        for (var i = 0; i < defenderTypes.Length; i++)
        {
            result *= GetMultiplier(attack, defenderTypes[i]);
        }

        return result;
    }

    /// <summary>
    /// 공격 타입이 공격자 타입에 있으면 1.5, 없으면 1이다.
    /// </summary>
    public static float GetStab(MonsterType attack, MonsterType[] attackerTypes)
    {
        if (attackerTypes == null)
        {
            return NEUTRAL_MULTIPLIER;
        }

        for (var i = 0; i < attackerTypes.Length; i++)
        {
            if (attackerTypes[i] == attack)
            {
                return STAB_MULTIPLIER;
            }
        }

        return NEUTRAL_MULTIPLIER;
    }
}
