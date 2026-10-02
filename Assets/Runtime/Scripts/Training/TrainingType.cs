/// <summary>
/// 트레이닝 종류. 값은 저장 키에 쓰이므로 바꾸지 않는다.
/// </summary>
public enum TrainingType
{
    Experience = 0,
    MoveSpeed = 1,
    LastStand = 2,
    StrictSelection = 3,
    MaxHp = 4,
    ShardBonus = 5,
    Attack = 6,
    HeadStart = 7,
    PreparedChoice = 8,
}

/// <summary>
/// 트레이닝 효과 수치를 칸에 그리는 형식.
/// </summary>
public enum TrainingValueDisplay
{
    PercentBonus = 0,
    PercentReduction = 1,
    Count = 2,
}
