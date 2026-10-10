/// <summary>
/// 포켓몬 한 종류의 정의와, 가방에 들어 있는 한 마리의 성.
/// </summary>
public class Item
{
    public const int STAR_MIN = 1;
    public const int STAR_MAX = 3;

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
    /// 이 성의 구매가. 판매가는 ItemManager.GetSellPrice가 계산한다.
    /// </summary>
    public int price;

    /// <summary>
    /// 사고팔 때 쓰는 화폐 id.
    /// </summary>
    public string currencyId = CurrenciesManager.MONSTER_BALL_ID;

    /// <summary>
    /// 다음 진화 종들. 비면 최종 진화, 하나면 3성 합성으로 진화, 둘 이상이면 갈래 진화다.
    /// 종 정의끼리 같은 배열을 나눠 쓰므로 고치지 않는다.
    /// </summary>
    public int[] evolutionUids = System.Array.Empty<int>();

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
            currencyId = currencyId,
            evolutionUids = evolutionUids
        };
    }
}
