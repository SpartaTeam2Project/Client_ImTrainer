/// <summary>
/// 지속 시간 배율을 플레이어에 적용한다.
/// </summary>
public class DurationAbilityBehavior : AbilityBehavior<DurationAbilityData, DurationAbilityLevel>
{
    protected override void SetAbilityLevel(int levelId)
    {
        base.SetAbilityLevel(levelId);
        if (AbilityLevel != null && TryGetPlayer(out var player))
        {
            player.RecalculateDurationMultiplier(AbilityLevel.DurationMultiplier);
        }
    }

    /// <summary>
    /// 배율을 기본값으로 되돌리고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.RecalculateDurationMultiplier(1f);
        }

        base.Clear();
    }
}
