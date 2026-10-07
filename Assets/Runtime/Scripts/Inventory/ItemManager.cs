using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 판 안의 포켓몬 가방과 장착을 플레이어 식별자마다 들고 있다.
/// 종 정의는 ItemCatalog, 칸 조작은 RunInventory, 상점은 ItemShop, 합성은 ItemSynthesis,
/// 능력 레벨은 EquippedAbilityResolver가 맡고, 여기서는 재화·계정 호출과 이벤트 발행을 한다.
/// </summary>
public class ItemManager : BaseManager
{
    public const int INVENTORY_SIZE = 20;
    /// <summary>
    /// 가방 한 칸에 들어가는 마릿수. 한 마리당 한 칸을 차지한다.
    /// </summary>
    public const int MAX_STACK = 1;
    public const int SYNTHESIS_COUNT = 3;

    /// <summary>
    /// 판매 때 돌려주는 몬스터볼 비율. 구매가에 곱하고 내린다. 최소 1개.
    /// </summary>
    public const float SELL_PRICE_RATE = 0.5f;
    public const int MIN_SELL_PRICE = 1;
    public const int MIN_EQUIPPED = 1;
    public const int SHOP_OFFER_COUNT = 4;
    public const int SHOP_REFRESH_PRICE = 1;

    [SerializeField] private MonsterDatabase _monsters;
    [SerializeField] private InventoryUi _inventoryWindow;

    private InventoryUi _window;
    private readonly ItemCatalog _catalog = new ItemCatalog();
    private readonly Dictionary<int, RunInventory> _runs = new Dictionary<int, RunInventory>();
    private readonly List<int> _usedSlots = new List<int>(SYNTHESIS_COUNT);

    public string LastMessage { get; private set; } = string.Empty;

    public IReadOnlyList<Item> Items => _catalog.Items;

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
        _catalog.ReInit(_monsters != null ? _monsters.Monsters : System.Array.Empty<MonsterVisualData>());
    }

    /// <summary>
    /// 일반 진화, 메가진화, 거다이맥스 중 하나로 이 종이 되는 이전 종. 없으면 -1.
    /// </summary>
    public int FindPreEvolutionUid(int uid)
    {
        return _catalog.FindPreEvolutionUid(uid);
    }

    /// <summary>
    /// 이 종이 속한 진화 계통을 단계 칸으로 나눈다. 정의가 없으면 모든 칸이 -1이다.
    /// </summary>
    public EvolutionLine GetEvolutionLine(int uid)
    {
        return _catalog.GetEvolutionLine(uid);
    }

    /// <summary>
    /// 정의가 있으면 그대로 돌려준다.
    /// </summary>
    public Item TryGetItem(int uid)
    {
        return _catalog.TryGetItem(uid);
    }

    /// <summary>
    /// 종 목록의 무기 능력이면 true.
    /// </summary>
    public bool IsCatalogWeaponAbility(WeaponAbilityType abilityType)
    {
        return _catalog.IsCatalogWeaponAbility(abilityType);
    }

    /// <summary>
    /// uid에 해당하는 포켓몬 그림. 없으면 null.
    /// </summary>
    public MonsterVisualData GetVisual(int uid)
    {
        return _catalog.GetVisual(uid);
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
        if (_catalog.Items.Count == 0)
        {
            ReInit();
        }

        var run = RunInventory.Create();
        _runs[playerId] = run;
        ItemShop.RollOffers(run, _catalog.ShopPool);
        var visual = ResolveStartingVisual();
        var uid = _catalog.FindUid(visual);
        if (uid < 0)
        {
            LastMessage = "시작 포켓몬을 찾지 못했습니다.";
            Debug.LogWarning(LastMessage);
            FinishBag(playerId);
            PublishAllEquipment(playerId);
            return;
        }

        var created = _catalog.CreateItem(uid, Item.STAR_MIN);
        run.Bag.AddItem(created, 1);
        RecordObtained(playerId, uid);
        var index = run.Bag.FindIndex(uid, Item.STAR_MIN);
        if (!run.TryMoveToSlot(index, (int)WeaponSlot.Hour3))
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
    /// 상점 칸에 진열된 포켓몬을 칸의 성과 가격으로 만든다. 진열이 없으면 null.
    /// </summary>
    public Item CreateOfferItem(ShopOffer offer)
    {
        return offer != null && offer.Uid >= 0 ? _catalog.CreateItem(offer.Uid, offer.Star) : null;
    }

    /// <summary>
    /// 한 마리를 팔 때 돌려받는 몬스터볼.
    /// </summary>
    public static int GetSellPrice(Item item)
    {
        return ItemShop.GetSellPrice(item);
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

        ItemShop.RollOffers(run, _catalog.ShopPool);
        LastMessage = "상점을 새로 고쳤습니다.";
        PublishInventory(playerId);
        return true;
    }

    /// <summary>
    /// 상점 칸 잠금을 바꾼다. 잠긴 칸은 새로고침에서 빠진다.
    /// </summary>
    public void ToggleShopLock(int playerId, int offerIndex)
    {
        if (ItemShop.TryToggleLock(GetRun(playerId), offerIndex))
        {
            PublishInventory(playerId);
        }
    }

    /// <summary>
    /// 상점 칸의 포켓몬 한 마리를 몬스터볼로 사서 가방에 넣는다. 산 칸은 Purchased로 표시한다.
    /// </summary>
    public bool TryPurchase(int playerId, int offerIndex)
    {
        var run = GetRun(playerId);
        if (!ItemShop.TryPrepareOffer(run, _catalog, offerIndex, out var created, out var message))
        {
            LastMessage = message;
            return false;
        }

        if (!Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies)
            || !currencies.TryWithdraw(playerId, CurrenciesManager.MONSTER_BALL_ID, created.price, false))
        {
            LastMessage = "몬스터볼이 부족합니다.";
            return false;
        }

        ItemShop.CompletePurchase(run, offerIndex, created);
        RecordObtained(playerId, created.uid);
        LastMessage = created.name + "을 가방에 넣었습니다.";
        FinishBag(playerId);
        return true;
    }

    /// <summary>
    /// 가방과 장착 칸을 합쳐 같은 종, 같은 성 세 마리를 합성한다.
    /// </summary>
    public bool TrySynthesize(int playerId, int uid, int upgradeLevel)
    {
        return TrySynthesize(playerId, uid, upgradeLevel, -1, -1);
    }

    /// <summary>
    /// 가방과 장착 칸을 합쳐 같은 종, 같은 성 세 마리를 합성한다.
    /// 재료는 놓은 장착 칸, 끈 장착 칸, 가방, 나머지 장착 칸 순서로 쓴다.
    /// 장착 칸이 재료로 쓰이면 결과는 처음 쓰인 장착 칸에 남고, 아니면 가방에 들어간다.
    /// </summary>
    public bool TrySynthesize(int playerId, int uid, int upgradeLevel, int targetSlot, int sourceSlot)
    {
        var success = ItemSynthesis.TrySynthesize(GetRun(playerId), _catalog, uid, upgradeLevel, targetSlot, sourceSlot,
            _usedSlots, out var result, out var message);
        LastMessage = message;
        if (!success)
        {
            return false;
        }

        RecordObtained(playerId, result.uid);
        FinishBag(playerId);
        for (var i = 0; i < _usedSlots.Count; i++)
        {
            PublishEquipmentSlot(playerId, _usedSlots[i]);
        }

        return true;
    }

    /// <summary>
    /// 가방 칸 한 마리를 빈 시계 칸에 장착한다.
    /// </summary>
    public bool TryEquip(int playerId, int bagIndex)
    {
        var run = GetRun(playerId);
        if (run == null || !run.HasBagItem(bagIndex))
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

        if (!run.TryMoveToSlot(bagIndex, slot))
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
    /// 가방 칸 한 마리를 정한 시계 칸에 장착한다. 칸이 차 있으면 서로 바꾼다.
    /// </summary>
    public bool TryEquipToSlot(int playerId, int bagIndex, int slot)
    {
        var run = GetRun(playerId);
        if (run == null || !run.HasBagItem(bagIndex))
        {
            LastMessage = "장착할 포켓몬이 없습니다.";
            return false;
        }

        if (!run.IsValidSlot(slot))
        {
            LastMessage = "장착하지 못했습니다.";
            return false;
        }

        if (run.HasEquipped(slot))
        {
            return TrySwapBagWithEquipped(playerId, bagIndex, slot);
        }

        if (!run.TryMoveToSlot(bagIndex, slot))
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
    /// 가방 칸과 시계 칸의 포켓몬을 맞바꾼다.
    /// </summary>
    public bool TrySwapBagWithEquipped(int playerId, int bagIndex, int slot)
    {
        var run = GetRun(playerId);
        if (run == null || !run.HasBagItem(bagIndex) || !run.HasEquipped(slot))
        {
            LastMessage = "교체할 포켓몬이 없습니다.";
            return false;
        }

        var bagItem = run.Bag.Stacks[bagIndex].Item.Copy();
        var equippedItem = run.Equipment.Stacks[slot].Item.Copy();
        run.Bag.Stacks[bagIndex].SetItem(equippedItem, 1);
        run.Equipment.Stacks[slot].SetItem(bagItem, 1);
        LastMessage = "교체했습니다.";
        FinishBag(playerId);
        PublishEquipmentSlot(playerId, slot);
        return true;
    }

    /// <summary>
    /// 시계 칸끼리 자리를 바꾼다. 한쪽이 비어 있으면 옮기기만 한다.
    /// </summary>
    public bool TrySwapEquipped(int playerId, int from, int to)
    {
        var run = GetRun(playerId);
        if (run == null || from == to || !run.HasEquipped(from) || !run.IsValidSlot(to))
        {
            return false;
        }

        var fromStack = run.Equipment.Stacks[from];
        var toStack = run.Equipment.Stacks[to];
        var moving = fromStack.Item.Copy();
        if (toStack.Empty)
        {
            fromStack.Delete();
        }
        else
        {
            fromStack.SetItem(toStack.Item, 1);
        }

        toStack.SetItem(moving, 1);
        LastMessage = "자리를 바꿨습니다.";
        PublishInventory(playerId);
        PublishEquipmentSlot(playerId, from);
        PublishEquipmentSlot(playerId, to);
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
        if (run == null || !run.TryTakeBag(bagIndex, out var item, out var number))
        {
            LastMessage = "판매할 포켓몬이 없습니다.";
            return false;
        }

        Refund(playerId, GetSellPrice(item) * number);
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
        if (run == null || !run.TryTakeBag(bagIndex, out _, out _))
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
        Refund(playerId, GetSellPrice(item));
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
        return EquippedAbilityResolver.GetLevel(GetRun(playerId)?.Equipment, _catalog, ability);
    }

    /// <summary>
    /// 점 발사 프리팹이면 true. 칸마다 따로 둔다.
    /// </summary>
    public static bool IsSlotOriginAbility(WeaponAbilityData ability)
    {
        return EquippedAbilityResolver.IsSlotOriginAbility(ability);
    }

    /// <summary>
    /// 장착된 포켓몬의 능력을 모은다. 점 발사는 칸마다, 그 외는 타입에서 가장 높은 성이다.
    /// </summary>
    public void CollectEquippedAbilityLevels(int playerId, List<EquippedAbilityLevel> results)
    {
        EquippedAbilityResolver.Collect(GetRun(playerId)?.Equipment, _catalog, results);
    }

    /// <summary>
    /// 성을 무기 능력 레벨 인덱스로 바꾼다. 1성은 0번이다.
    /// </summary>
    public static int ToAbilityLevel(int star, WeaponAbilityData ability)
    {
        return EquippedAbilityResolver.ToAbilityLevel(star, ability);
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

    private RunInventory GetRun(int playerId)
    {
        _runs.TryGetValue(playerId, out var run);
        return run;
    }

    private bool TryTakeEquipped(RunInventory run, int slot, out Item item)
    {
        item = null;
        var message = "장착된 포켓몬이 없습니다.";
        if (run == null || !run.TryTakeEquipped(slot, out item, out message))
        {
            LastMessage = message;
            return false;
        }

        return true;
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

        account.MarkMonsterObtained(_catalog.GetVisual(uid));
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

    private void Refund(int playerId, int amount)
    {
        if (amount <= 0 || Managers.Instance == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return;
        }

        currencies.Deposit(playerId, CurrenciesManager.MONSTER_BALL_ID, amount);
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
        var uid = ItemCatalog.EMPTY_UID;
        var star = 0;
        if (run != null && run.HasEquipped(slot))
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
}
