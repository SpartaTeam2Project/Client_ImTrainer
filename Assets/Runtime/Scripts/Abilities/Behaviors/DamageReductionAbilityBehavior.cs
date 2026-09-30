/// <summary>
/// 피해 감소를 플레이어에 적용한다.
/// </summary>
public class DamageReductionAbilityBehavior : AbilityBehavior<DamageReductionAbilityData, DamageReductionAbilityLevel>
{
    protected override void SetAbilityLevel(int levelId)
    {
        base.SetAbilityLevel(levelId);
        if (AbilityLevel != null && TryGetPlayer(out var player))
        {
            player.RecalculateDamageReduction(AbilityLevel.DamageReductionPercent);
        }
    }

    /// <summary>
    /// 감소를 없애고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.RecalculateDamageReduction(0f);
        }

        base.Clear();
    }
}
