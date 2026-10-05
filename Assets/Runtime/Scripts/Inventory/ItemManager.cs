using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 판 안의 포켓몬 가방과 장착을 플레이어 식별자마다 들고 있다.
/// </summary>
public class ItemManager : BaseManager
{
    public const int INVENTORY_SIZE = 20;
    public const int MAX_STACK = 99;
    public const int BUY_PRICE = 10;
    public const int SYNTHESIS_COUNT = 3;
    public const int MIN_EQUIPPED = 1;
    public const int SHOP_OFFER_COUNT = 4;
    public const int SHOP_REFRESH_PRICE = 1;

    private const int EMPTY_UID = -1;
    private const int EVOLUTION_SEARCH_DEPTH = 8;

    [SerializeField] private MonsterDatabase _monsters;
    [SerializeField] private InventoryUi _inventoryWindow;

    private InventoryUi _window;
    private readonly List<Item> _items = new List<Item>();
    private readonly List<MonsterVisualData> _visuals = new List<MonsterVisualData>();
    private readonly List<int> _shopPool = new List<int>();
    private readonly Dictionary<int, RunInventory> _runs = new Dictionary<int, RunInventory>();

    public string LastMessage { get; private set; } = string.Empty;

    public IReadOnlyList<Item> Items => _items;

    /// <summary>
    /// 종 목록으로 아이템 정의를 만든다.
    /// </summary>
    public override UniTask InitializeAsync()
    {
        ReInit();
        EnsureWindow();
        return base.InitializeAsync();
    }

    /// <summary>
    /// 판 가방을 비운다.
    /// </summary>
    public override void Cleanup()
    {
        _runs.Clear();
        base.Cleanup();
    }

    /// <summary>
    /// 데이터베이스 순서로 uid를 다시 매긴다.
    /// </summary>
    public void ReInit()
    {
        _items.Clear();
        _visuals.Clear();
        _shopPool.Clear();
        var monsters = _monsters != null ? _monsters.Monsters : System.Array.Empty<MonsterVisualData>();
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
                maxiumStack = MAX_STACK,
                price = BUY_PRICE,
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
            if (_items[i] != null && FindPreEvolutionUid(i) < 0)
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
    /// 이번 판 가방이 있으면 true.
    /// </summary>
    public bool HasRun(int playerId)
    {
        return _runs.ContainsKey(playerId);
    }

    /// <summary>
    /// 가방. 판이 없으면 null.
    /// </summary>
    public InventoryHolder GetInventory(int playerId)
    {
        return _runs.TryGetValue(playerId, out var run) ? run.Bag : null;
    }

    /// <summary>
    /// 장착 칸. 판이 없으면 null.
    /// </summary>
    public InventoryHolder GetEquipment(int playerId)
    {
        return _runs.TryGetValue(playerId, out var run) ? run.Equipment : null;
    }

    /// <summary>
    /// 시작 포켓몬 한 마리를 비용 없이 넣고 시작 칸에 장착한다.
    /// </summary>
    public void BeginRun(int playerId)
    {
        if (_items.Count == 0)
        {
            ReInit();
        }

        var run = CreateRun();
        _runs[playerId] = run;
        RollOffers(run);
        var visual = ResolveStartingVisual();
        var uid = FindUid(visual);
        if (uid < 0)
        {
            LastMessage = "시작 포켓몬을 찾지 못했습니다.";
            Debug.LogWarning(LastMessage);
            FinishBag(playerId);
            PublishAllEquipment(playerId);
            return;
        }

        var created = CreateItem(uid, Item.STAR_MIN);
        run.Bag.AddItem(created, 1);
        RecordObtained(playerId, uid);
        var index = run.Bag.FindIndex(uid, Item.STAR_MIN);
        if (!TryMoveToSlot(run, index, (int)WeaponSlot.Hour3))
        {
            LastMessage = "시작 포켓몬을 장착하지 못했습니다.";
            Debug.LogWarning(LastMessage);
        }

        FinishBag(playerId);
        PublishAllEquipment(playerId);
    }

    /// <summary>
    /// 가방과 장착 데이터를 지운다. 결과 연출 중에는 씬의 포켓몬을 남긴다.
    /// </summary>
    public void EndRun(int playerId, bool keepSceneWeapons = false)
    {
        _runs.Remove(playerId);
        if (!keepSceneWeapons)
        {
            PublishAllEquipment(playerId);
        }

        PublishInventory(playerId);
        if (keepSceneWeapons || Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        playerManager.ClearEquipped();
    }

    /// <summary>
    /// 상점 칸. 판이 없으면 null.
    /// </summary>
    public IReadOnlyList<ShopOffer> GetShopOffers(int playerId)
    {
        return _runs.TryGetValue(playerId, out var run) ? run.Offers : null;
    }

    /// <summary>
    /// 몬스터볼을 내고 잠기지 않은 상점 칸을 다시 뽑는다.
    /// </summary>
    public bool TryRefreshShop(int playerId)
    {
        var run = GetRun(playerId);
        if (run == null)
        {
            return false;
        }

        if (!Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies)
            || !currencies.TryWithdraw(playerId, CurrenciesManager.MONSTER_BALL_ID, SHOP_REFRESH_PRICE, false))
        {
            LastMessage = "몬스터볼이 부족합니다.";
            PublishInventory(playerId);
            return false;
        }

        RollOffers(run);
        LastMessage = "상점을 새로 고쳤습니다.";
        PublishInventory(playerId);
        return true;
    }

    /// <summary>
    /// 상점 칸 잠금을 바꾼다. 잠긴 칸은 새로고침에서 빠진다.
    /// </summary>
    public void ToggleShopLock(int playerId, int offerIndex)
    {
        var run = GetRun(playerId);
        if (run == null || offerIndex < 0 || offerIndex >= run.Offers.Length || run.Offers[offerIndex].Sold)
        {
            return;
        }

        run.Offers[offerIndex].Locked = !run.Offers[offerIndex].Locked;
        PublishInventory(playerId);
    }

    /// <summary>
    /// 상점 칸의 1성 한 마리를 몬스터볼로 사서 가방에 넣는다. 산 칸은 비운다.
    /// </summary>
    public bool TryPurchase(int playerId, int offerIndex)
    {
        var run = GetRun(playerId);
        if (run == null || offerIndex < 0 || offerIndex >= run.Offers.Length || run.Offers[offerIndex].Sold)
        {
            LastMessage = "살 수 없는 포켓몬입니다.";
            return false;
        }

        var offer = run.Offers[offerIndex];
        var created = CreateItem(offer.Uid, Item.STAR_MIN);
        if (created == null)
        {
            LastMessage = "살 수 없는 포켓몬입니다.";
            return false;
        }

        if (!run.Bag.CanAccept(created, 1))
        {
            LastMessage = "가방이 가득 찼습니다.";
            return false;
        }

        if (!Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies)
            || !currencies.TryWithdraw(playerId, CurrenciesManager.MONSTER_BALL_ID, created.price, false))
        {
            LastMessage = "몬스터볼이 부족합니다.";
            return false;
        }

        run.Bag.AddItem(created, 1);
        RecordObtained(playerId, created.uid);
        offer.Uid = ShopOffer.SOLD_UID;
        offer.Locked = false;
        LastMessage = created.name + "을 가방에 넣었습니다.";
        FinishBag(playerId);
        return true;
    }

    /// <summary>
    /// 가방의 같은 종, 같은 성 세 마리를 합성한다.
    /// </summary>
    public bool TrySynthesize(int playerId, int uid, int upgradeLevel)
    {
        var run = GetRun(playerId);
        var source = TryGetItem(uid);
        if (run == null || source == null)
        {
            LastMessage = "합성할 포켓몬이 없습니다.";
            return false;
        }

        if (run.Bag.GetItemNumber(uid, upgradeLevel) < SYNTHESIS_COUNT)
        {
            LastMessage = "같은 성 세 마리가 필요합니다.";
            return false;
        }

        Item result;
        if (upgradeLevel < Item.STAR_MAX)
        {
            result = CreateItem(uid, upgradeLevel + 1);
        }
        else if (source.evolutionUid >= 0)
        {
            result = CreateItem(source.evolutionUid, Item.STAR_MIN);
        }
        else
        {
            LastMessage = "다음 포켓몬이 없습니다.";
            return false;
        }

        if (result == null)
        {
            LastMessage = "합성 결과를 만들지 못했습니다.";
            return false;
        }

        var snapshot = run.Bag.Capture();
        run.Bag.RemoveItem(uid, upgradeLevel, SYNTHESIS_COUNT);
        var leftover = run.Bag.AddItem(result, 1);
        if (!leftover.Empty)
        {
            run.Bag.Restore(snapshot);
            LastMessage = "가방이 가득 찼습니다.";
            return false;
        }

        RecordObtained(playerId, result.uid);
        LastMessage = result.name + " " + result.upgradeLevel + "성이 되었습니다.";
        FinishBag(playerId);
        return true;
    }

    /// <summary>
    /// 가방 칸 한 마리를 빈 시계 칸에 장착한다.
    /// </summary>
    public bool TryEquip(int playerId, int bagIndex)
    {
        var run = GetRun(playerId);
        if (run == null || bagIndex < 0 || bagIndex >= run.Bag.Stacks.Count || run.Bag.Stacks[bagIndex].Empty)
        {
            LastMessage = "장착할 포켓몬이 없습니다.";
            return false;
        }

        var slot = run.Equipment.FindEmptyIndex();
        if (slot < 0)
        {
            LastMessage = "장착 칸이 가득 찼습니다.";
            return false;
        }

        if (!TryMoveToSlot(run, bagIndex, slot))
        {
            LastMessage = "장착하지 못했습니다.";
            return false;
        }

        LastMessage = "장착했습니다.";
        FinishBag(playerId);
        PublishEquipmentSlot(playerId, slot);
        return true;
    }

    /// <summary>
    /// 장착 칸을 가방으로 되돌린다. 마지막 한 마리는 해제할 수 없다.
    /// </summary>
    public bool TryUnequip(int playerId, int slot)
    {
        var run = GetRun(playerId);
        if (!TryTakeEquipped(run, slot, out var item))
        {
            return false;
        }

        if (!run.Bag.CanAccept(item, 1))
        {
            LastMessage = "가방이 가득 찼습니다.";
            return false;
        }

        run.Equipment.ClearSlot(slot);
        run.Bag.AddItem(item, 1);
        LastMessage = "해제했습니다.";
        FinishBag(playerId);
        PublishEquipmentSlot(playerId, slot);
        return true;
    }

    /// <summary>
    /// 가방 칸을 비우고 가격만큼 몬스터볼을 돌려준다.
    /// </summary>
    public bool TrySell(int playerId, int bagIndex)
    {
        var run = GetRun(playerId);
        if (!TryTakeBag(run, bagIndex, out var item, out var number))
        {
            LastMessage = "판매할 포켓몬이 없습니다.";
            return false;
        }

        Refund(playerId, item.price * number);
        LastMessage = "판매했습니다.";
        FinishBag(playerId);
        return true;
    }

    /// <summary>
    /// 가방 칸을 환급 없이 비운다.
    /// </summary>
    public bool TryDiscard(int playerId, int bagIndex)
    {
        var run = GetRun(playerId);
        if (!TryTakeBag(run, bagIndex, out _, out _))
        {
            LastMessage = "버릴 포켓몬이 없습니다.";
            return false;
        }

        LastMessage = "버렸습니다.";
        FinishBag(playerId);
        return true;
    }

    /// <summary>
    /// 장착 칸을 팔아 몬스터볼을 돌려준다. 마지막 한 마리는 팔 수 없다.
    /// </summary>
    public bool TrySellEquipped(int playerId, int slot)
    {
        var run = GetRun(playerId);
        if (!TryTakeEquipped(run, slot, out var item))
        {
            return false;
        }

        run.Equipment.ClearSlot(slot);
        Refund(playerId, item.price);
        LastMessage = "판매했습니다.";
        PublishInventory(playerId);
        PublishEquipmentSlot(playerId, slot);
        return true;
    }

    /// <summary>
    /// 장착 칸을 환급 없이 비운다. 마지막 한 마리는 버릴 수 없다.
    /// </summary>
    public bool TryDiscardEquipped(int playerId, int slot)
    {
        var run = GetRun(playerId);
        if (!TryTakeEquipped(run, slot, out _))
        {
            return false;
        }

        run.Equipment.ClearSlot(slot);
        LastMessage = "버렸습니다.";
        PublishInventory(playerId);
        PublishEquipmentSlot(playerId, slot);
        return true;
    }

    /// <summary>
    /// 장착 중인 같은 능력 중 가장 높은 성에 해당하는 레벨 인덱스. 없으면 0.
    /// </summary>
    public int GetEquippedAbilityLevel(int playerId, WeaponAbilityData ability)
    {
        var star = GetHighestEquippedStar(playerId, ability);
        if (star <= 0)
        {
            return 0;
        }

        return ToAbilityLevel(star, ability);
    }

    /// <summary>
    /// 점 발사 프리팹이면 true. 칸마다 따로 둔다.
    /// </summary>
    public static bool IsSlotOriginAbility(WeaponAbilityData ability)
    {
        return ability != null
            && ability.Prefab != null
            && ability.Prefab.GetComponent<ISlotOriginAbility>() != null;
    }

    /// <summary>
    /// 장착된 포켓몬의 능력을 모은다. 점 발사는 칸마다, 그 외는 타입에서 가장 높은 성이다.
    /// </summary>
    public void CollectEquippedAbilityLevels(int playerId, List<EquippedAbilityLevel> results)
    {
        results.Clear();
        var run = GetRun(playerId);
        if (run == null)
        {
            return;
        }

        for (var i = 0; i < run.Equipment.Stacks.Count; i++)
        {
            var stack = run.Equipment.Stacks[i];
            if (stack.Empty || stack.Item == null)
            {
                continue;
            }

            var visual = GetVisual(stack.Item.uid);
            var ability = visual != null ? visual.WeaponAbility : null;
            if (ability == null)
            {
                continue;
            }

            var level = ToAbilityLevel(stack.Item.upgradeLevel, ability);
            if (IsSlotOriginAbility(ability))
            {
                results.Add(new EquippedAbilityLevel(ability, level, i));
                continue;
            }

            var found = false;
            for (var u = 0; u < results.Count; u++)
            {
                if (results[u].Slot != EquippedAbilityLevel.UNSLOTTED
                    || results[u].Ability.WeaponAbilityType != ability.WeaponAbilityType)
                {
                    continue;
                }

                found = true;
                if (level > results[u].LevelIndex)
                {
                    results[u] = new EquippedAbilityLevel(ability, level, EquippedAbilityLevel.UNSLOTTED);
                }

                break;
            }

            if (!found)
            {
                results.Add(new EquippedAbilityLevel(ability, level, EquippedAbilityLevel.UNSLOTTED));
            }
        }
    }

    /// <summary>
    /// 성을 무기 능력 레벨 인덱스로 바꾼다. 1성은 0번이다.
    /// </summary>
    public static int ToAbilityLevel(int star, WeaponAbilityData ability)
    {
        var index = star - Item.STAR_MIN;
        if (index < 0)
        {
            index = 0;
        }

        if (ability == null || ability.LevelsCount <= 0)
        {
            return 0;
        }

        var last = ability.LevelsCount - 1;
        return index > last ? last : index;
    }

    private void EnsureWindow()
    {
        if (_window != null)
        {
            return;
        }

        if (_inventoryWindow == null)
        {
            Debug.LogError("인벤토리 창 프리팹이 없습니다.");
            return;
        }

        _window = Instantiate(_inventoryWindow, transform);
    }

    private RunInventory CreateRun()
    {
        var bag = new InventoryHolder
        {
            Name = "Inventory",
            Type = InventoryHolder.HolderType.PlayerInventory,
            InventorySize = INVENTORY_SIZE
        };
        bag.EnsureSize();
        var equipment = new InventoryHolder
        {
            Name = "Equipment",
            Type = InventoryHolder.HolderType.PlayerEquipment,
            InventorySize = WeaponSlots.MAX_COUNT
        };
        equipment.EnsureSize();
        var offers = new ShopOffer[SHOP_OFFER_COUNT];
        for (var i = 0; i < offers.Length; i++)
        {
            offers[i] = new ShopOffer();
        }

        return new RunInventory
        {
            Bag = bag,
            Equipment = equipment,
            Offers = offers
        };
    }

    private void RollOffers(RunInventory run)
    {
        for (var i = 0; i < run.Offers.Length; i++)
        {
            var offer = run.Offers[i];
            if (offer.Locked)
            {
                continue;
            }

            offer.Uid = _shopPool.Count > 0 ? _shopPool[Random.Range(0, _shopPool.Count)] : ShopOffer.SOLD_UID;
        }
    }

    private RunInventory GetRun(int playerId)
    {
        _runs.TryGetValue(playerId, out var run);
        return run;
    }

    private Item CreateItem(int uid, int star)
    {
        var source = TryGetItem(uid);
        if (source == null)
        {
            return null;
        }

        var copy = source.Copy();
        copy.upgradeLevel = star;
        return copy;
    }

    private int FindUid(MonsterVisualData visual)
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
    /// 가방에 들어온 포켓몬을 계정 획득 기록에 남긴다. 계정은 로컬 플레이어 것만 있다.
    /// </summary>
    private void RecordObtained(int playerId, int uid)
    {
        if (Managers.Instance == null
            || !Managers.Instance.TryGetManager<AccountManager>(out var account)
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
            || playerId != playerManager.LocalPlayerId)
        {
            return;
        }

        account.MarkMonsterObtained(GetVisual(uid));
    }

    private MonsterVisualData ResolveStartingVisual()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            return null;
        }

        var runMonster = account.ResolveRunMonster();
        if (runMonster != null)
        {
            return runMonster;
        }

        return account.ResolveStarterVisual();
    }

    private bool TryMoveToSlot(RunInventory run, int bagIndex, int slot)
    {
        if (run == null || bagIndex < 0 || bagIndex >= run.Bag.Stacks.Count)
        {
            return false;
        }

        var stack = run.Bag.Stacks[bagIndex];
        if (stack.Empty || stack.Item == null)
        {
            return false;
        }

        if (slot < 0 || slot >= run.Equipment.Stacks.Count || !run.Equipment.Stacks[slot].Empty)
        {
            return false;
        }

        var item = stack.Item.Copy();
        stack.AddNumber(-1);
        if (!run.Equipment.TrySetSlot(slot, item))
        {
            run.Bag.AddItem(item, 1);
            return false;
        }

        return true;
    }

    private bool TryTakeBag(RunInventory run, int bagIndex, out Item item, out int number)
    {
        item = null;
        number = 0;
        if (run == null || bagIndex < 0 || bagIndex >= run.Bag.Stacks.Count || run.Bag.Stacks[bagIndex].Empty)
        {
            return false;
        }

        var stack = run.Bag.Stacks[bagIndex];
        item = stack.Item.Copy();
        number = stack.Number;
        stack.Delete();
        return true;
    }

    private bool TryTakeEquipped(RunInventory run, int slot, out Item item)
    {
        item = null;
        if (run == null || slot < 0 || slot >= run.Equipment.Stacks.Count || run.Equipment.Stacks[slot].Empty)
        {
            LastMessage = "장착된 포켓몬이 없습니다.";
            return false;
        }

        if (run.Equipment.CountFilled() <= MIN_EQUIPPED)
        {
            LastMessage = "포켓몬은 한 마리 이상 장착해야 합니다.";
            return false;
        }

        item = run.Equipment.Stacks[slot].Item.Copy();
        return item != null;
    }

    private void Refund(int playerId, int amount)
    {
        if (amount <= 0 || Managers.Instance == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return;
        }

        currencies.Deposit(playerId, CurrenciesManager.MONSTER_BALL_ID, amount);
    }

    private int GetHighestEquippedStar(int playerId, WeaponAbilityData ability)
    {
        var run = GetRun(playerId);
        if (run == null || ability == null)
        {
            return 0;
        }

        var star = 0;
        for (var i = 0; i < run.Equipment.Stacks.Count; i++)
        {
            var stack = run.Equipment.Stacks[i];
            if (stack.Empty || stack.Item == null || stack.Item.upgradeLevel <= star)
            {
                continue;
            }

            var visual = GetVisual(stack.Item.uid);
            if (visual == null || visual.WeaponAbility == null)
            {
                continue;
            }

            if (visual.WeaponAbility.WeaponAbilityType == ability.WeaponAbilityType)
            {
                star = stack.Item.upgradeLevel;
            }
        }

        return star;
    }

    private void FinishBag(int playerId)
    {
        var run = GetRun(playerId);
        if (run != null)
        {
            run.Bag.Sort();
        }

        PublishInventory(playerId);
    }

    private void PublishInventory(int playerId)
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Publish(new InventoryChanged(playerId));
        }
    }

    private void PublishEquipmentSlot(int playerId, int slot)
    {
        var run = GetRun(playerId);
        var uid = EMPTY_UID;
        var star = 0;
        if (run != null && slot >= 0 && slot < run.Equipment.Stacks.Count && !run.Equipment.Stacks[slot].Empty)
        {
            uid = run.Equipment.Stacks[slot].Item.uid;
            star = run.Equipment.Stacks[slot].Item.upgradeLevel;
        }

        if (Managers.Instance != null && Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            eventManager.Publish(new EquipmentChanged(playerId, slot, uid, star));
        }
    }

    private void PublishAllEquipment(int playerId)
    {
        var count = WeaponSlots.MAX_COUNT;
        var run = GetRun(playerId);
        if (run != null)
        {
            count = run.Equipment.Stacks.Count;
        }

        for (var i = 0; i < count; i++)
        {
            PublishEquipmentSlot(playerId, i);
        }
    }

    private sealed class RunInventory
    {
        public InventoryHolder Bag;
        public InventoryHolder Equipment;
        public ShopOffer[] Offers;
    }
}

/// <summary>
/// 상점 한 칸. 판 동안 플레이어마다 유지된다.
/// </summary>
public class ShopOffer
{
    public const int SOLD_UID = -1;

    /// <summary>
    /// 파는 종의 uid. 팔렸으면 SOLD_UID.
    /// </summary>
    public int Uid = SOLD_UID;

    /// <summary>
    /// true면 새로고침해도 바뀌지 않는다.
    /// </summary>
    public bool Locked;

    public bool Sold => Uid < 0;
}

/// <summary>
/// 진화 계통의 단계 칸. 일반 진화는 최대 3단이고 메가진화와 거다이맥스는 따로 둔다.
/// </summary>
public enum EvolutionStage
{
    Basic = 0,
    Stage1 = 1,
    Stage2 = 2,
    Mega = 3,
    VMax = 4
}

/// <summary>
/// 한 진화 계통의 단계별 uid와 고른 종의 단계. 없는 칸은 -1이다.
/// </summary>
public struct EvolutionLine
{
    public const int STAGE_COUNT = 5;
    public const int NONE = -1;

    private readonly int[] _uids;

    /// <summary>
    /// 고른 종의 uid.
    /// </summary>
    public int SelectedUid { get; }

    public EvolutionLine(int selectedUid)
    {
        SelectedUid = selectedUid;
        _uids = new int[STAGE_COUNT];
        for (var i = 0; i < _uids.Length; i++)
        {
            _uids[i] = NONE;
        }
    }

    /// <summary>
    /// 단계 칸의 uid. 없으면 -1.
    /// </summary>
    public int Get(EvolutionStage stage)
    {
        var index = (int)stage;
        return _uids != null && index >= 0 && index < _uids.Length ? _uids[index] : NONE;
    }

    public void Set(EvolutionStage stage, int uid)
    {
        var index = (int)stage;
        if (_uids != null && index >= 0 && index < _uids.Length)
        {
            _uids[index] = uid < 0 ? NONE : uid;
        }
    }

    /// <summary>
    /// 고른 종이 들어간 칸이면 true.
    /// </summary>
    public bool IsSelected(EvolutionStage stage)
    {
        return SelectedUid >= 0 && Get(stage) == SelectedUid;
    }
}

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
