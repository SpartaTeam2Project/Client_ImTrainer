/// <summary>
/// 플레이어 한 명의 판 통계. 카운터, 재화 획득, 개체별 피해 장부를 가진다.
/// </summary>
internal sealed class StageStatsTracker
{
    public readonly StageCounterBook Counters = new StageCounterBook();
    public readonly StageCounterBook CurrenciesGained = new StageCounterBook();
    public readonly StageUnitDamageBook Units = new StageUnitDamageBook();

    public StageStatsReport Finish(float now, StageStatValue[] playerStats, StagePartyMember[] party)
    {
        return new StageStatsReport
        {
            CurrenciesGained = CurrenciesGained.ExportCurrencies(),
            PlayerStats = playerStats,
            Counters = Counters.Export(),
            UnitDamages = Units.Export(now),
            Party = party
        };
    }
}
