using UnityEngine;

/// <summary>
/// 게임 안 판 결과를 서버 요청 DTO로 바꾼다. enum은 이름 문자열로, 포켓몬은 판마다 바뀌는 uid 대신
/// 데이터 에셋 이름과 도감 번호로 보낸다.
/// </summary>
public static class StageResultMapper
{
    public static StageResultRequest ToRequest(StageResult result, ItemManager itemManager)
    {
        return new StageResultRequest
        {
            runId = result.RunId,
            schemaVersion = StageResultRequest.SCHEMA_VERSION,
            clientVersion = Application.version,
            clearedAtUtc = result.ClearedAtUtc,
            playerSlot = result.PlayerId,
            stageId = result.StageId,
            cleared = result.Cleared,
            elapsedSeconds = result.ElapsedSeconds,
            killCount = result.KillCount,
            reachedLevel = result.ReachedLevel,
            playableId = result.PlayableId,
            playerStats = ToStats(result.PlayerStats),
            counters = ToStats(result.Counters),
            currenciesGained = ToCurrencies(result.CurrenciesGained),
            currenciesHeld = ToCurrencies(result.Currencies),
            party = ToParty(result.Party, itemManager),
            unitDamages = ToUnitDamages(result.UnitDamages, itemManager)
        };
    }

    private static StageResultStatDto[] ToStats(StageStatValue[] values)
    {
        var dtos = new StageResultStatDto[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            dtos[i] = new StageResultStatDto { key = values[i].Key, value = values[i].Value };
        }

        return dtos;
    }

    private static StageResultCurrencyDto[] ToCurrencies(CurrencyAmount[] amounts)
    {
        var dtos = new StageResultCurrencyDto[amounts.Length];
        for (var i = 0; i < amounts.Length; i++)
        {
            dtos[i] = new StageResultCurrencyDto { currencyId = amounts[i].CurrencyId, amount = amounts[i].Amount };
        }

        return dtos;
    }

    private static StageResultPartyDto[] ToParty(StagePartyMember[] party, ItemManager itemManager)
    {
        var dtos = new StageResultPartyDto[party.Length];
        for (var i = 0; i < party.Length; i++)
        {
            var visual = ResolveVisual(itemManager, party[i].Uid);
            dtos[i] = new StageResultPartyDto
            {
                slot = party[i].Slot,
                memberId = party[i].MemberId,
                monsterId = visual != null ? visual.name : string.Empty,
                dexNumber = visual != null ? visual.DexNumber : 0,
                upgradeLevel = party[i].UpgradeLevel
            };
        }

        return dtos;
    }

    private static StageResultUnitDamageDto[] ToUnitDamages(StageUnitDamage[] units, ItemManager itemManager)
    {
        var dtos = new StageResultUnitDamageDto[units.Length];
        for (var i = 0; i < units.Length; i++)
        {
            var unit = units[i];
            var visual = ResolveVisual(itemManager, unit.Uid);
            dtos[i] = new StageResultUnitDamageDto
            {
                memberId = unit.MemberId,
                monsterId = visual != null ? visual.name : string.Empty,
                dexNumber = visual != null ? visual.DexNumber : 0,
                upgradeLevel = unit.UpgradeLevel,
                abilityType = unit.AbilityType.ToString(),
                totalDamage = unit.TotalDamage,
                secondsInParty = unit.SecondsInParty,
                recentDamage = unit.RecentDamage,
                recentSeconds = unit.RecentSeconds,
                inPartyAtEnd = unit.InPartyAtEnd
            };
        }

        return dtos;
    }

    private static MonsterVisualData ResolveVisual(ItemManager itemManager, int uid)
    {
        return itemManager != null && uid >= 0 ? itemManager.GetVisual(uid) : null;
    }
}
