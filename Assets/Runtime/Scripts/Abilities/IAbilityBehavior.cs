/// <summary>
/// 능력 프리팹이 구현하는 동작.
/// </summary>
public interface IAbilityBehavior
{
    AbilityType AbilityType { get; }

    AbilityData AbilityData { get; }

    int LevelId { get; }

    /// <summary>
    /// 플레이어에게 능력을 붙이고 레벨 수치를 적용한다.
    /// </summary>
    void Init(int playerId, AbilityData data, int level);

    /// <summary>
    /// 레벨 수치를 다시 적용한다.
    /// </summary>
    void ApplyLevel(int level);

    /// <summary>
    /// 능력을 치운다.
    /// </summary>
    void Clear();
}
