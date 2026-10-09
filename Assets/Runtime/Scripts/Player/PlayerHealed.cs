/// <summary>
/// 플레이어 체력이 회복으로 늘었다. Amount는 실제로 오른 체력이다.
/// </summary>
[System.Serializable]
public struct PlayerHealed
{
    public int PlayerId;
    public float Amount;

    public PlayerHealed(int playerId, float amount)
    {
        PlayerId = playerId;
        Amount = amount;
    }
}
