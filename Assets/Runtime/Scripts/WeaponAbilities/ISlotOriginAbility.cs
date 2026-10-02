/// <summary>
/// 시계 칸 포켓몬 위치에서 나가는 점 발사 능력.
/// </summary>
public interface ISlotOriginAbility
{
    WeaponSlot OriginSlot { get; }

    /// <summary>
    /// 발사 기준으로 쓸 장착 칸을 기억한다.
    /// </summary>
    void BindSlot(WeaponSlot slot);
}
