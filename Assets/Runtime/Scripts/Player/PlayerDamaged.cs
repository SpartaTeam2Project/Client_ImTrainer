/// <summary>
/// 플레이어 체력이 피해로 줄었다.
/// </summary>
[System.Serializable]
public struct PlayerDamaged
{
    public int PlayerId;
    public float Amount;
    public float CurrentHealth;
    public float MaxHealth;

    public PlayerDamaged(int playerId, float amount, float currentHealth, float maxHealth)
    {
        PlayerId = playerId;
        Amount = amount;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
    }
}
