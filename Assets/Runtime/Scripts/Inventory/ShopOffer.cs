/// <summary>
/// 상점 한 칸. 판 동안 플레이어마다 유지된다.
/// </summary>
public class ShopOffer
{
    public const int SOLD_UID = -1;

    /// <summary>
    /// 파는 종의 uid. 진열할 포켓몬이 없으면 SOLD_UID. 산 뒤에도 산 포켓몬을 가리킨다.
    /// </summary>
    public int Uid = SOLD_UID;

    /// <summary>
    /// true면 새로고침해도 바뀌지 않는다.
    /// </summary>
    public bool Locked;

    /// <summary>
    /// 파는 성. 지금은 늘 1성이고, 높은 성 진열 규칙은 RollOffers에서 정한다.
    /// </summary>
    public int Star = Item.STAR_MIN;

    /// <summary>
    /// 이번 진열에서 이미 산 칸. 새로 뽑으면 false로 돌아간다.
    /// </summary>
    public bool Purchased;

    public bool Sold => Purchased || Uid < 0;
}
