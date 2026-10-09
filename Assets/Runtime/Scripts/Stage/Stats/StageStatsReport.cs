/// <summary>
/// StageStatsManager가 판 끝에 넘기는 통계 묶음. StageResult가 그대로 담는다.
/// </summary>
public struct StageStatsReport
{
    public CurrencyAmount[] CurrenciesGained;
    public StageStatValue[] PlayerStats;
    public StageStatValue[] Counters;
    public StageUnitDamage[] UnitDamages;
    public StagePartyMember[] Party;
}
