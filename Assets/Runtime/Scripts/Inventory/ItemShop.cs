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
    /// 잠기지 않은 칸을 이번 판 상점 풀에서 다시 뽑는다.
    /// 레벨 구간의 가중치로 등급을 고르고, 등급 안에서는 보유 계통의 기본형을 더 자주 고른 뒤, 성을 고른다.
    /// 설정이 없으면 풀에서 균등하게 1성을 뽑는다.
    /// </summary>
    public static void RollOffers(RunInventory run, ItemCatalog catalog, ShopOddsSettings odds, int level)
    {
        var shopPool = run.ShopPool;
        var bracket = odds != null ? odds.GetBracket(level) : null;
        int[] tiers = null;
        HashSet<int> owned = null;
        if (bracket != null)
        {
            tiers = new int[shopPool.Count];
            for (var i = 0; i < shopPool.Count; i++)
            {
                tiers[i] = odds.GetTier(catalog.GetVisual(shopPool[i]));
            }

            owned = CollectOwnedBasics(run, catalog);
        }

        for (var i = 0; i < run.Offers.Length; i++)
        {
            var offer = run.Offers[i];
            if (offer.Locked)
            {
                continue;
            }

            offer.Purchased = false;
            offer.Star = Item.STAR_MIN;
            if (shopPool.Count == 0)
            {
                offer.Uid = ShopOffer.SOLD_UID;
                continue;
            }

            if (bracket == null)
            {
                offer.Uid = shopPool[Random.Range(0, shopPool.Count)];
                continue;
            }

            var index = PickPoolIndex(shopPool, tiers, owned, bracket, odds.OwnedWeightMultiplier);
            offer.Uid = shopPool[index];
            if (tiers[index] != ShopOddsSettings.LEGENDARY_TIER || !odds.LegendaryAlwaysOneStar)
            {
                offer.Star = PickStar(bracket);
            }
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

    /// <summary>
    /// 풀에 종이 있는 등급만 가중치로 고르고, 그 등급 안에서 종을 고른다. 고를 등급이 없으면 풀 전체에서 균등하게 고른다.
    /// </summary>
    private static int PickPoolIndex(IReadOnlyList<int> shopPool, int[] tiers, HashSet<int> owned,
        ShopOddsSettings.LevelBracket bracket, float ownedMultiplier)
    {
        var tierWeights = new float[ShopOddsSettings.TIER_COUNT];
        for (var i = 0; i < tiers.Length; i++)
        {
            tierWeights[tiers[i]] = bracket.GetTierWeight(tiers[i]);
        }

        var tier = PickWeighted(tierWeights);
        if (tier < 0)
        {
            return Random.Range(0, shopPool.Count);
        }

        var total = 0f;
        for (var i = 0; i < tiers.Length; i++)
        {
            if (tiers[i] == tier)
            {
                total += SpeciesWeight(shopPool[i], owned, ownedMultiplier);
            }
        }

        var roll = Random.value * total;
        var last = -1;
        for (var i = 0; i < tiers.Length; i++)
        {
            if (tiers[i] != tier)
            {
                continue;
            }

            last = i;
            roll -= SpeciesWeight(shopPool[i], owned, ownedMultiplier);
            if (roll < 0f)
            {
                return i;
            }
        }

        return last;
    }

    private static float SpeciesWeight(int uid, HashSet<int> owned, float ownedMultiplier)
    {
        return owned != null && owned.Contains(uid) ? ownedMultiplier : 1f;
    }

    private static int PickStar(ShopOddsSettings.LevelBracket bracket)
    {
        var weights = new float[Item.STAR_MAX];
        for (var i = 0; i < weights.Length; i++)
        {
            weights[i] = bracket.GetStarWeight(Item.STAR_MIN + i);
        }

        var index = PickWeighted(weights);
        return index < 0 ? Item.STAR_MIN : Item.STAR_MIN + index;
    }

    /// <summary>
    /// 가중치 비율로 순서를 고른다. 합이 0이면 -1.
    /// </summary>
    private static int PickWeighted(float[] weights)
    {
        var total = 0f;
        for (var i = 0; i < weights.Length; i++)
        {
            total += weights[i];
        }

        if (total <= 0f)
        {
            return -1;
        }

        var roll = Random.value * total;
        var last = -1;
        for (var i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0f)
            {
                continue;
            }

            last = i;
            roll -= weights[i];
            if (roll < 0f)
            {
                return i;
            }
        }

        return last;
    }

    /// <summary>
    /// 가방과 장착 칸에 있는 포켓몬 계통의 기본형 uid.
    /// </summary>
    private static HashSet<int> CollectOwnedBasics(RunInventory run, ItemCatalog catalog)
    {
        var owned = new HashSet<int>();
        AddOwnedBasics(run.Bag, catalog, owned);
        AddOwnedBasics(run.Equipment, catalog, owned);
        return owned;
    }

    private static void AddOwnedBasics(InventoryHolder holder, ItemCatalog catalog, HashSet<int> owned)
    {
        if (holder == null || holder.Stacks == null)
        {
            return;
        }

        for (var i = 0; i < holder.Stacks.Count; i++)
        {
            var stack = holder.Stacks[i];
            if (stack == null || stack.Empty || stack.Item == null)
            {
                continue;
            }

            var basic = catalog.GetEvolutionLine(stack.Item.uid).Get(EvolutionStage.Basic);
            if (basic >= 0)
            {
                owned.Add(basic);
            }
        }
    }
}
