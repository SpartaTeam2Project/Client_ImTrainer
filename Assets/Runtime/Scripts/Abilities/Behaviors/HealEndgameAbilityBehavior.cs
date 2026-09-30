/// <summary>
/// 한 번 최대 체력 비율만큼 회복한다.
/// </summary>
public class HealEndgameAbilityBehavior : AbilityBehavior<HealEndgameAbilityData, HealEndgameAbilityLevel>
{
    protected override void SetAbilityLevel(int levelId)
    {
        base.SetAbilityLevel(levelId);
        if (AbilityLevel != null && TryGetPlayer(out var player))
        {
            player.RestoreHp(AbilityLevel.HealPersentage);
        }
    }
}
