/// <summary>
/// 판 카운터 하나를 늘린다. 상자 열기처럼 나중에 붙는 통계가 쓴다. Key는 StageStatKeys다.
/// </summary>
[System.Serializable]
public struct StageCounterAdded
{
    public int PlayerId;
    public string Key;
    public float Amount;

    public StageCounterAdded(int playerId, string key, float amount)
    {
        PlayerId = playerId;
        Key = key;
        Amount = amount;
    }
}
