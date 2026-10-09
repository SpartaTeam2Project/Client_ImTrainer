using System;

/// <summary>
/// 리더보드 서버로 보내는 판 결과. 필드 이름이 곧 JSON 키다. 바꾸면 Docs/stage-result-api.md도 같이 고친다.
/// 계정은 Bearer 토큰으로 서버가 정하므로 계정 id는 넣지 않는다.
/// </summary>
[Serializable]
public class StageResultRequest
{
    /// <summary>
    /// 계약 버전. 필드를 빼거나 뜻을 바꾸면 올린다.
    /// </summary>
    public const int SCHEMA_VERSION = 1;

    public string runId;
    public int schemaVersion;
    public string clientVersion;
    public string clearedAtUtc;
    public int playerSlot;
    public string stageId;
    public bool cleared;
    public float elapsedSeconds;
    public int killCount;
    public int reachedLevel;
    public string playableId;
    public StageResultStatDto[] playerStats;
    public StageResultStatDto[] counters;
    public StageResultCurrencyDto[] currenciesGained;
    public StageResultCurrencyDto[] currenciesHeld;
    public StageResultPartyDto[] party;
    public StageResultUnitDamageDto[] unitDamages;
}

[Serializable]
public class StageResultStatDto
{
    public string key;
    public float value;
}

[Serializable]
public class StageResultCurrencyDto
{
    public string currencyId;
    public int amount;
}

[Serializable]
public class StageResultPartyDto
{
    public int slot;
    public int memberId;
    public string monsterId;
    public int dexNumber;
    public int upgradeLevel;
}

[Serializable]
public class StageResultUnitDamageDto
{
    public int memberId;
    public string monsterId;
    public int dexNumber;
    public int upgradeLevel;
    public string abilityType;
    public float totalDamage;
    public float secondsInParty;
    public float recentDamage;
    public float recentSeconds;
    public bool inPartyAtEnd;
}

/// <summary>
/// 서버 응답 자리. 서버 계약이 정해지면 맞춘다.
/// </summary>
[Serializable]
public class StageResultResponse
{
    public string runId;
    public bool accepted;
}
