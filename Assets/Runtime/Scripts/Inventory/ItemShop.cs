using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 판 상점 칸의 진열, 잠금, 구매 준비와 판매가.
/// </summary>
internal static class ItemShop
{
    /// <summary>
    /// 한 마리를 팔 때 돌려받는 화폐 수. 화폐는 item.currencyId다.
    /// </summary>
    public static int GetSellPrice(Item item)
    {
        if (item == null)
        {
            return 0;
        }

        var price = Mathf.FloorToInt(item.price * ItemManager.SELL_PRICE_RATE);
        return price < ItemManager.MIN_SELL_PRICE ? ItemManager.MIN_SELL_PRICE : price;
    }

    /// <summary>
    /// 잠기지 않은 칸을 상점 풀에서 다시 뽑는다.
    /// </summary>
    public static void RollOffers(RunInventory run, IReadOnlyList<int> shopPool)
    {
        for (var i = 0; i < run.Offers.Length; i++)
        {
            var offer = run.Offers[i];
            if (offer.Locked)
            {
                continue;
            }

            offer.Uid = shopPool.Count > 0 ? shopPool[Random.Range(0, shopPool.Count)] : ShopOffer.SOLD_UID;
            offer.Star = Item.STAR_MIN;
            offer.Purchased = false;
        }
    }

    /// <summary>
    /// 아직 팔리지 않은 칸의 잠금을 바꾼다.
    /// </summary>
    public static bool TryToggleLock(RunInventory run, int offerIndex)
    {
        if (!IsOpenOffer(run, offerIndex))
        {
            return false;
        }

        run.Offers[offerIndex].Locked = !run.Offers[offerIndex].Locked;
        return true;
    }

    /// <summary>
    /// 칸의 포켓몬을 만들고 가방에 들어가는지 본다. 몬스터볼은 빼지 않는다.
    /// </summary>
    public static bool TryPrepareOffer(RunInventory run, ItemCatalog catalog, int offerIndex, out Item item, out string message)
    {
        item = null;
        message = "살 수 없는 포켓몬입니다.";
        if (!IsOpenOffer(run, offerIndex))
        {
            return false;
        }

        var offer = run.Offers[offerIndex];
        item = catalog.CreateItem(offer.Uid, offer.Star);
        if (item == null)
        {
            return false;
        }

        if (!run.Bag.CanAccept(item, 1))
        {
            message = "가방이 가득 찼습니다.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    /// <summary>
    /// 값을 치른 포켓몬을 가방에 넣고 칸을 산 칸으로 표시한다.
    /// </summary>
    public static void CompletePurchase(RunInventory run, int offerIndex, Item item)
    {
        run.Bag.AddItem(item, 1);
        var offer = run.Offers[offerIndex];
        offer.Purchased = true;
        offer.Locked = false;
    }

    private static bool IsOpenOffer(RunInventory run, int offerIndex)
    {
        return run != null && offerIndex >= 0 && offerIndex < run.Offers.Length && !run.Offers[offerIndex].Sold;
    }
}
