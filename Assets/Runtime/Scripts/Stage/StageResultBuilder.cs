using System;
using System.Globalization;

/// <summary>
/// 판 끝의 값들을 StageResult 하나로 묶는다. 판 번호와 끝난 시각도 여기서 정한다.
/// </summary>
public static class StageResultBuilder
{
    private const string UTC_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public static StageResult Build(int playerId, StageData stageData, bool cleared, int killCount, float elapsedSeconds,
        CurrencyAmount[] currencies, StageStatsReport report)
    {
        var info = new StageResultInfo
        {
            RunId = Guid.NewGuid().ToString(),
            ClearedAtUtc = DateTime.UtcNow.ToString(UTC_FORMAT, CultureInfo.InvariantCulture),
            PlayerId = playerId,
            StageId = stageData != null ? stageData.StageId : string.Empty,
            StageName = stageData != null ? stageData.StageName : string.Empty,
            PlayableId = ResolvePlayableId(),
            Cleared = cleared,
            KillCount = killCount,
            ElapsedSeconds = elapsedSeconds,
            ReachedLevel = ResolveLevel(report)
        };

        return new StageResult(info, currencies, report);
    }

    private static string ResolvePlayableId()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            return string.Empty;
        }

        var playable = account.ResolveSelectedPlayable();
        return playable != null ? playable.name : string.Empty;
    }

    private static int ResolveLevel(StageStatsReport report)
    {
        var counters = report.Counters;
        if (counters == null)
        {
            return 0;
        }

        for (var i = 0; i < counters.Length; i++)
        {
            if (counters[i].Key == StageStatKeys.REACHED_LEVEL)
            {
                return (int)counters[i].Value;
            }
        }

        return 0;
    }
}
