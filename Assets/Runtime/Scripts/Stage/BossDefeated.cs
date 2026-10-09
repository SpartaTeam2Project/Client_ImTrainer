/// <summary>
/// 보스 한 마리가 쓰러졌다.
/// </summary>
[System.Serializable]
public struct BossDefeated
{
    public int PlayerId;

    public BossDefeated(int playerId)
    {
        PlayerId = playerId;
    }
}
