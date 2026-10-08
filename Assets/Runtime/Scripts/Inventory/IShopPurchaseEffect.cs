using UnityEngine;

/// <summary>
/// 상점 구매 연출 단계를 받는 쪽. ShopUi가 차례대로 부른다.
/// </summary>
public interface IShopPurchaseEffect
{
    /// <summary>
    /// 방금 샀다. 가방 칸이 다시 그려진 같은 프레임이다.
    /// </summary>
    void OnPurchased(int offerIndex);

    /// <summary>
    /// 가격 버튼의 볼이 열렸다. ball은 볼 그림이다.
    /// </summary>
    void OnBallOpened(int offerIndex, RectTransform ball);
}
