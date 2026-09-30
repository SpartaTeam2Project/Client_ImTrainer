/// <summary>
/// 공격력 배율을 플레이어에 적용한다.
/// </summary>
public class DamageAbilityBehavior : AbilityBehavior<DamageAbilityData, DamageAbilityLevel>
{
    protected override void SetAbilityLevel(int levelId)
    {
        base.SetAbilityLevel(levelId);
        if (AbilityLevel != null && TryGetPlayer(out var player))
        {
            player.RecalculateDamage(AbilityLevel.DamageMultiplier);
        }
    }

    /// <summary>
    /// 배율을 기본값으로 되돌리고 치운다.
    /// </summary>
    public override void Clear()
    {
        if (TryGetPlayer(out var player))
        {
            player.RecalculateDamage(1f);
        }

        base.Clear();
    }
}
