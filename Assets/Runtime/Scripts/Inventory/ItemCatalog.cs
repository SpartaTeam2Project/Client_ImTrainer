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
    private EvolutionGraph _evolutions = new EvolutionGraph(null, null);

    public IReadOnlyList<Item> Items => _items;

    /// <summary>
    /// 상점에 진열할 수 있는 종. 시작 가능이면서 다른 종에서 진화해 오지 않는 종만 들어간다.
    /// 스테이지 세대 제한 전의 전체 후보다. 판에서 쓰는 풀은 CollectShopPool로 거른다.
    /// </summary>
    public IReadOnlyList<int> ShopPool => _shopPool;

    /// <summary>
    /// 상점 후보 중 계통의 어느 종이든 세대 마스크에 드는 종만 담는다. 1세대만 켜도 피카츄의 이전 종인 피츄가 나온다.
    /// 하나도 없으면 경고하고 전체 후보를 담는다.
    /// </summary>
    public void CollectShopPool(ShopGeneration generations, List<int> result)
    {
        result.Clear();
        for (var i = 0; i < _shopPool.Count; i++)
        {
            var uid = _shopPool[i];
            if (((int)generations & _evolutions.GetLineGenerationMask(uid)) != 0)
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
        monsters = AppendEvolutions(monsters);
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
                currencyId = visual.ShopCurrencyId
            });
        }

        var next = new int[_visuals.Count][];
        var generations = new int[_visuals.Count];
        for (var i = 0; i < _visuals.Count; i++)
        {
            var visual = _visuals[i];
            if (visual == null || _items[i] == null)
            {
                continue;
            }

            generations[i] = visual.Generation;
            next[i] = FindUids(visual.Evolutions);
            _items[i].evolutionUids = next[i];
        }

        _evolutions = new EvolutionGraph(next, generations);

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

        var previous = _evolutions.FindPrevious(uid);
        if (previous >= 0)
        {
            return previous;
        }

        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && (FindMegaUid(i) == uid || FindVMaxUid(i) == uid || HasExtraMega(i, uid)))
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

        // 메가진화나 거다이맥스 종을 고르면 그 원래 종의 계통을 보여 준다.
        var lineUid = uid;
        for (var depth = 0; depth < EVOLUTION_SEARCH_DEPTH && _evolutions.FindPrevious(lineUid) < 0; depth++)
        {
            var previous = FindPreEvolutionUid(lineUid);
            if (previous < 0)
            {
                break;
            }

            lineUid = previous;
        }

        _evolutions.FillLine(ref line, lineUid);
        line.Set(EvolutionStage.Mega, FindLastBranch(line, FindMegaUid));
        FillExtraMegas(ref line);
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
    /// 데이터베이스는 스토리지에 보일 종만 담으므로, 거기서 이어지는 진화, 메가진화, 거다이맥스 종을 뒤에 붙인다.
    /// 데이터베이스 종의 uid는 그대로다.
    /// </summary>
    private static MonsterVisualData[] AppendEvolutions(MonsterVisualData[] monsters)
    {
        var result = new List<MonsterVisualData>(monsters);
        var included = new HashSet<MonsterVisualData>();
        foreach (var monster in monsters)
        {
            if (monster != null)
            {
                included.Add(monster);
            }
        }

        for (var i = 0; i < result.Count; i++)
        {
            var visual = result[i];
            if (visual == null)
            {
                continue;
            }

            foreach (var next in visual.Evolutions)
            {
                AppendIfNew(next, result, included);
            }

            AppendIfNew(visual.MegaEvolution, result, included);
            foreach (var extra in visual.ExtraMegaEvolutions)
            {
                AppendIfNew(extra, result, included);
            }

            AppendIfNew(visual.VMaxEvolution, result, included);
        }

        return result.ToArray();
    }

    private static void AppendIfNew(MonsterVisualData visual, List<MonsterVisualData> result, HashSet<MonsterVisualData> included)
    {
        if (visual != null && included.Add(visual))
        {
            result.Add(visual);
        }
    }

    /// <summary>
    /// 목록에 있는 종의 uid. 카탈로그에 없는 종은 뺀다.
    /// </summary>
    private int[] FindUids(IReadOnlyList<MonsterVisualData> visuals)
    {
        if (visuals == null || visuals.Count == 0)
        {
            return System.Array.Empty<int>();
        }

        var uids = new List<int>(visuals.Count);
        for (var i = 0; i < visuals.Count; i++)
        {
            var uid = FindUid(visuals[i]);
            if (uid >= 0 && !uids.Contains(uid))
            {
                uids.Add(uid);
            }
        }

        return uids.ToArray();
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

    /// <summary>
    /// 메가진화 칸을 정한 단계와 같은 단계에서 나머지 메가진화 갈래(Y, Z)를 넣는다.
    /// </summary>
    private void FillExtraMegas(ref EvolutionLine line)
    {
        for (var stage = EvolutionStage.Stage2; stage >= EvolutionStage.Basic; stage--)
        {
            var visual = GetVisual(line.Get(stage));
            if (visual == null || (visual.MegaEvolution == null && visual.ExtraMegaEvolutions.Count == 0))
            {
                continue;
            }

            foreach (var extra in visual.ExtraMegaEvolutions)
            {
                line.AddExtraMega(FindUid(extra));
            }

            return;
        }
    }

    private bool HasExtraMega(int uid, int megaUid)
    {
        var visual = GetVisual(uid);
        if (visual == null)
        {
            return false;
        }

        foreach (var extra in visual.ExtraMegaEvolutions)
        {
            if (FindUid(extra) == megaUid)
            {
                return true;
            }
        }

        return false;
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
