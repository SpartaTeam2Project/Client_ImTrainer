/// <summary>
/// 자석 반경 배율을 플레이어에 적용한다.
/// </summary>
public class MagnetAbilityBehavior : AbilityBehavior<MagnetAbilityData, MagnetAbilityLevel>
{
    protected override void SetAbilityLevel(int levelId)
    {
        base.SetAbilityLevel(levelId);
        if (AbilityLevel != null && TryGetPlayer(out var player))
        {
            player.RecalculateMagnetRadius(AbilityLevel.RadiusMultiplier);
        }
    }

    /// <summary>
    /// 배율을 기본값으로 되돌리고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.RecalculateMagnetRadius(1f);
        }

        base.Clear();
    }
}
