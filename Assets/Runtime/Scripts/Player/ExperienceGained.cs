/// <summary>
/// 플레이어가 경험치를 얻었다. Amount는 배율을 곱한 값이다.
/// </summary>
[System.Serializable]
public struct ExperienceGained
{
    public int PlayerId;
    public float Amount;

    public ExperienceGained(int playerId, float amount)
    {
        PlayerId = playerId;
        Amount = amount;
    }
}
