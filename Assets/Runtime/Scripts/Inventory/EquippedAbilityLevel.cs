/// <summary>
/// 장착 포켓몬 하나의 무기 능력과 성으로 고른 레벨 인덱스. 점 발사는 칸을 가진다.
/// </summary>
public struct EquippedAbilityLevel
{
    public const int UNSLOTTED = -1;

    public WeaponAbilityData Ability;
    public int LevelIndex;
    public int Slot;

    public EquippedAbilityLevel(WeaponAbilityData ability, int levelIndex, int slot)
    {
        Ability = ability;
        LevelIndex = levelIndex;
        Slot = slot;
    }
}
