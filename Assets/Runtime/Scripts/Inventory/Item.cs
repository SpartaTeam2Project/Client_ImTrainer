/// <summary>
/// 포켓몬 한 종류의 정의와, 가방에 들어 있는 한 마리의 성.
/// </summary>
public class Item
{
    public const int STAR_MIN = 1;
    public const int STAR_MAX = 3;
    public const int NO_EVOLUTION = -1;

    /// <summary>
    /// 데이터베이스 순서로 매긴 식별자.
    /// </summary>
    public int uid;

    /// <summary>
    /// 표시 이름.
    /// </summary>
    public string name = string.Empty;

    /// <summary>
    /// 성. 1부터 시작한다.
    /// </summary>
    public int upgradeLevel = STAR_MIN;

    /// <summary>
    /// 한 칸에 쌓이는 최대 수.
    /// </summary>
    public int maxiumStack = 99;

    /// <summary>
    /// 구매와 판매에 쓰는 몬스터볼 가격.
    /// </summary>
    public int price;

    /// <summary>
    /// 3성 합성으로 이어지는 다음 종. 없으면 -1.
    /// </summary>
    public int evolutionUid = NO_EVOLUTION;

    /// <summary>
    /// 정의와 성을 복사한다.
    /// </summary>
    public Item Copy()
    {
        return new Item
        {
            uid = uid,
            name = name,
            upgradeLevel = upgradeLevel,
            maxiumStack = maxiumStack,
            price = price,
            evolutionUid = evolutionUid
        };
    }
}
