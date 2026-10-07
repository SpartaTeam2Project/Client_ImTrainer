using System.Collections.Generic;

/// <summary>
/// 종 목록으로 만든 아이템 정의와 진화 계통. uid는 데이터베이스 순서다.
/// </summary>
public class ItemCatalog
{
    public const int EMPTY_UID = -1;
    private const int EVOLUTION_SEARCH_DEPTH = 8;

    private readonly List<Item> _items = new List<Item>();
    private readonly List<MonsterVisualData> _visuals = new List<MonsterVisualData>();
    private readonly List<int> _shopPool = new List<int>();

    public IReadOnlyList<Item> Items => _items;

    /// <summary>
    /// 상점에 진열할 수 있는 종. 시작 가능이면서 다른 종에서 진화해 오지 않는 종만 들어간다.
    /// 스테이지 세대 제한 전의 전체 후보다. 판에서 쓰는 풀은 CollectShopPool로 거른다.
    /// </summary>
    public IReadOnlyList<int> ShopPool => _shopPool;

    /// <summary>
    /// 상점 후보 중 세대 마스크에 드는 종만 담는다. 하나도 없으면 경고하고 전체 후보를 담는다.
    /// </summary>
    public void CollectShopPool(ShopGeneration generations, List<int> result)
    {
        result.Clear();
        for (var i = 0; i < _shopPool.Count; i++)
        {
            var uid = _shopPool[i];
            if (ShopGenerationMask.Contains(generations, _visuals[uid].Generation))
            {
                result.Add(uid);
            }
        }

        if (result.Count == 0 && _shopPool.Count > 0)
        {
            UnityEngine.Debug.LogWarning($"상점 세대 {generations}에 진열할 포켓몬이 없어 전체 세대로 진열합니다.");
            result.AddRange(_shopPool);
        }
    }

    /// <summary>
    /// 데이터베이스 순서로 uid를 다시 매긴다.
    /// </summary>
    public void ReInit(MonsterVisualData[] monsters)
    {
        _items.Clear();
        _visuals.Clear();
        _shopPool.Clear();
        for (var i = 0; i < monsters.Length; i++)
        {
            var visual = monsters[i];
            if (visual == null)
            {
                _visuals.Add(null);
                _items.Add(null);
                continue;
            }

            _visuals.Add(visual);
            _items.Add(new Item
            {
                uid = i,
                name = string.IsNullOrEmpty(visual.MonsterName) ? visual.name : visual.MonsterName,
                upgradeLevel = Item.STAR_MIN,
                maxiumStack = ItemManager.MAX_STACK,
                price = visual.ShopPrice,
                currencyId = visual.ShopCurrencyId,
                evolutionUid = Item.NO_EVOLUTION
            });
        }

        for (var i = 0; i < _visuals.Count; i++)
        {
            var visual = _visuals[i];
            if (visual == null || visual.Evolution == null || _items[i] == null)
            {
                continue;
            }

            _items[i].evolutionUid = FindUid(visual.Evolution);
        }

        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && _visuals[i].Startable && FindPreEvolutionUid(i) < 0)
            {
                _shopPool.Add(i);
            }
        }
    }

    /// <summary>
    /// 일반 진화, 메가진화, 거다이맥스 중 하나로 이 종이 되는 이전 종. 없으면 -1.
    /// </summary>
    public int FindPreEvolutionUid(int uid)
    {
        if (uid < 0)
        {
            return EMPTY_UID;
        }

        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i] == null)
            {
                continue;
            }

            if (_items[i].evolutionUid == uid || FindMegaUid(i) == uid || FindVMaxUid(i) == uid)
            {
                return i;
            }
        }

        return EMPTY_UID;
    }

    /// <summary>
    /// 이 종이 속한 진화 계통을 단계 칸으로 나눈다. 정의가 없으면 모든 칸이 -1이다.
    /// </summary>
    public EvolutionLine GetEvolutionLine(int uid)
    {
        var line = new EvolutionLine(uid);
        if (TryGetItem(uid) == null)
        {
            return line;
        }

        var basic = uid;
        for (var depth = 0; depth < EVOLUTION_SEARCH_DEPTH; depth++)
        {
            var previous = FindPreEvolutionUid(basic);
            if (previous < 0)
            {
                break;
            }

            basic = previous;
        }

        line.Set(EvolutionStage.Basic, basic);
        var stage1 = TryGetItem(basic) != null ? TryGetItem(basic).evolutionUid : EMPTY_UID;
        line.Set(EvolutionStage.Stage1, stage1);
        var stage2 = TryGetItem(stage1) != null ? TryGetItem(stage1).evolutionUid : EMPTY_UID;
        line.Set(EvolutionStage.Stage2, stage2);
        line.Set(EvolutionStage.Mega, FindLastBranch(line, FindMegaUid));
        line.Set(EvolutionStage.VMax, FindLastBranch(line, FindVMaxUid));
        return line;
    }

    /// <summary>
    /// 정의가 있으면 그대로 돌려준다.
    /// </summary>
    public Item TryGetItem(int uid)
    {
        if (uid < 0 || uid >= _items.Count)
        {
            return null;
        }

        return _items[uid];
    }

    /// <summary>
    /// uid에 해당하는 포켓몬 그림. 없으면 null.
    /// </summary>
    public MonsterVisualData GetVisual(int uid)
    {
        if (uid < 0 || uid >= _visuals.Count)
        {
            return null;
        }

        return _visuals[uid];
    }

    /// <summary>
    /// 종 목록의 무기 능력이면 true.
    /// </summary>
    public bool IsCatalogWeaponAbility(WeaponAbilityType abilityType)
    {
        for (var i = 0; i < _visuals.Count; i++)
        {
            var visual = _visuals[i];
            if (visual != null && visual.WeaponAbility != null && visual.WeaponAbility.WeaponAbilityType == abilityType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 정의를 복사해 star성 아이템을 만든다. 정의가 없으면 null.
    /// </summary>
    public Item CreateItem(int uid, int star)
    {
        var source = TryGetItem(uid);
        if (source == null)
        {
            return null;
        }

        var copy = source.Copy();
        copy.upgradeLevel = star;
        copy.price = GetStarPrice(source.price, star);
        return copy;
    }

    public int FindUid(MonsterVisualData visual)
    {
        if (visual == null)
        {
            return EMPTY_UID;
        }

        for (var i = 0; i < _visuals.Count; i++)
        {
            if (_visuals[i] == visual)
            {
                return i;
            }
        }

        return EMPTY_UID;
    }

    /// <summary>
    /// 1성 가격에서 star성 구매가를 낸다. 한 성 오를 때마다 합성에 드는 마릿수만큼 곱한다.
    /// </summary>
    private static int GetStarPrice(int basePrice, int star)
    {
        var price = basePrice;
        for (var i = Item.STAR_MIN; i < star; i++)
        {
            price *= ItemManager.SYNTHESIS_COUNT;
        }

        return price;
    }

    private static int FindLastBranch(EvolutionLine line, System.Func<int, int> branch)
    {
        for (var stage = EvolutionStage.Stage2; stage >= EvolutionStage.Basic; stage--)
        {
            var target = branch(line.Get(stage));
            if (target >= 0)
            {
                return target;
            }
        }

        return EMPTY_UID;
    }

    private int FindMegaUid(int uid)
    {
        var visual = GetVisual(uid);
        return visual != null ? FindUid(visual.MegaEvolution) : EMPTY_UID;
    }

    private int FindVMaxUid(int uid)
    {
        var visual = GetVisual(uid);
        return visual != null ? FindUid(visual.VMaxEvolution) : EMPTY_UID;
    }
}
