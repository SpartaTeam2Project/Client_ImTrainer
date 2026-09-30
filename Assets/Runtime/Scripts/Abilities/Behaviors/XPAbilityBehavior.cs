/// <summary>
/// 경험치 배율을 플레이어에 적용한다.
/// </summary>
public class XPAbilityBehavior : AbilityBehavior<XPAbilityData, XPAbilityLevel>
{
    protected override void SetAbilityLevel(int levelId)
    {
        base.SetAbilityLevel(levelId);
        if (AbilityLevel != null && TryGetPlayer(out var player))
        {
            player.RecalculateXpMultiplier(AbilityLevel.XPMultiplier);
        }
    }

    /// <summary>
    /// 배율을 기본값으로 되돌리고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.RecalculateXpMultiplier(1f);
        }

        base.Clear();
    }
}
