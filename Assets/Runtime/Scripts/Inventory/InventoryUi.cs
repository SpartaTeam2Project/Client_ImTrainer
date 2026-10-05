using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 판 안에서 가방과 장착을 다루는 창. 구매 목록은 ShopUi가 그린다.
/// </summary>
public class InventoryUi : MonoBehaviour
{
    private enum SelectionKind
    {
        None,
        Bag,
        Equipment,
        Shop
    }

    [SerializeField] private Canvas _canvas;
    [SerializeField] private EquipmentUi _equipmentUi;
    [SerializeField] private ShopUi _shopUi;
    [SerializeField] private HoverInformation _information;
    [SerializeField] private InventoryItem _slotPrefab;
    [SerializeField] private ResourceBar _resourceBar;
    [SerializeField] private TextMeshProUGUI _status;
    [SerializeField] private RectTransform _bagContent;

    [Header("Actions")]
    [SerializeField] private Button _synthesizeButton;
    [SerializeField] private Button _equipButton;
    [SerializeField] private Button _unequipButton;
    [SerializeField] private Button _sellButton;
    [SerializeField] private Button _discardButton;
    [SerializeField] private Button _closeButton;

    private readonly List<InventoryItem> _bagSlots = new List<InventoryItem>();
    private SelectionKind _selection = SelectionKind.None;
    private int _selectedIndex = -1;
    private int _selectedUid = -1;
    private int _selectedStar;
    private bool _open;
    private bool _holdTime;
    private bool _subscribed;

    private void Awake()
    {
        AddAction(_synthesizeButton, SynthesizeSelected);
        AddAction(_equipButton, EquipSelected);
        AddAction(_unequipButton, UnequipSelected);
        AddAction(_sellButton, SellSelected);
        AddAction(_discardButton, DiscardSelected);
        AddAction(_closeButton, Close);
        if (_shopUi != null)
        {
            _shopUi.Bind(Refresh, SelectShop);
        }

        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (_holdTime && Managers.Instance != null && Managers.Instance.CurrentState == GameState.Playing)
        {
            Time.timeScale = 1f;
        }

        _holdTime = false;
        _open = false;
    }

    private void Update()
    {
        if (!CanToggle())
        {
            return;
        }

        if (!Managers.Instance.TryGetManager<InputManager>(out var inputManager))
        {
            return;
        }

        if (_open && _shopUi != null && inputManager.ConsumeShopRefreshPressed())
        {
            _shopUi.RefreshOffers();
        }

        if (!inputManager.ConsumeInventoryPressed())
        {
            return;
        }

        if (_open)
        {
            Close();
            return;
        }

        Open();
    }

    private void Open()
    {
        if (_canvas == null)
        {
            Debug.LogError("인벤토리 창 캔버스가 없습니다.");
            return;
        }

        _open = true;
        _canvas.gameObject.SetActive(true);
        _holdTime = Managers.Instance.CurrentState == GameState.Playing;
        if (_holdTime)
        {
            Time.timeScale = 0f;
        }

        Refresh();
    }

    private void Close()
    {
        _open = false;
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(false);
        }

        if (_holdTime && Managers.Instance != null && Managers.Instance.CurrentState == GameState.Playing)
        {
            Time.timeScale = 1f;
        }

        _holdTime = false;
    }

    private bool CanToggle()
    {
        if (Managers.Instance == null)
        {
            return false;
        }

        var state = Managers.Instance.CurrentState;
        if (state != GameState.Playing && state != GameState.Paused)
        {
            if (_open)
            {
                Close();
            }

            return false;
        }

        if (Managers.Instance.SettingsWindow != null && Managers.Instance.SettingsWindow.IsOpen)
        {
            return false;
        }

        if (!Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
            || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return false;
        }

        return itemManager.HasRun(playerManager.LocalPlayerId);
    }

    private void Refresh()
    {
        if (!Managers.Instance.TryGetManager<PlayerManager>(out var playerManager)
            || !Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return;
        }

        var playerId = playerManager.LocalPlayerId;
        if (_resourceBar != null)
        {
            _resourceBar.Refresh(playerId);
        }

        if (_status != null)
        {
            _status.text = itemManager.LastMessage;
        }

        RebuildBag(itemManager, playerId);
        ResolveShopSelection(itemManager, playerId);
        if (_shopUi != null)
        {
            _shopUi.Refresh(playerId, _selection == SelectionKind.Shop ? _selectedIndex : -1);
        }
        if (_equipmentUi != null)
        {
            var selectedSlot = _selection == SelectionKind.Equipment ? _selectedIndex : -1;
            _equipmentUi.Refresh(playerId, selectedSlot, SelectEquipment);
        }

        RefreshInformation(itemManager, playerId);
    }

    private void RefreshInformation(ItemManager itemManager, int playerId)
    {
        if (_information == null)
        {
            return;
        }

        if (_selection == SelectionKind.Shop)
        {
            var offer = GetShopOffer(itemManager, playerId, _selectedIndex);
            var item = offer != null && !offer.Sold ? itemManager.TryGetItem(offer.Uid) : null;
            if (item == null)
            {
                _information.Clear();
                return;
            }

            _information.Show(item);
            return;
        }

        var holder = _selection == SelectionKind.Bag ? itemManager.GetInventory(playerId)
            : _selection == SelectionKind.Equipment ? itemManager.GetEquipment(playerId)
            : null;
        if (holder == null || _selectedIndex < 0 || _selectedIndex >= holder.Stacks.Count || holder.Stacks[_selectedIndex].Empty)
        {
            _information.Clear();
            return;
        }

        _information.Show(holder.Stacks[_selectedIndex].Item);
    }

    private void RebuildBag(ItemManager itemManager, int playerId)
    {
        var bag = itemManager.GetInventory(playerId);
        if (bag == null)
        {
            return;
        }

        EnsureBagSlots(bag);
        ResolveBagSelection(bag);
        for (var i = 0; i < _bagSlots.Count && i < bag.Stacks.Count; i++)
        {
            var slot = _bagSlots[i];
            var stack = bag.Stacks[i];
            var selected = _selection == SelectionKind.Bag && _selectedIndex == i;
            if (stack.Empty || stack.Item == null)
            {
                slot.ShowEmpty();
                slot.SetSelected(false);
                continue;
            }

            var visual = itemManager.GetVisual(stack.Item.uid);
            var portrait = visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
            slot.ShowPokemon(portrait, stack.Item.name, stack.Item.upgradeLevel, stack.Number, true, selected);
        }
    }

    private void EnsureBagSlots(InventoryHolder bag)
    {
        if (_slotPrefab == null || _bagContent == null || bag == null || _bagSlots.Count == bag.Stacks.Count)
        {
            return;
        }

        ClearSlots(_bagSlots);
        for (var i = 0; i < bag.Stacks.Count; i++)
        {
            var index = i;
            var slot = Instantiate(_slotPrefab, _bagContent);
            slot.Button.onClick.AddListener(() => SelectBag(index));
            _bagSlots.Add(slot);
        }
    }

    private void ResolveBagSelection(InventoryHolder bag)
    {
        if (_selection != SelectionKind.Bag || _selectedUid < 0)
        {
            return;
        }

        var index = bag.FindIndex(_selectedUid, _selectedStar);
        if (index < 0)
        {
            _selection = SelectionKind.None;
            _selectedIndex = -1;
            _selectedUid = -1;
            return;
        }

        _selectedIndex = index;
    }

    private void SelectBag(int index)
    {
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        var bag = itemManager.GetInventory(playerId);
        if (bag == null || index < 0 || index >= bag.Stacks.Count || bag.Stacks[index].Empty || bag.Stacks[index].Item == null)
        {
            _selection = SelectionKind.None;
            _selectedIndex = -1;
            _selectedUid = -1;
            Refresh();
            return;
        }

        _selection = SelectionKind.Bag;
        _selectedIndex = index;
        _selectedUid = bag.Stacks[index].Item.uid;
        _selectedStar = bag.Stacks[index].Item.upgradeLevel;
        Refresh();
    }

    private void SelectEquipment(int slot)
    {
        _selection = SelectionKind.Equipment;
        _selectedIndex = slot;
        Refresh();
    }

    /// <summary>
    /// 상점 칸을 고른다. 정보 창만 보여 주고 장착, 판매 같은 동작은 하지 않는다.
    /// </summary>
    private void SelectShop(int index)
    {
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        var offer = GetShopOffer(itemManager, playerId, index);
        if (offer == null || offer.Sold)
        {
            ClearSelection();
            Refresh();
            return;
        }

        _selection = SelectionKind.Shop;
        _selectedIndex = index;
        _selectedUid = offer.Uid;
        Refresh();
    }

    /// <summary>
    /// 고른 상점 칸이 팔렸거나 새로고침으로 다른 포켓몬이 되면 선택을 푼다.
    /// </summary>
    private void ResolveShopSelection(ItemManager itemManager, int playerId)
    {
        if (_selection != SelectionKind.Shop)
        {
            return;
        }

        var offer = GetShopOffer(itemManager, playerId, _selectedIndex);
        if (offer == null || offer.Sold || offer.Uid != _selectedUid)
        {
            ClearSelection();
        }
    }

    private static ShopOffer GetShopOffer(ItemManager itemManager, int playerId, int index)
    {
        var offers = itemManager.GetShopOffers(playerId);
        if (offers == null || index < 0 || index >= offers.Count)
        {
            return null;
        }

        return offers[index];
    }

    private void ClearSelection()
    {
        _selection = SelectionKind.None;
        _selectedIndex = -1;
        _selectedUid = -1;
    }

    private void SynthesizeSelected()
    {
        if (!TryGetSelectedBag(out var itemManager, out var playerId, out var stack))
        {
            return;
        }

        itemManager.TrySynthesize(playerId, stack.Item.uid, stack.Item.upgradeLevel);
        Refresh();
    }

    private void EquipSelected()
    {
        if (_selection != SelectionKind.Bag || !TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        itemManager.TryEquip(playerId, _selectedIndex);
        Refresh();
    }

    private void UnequipSelected()
    {
        if (_selection != SelectionKind.Equipment || !TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        itemManager.TryUnequip(playerId, _selectedIndex);
        Refresh();
    }

    private void SellSelected()
    {
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        if (_selection == SelectionKind.Bag)
        {
            itemManager.TrySell(playerId, _selectedIndex);
        }
        else if (_selection == SelectionKind.Equipment)
        {
            itemManager.TrySellEquipped(playerId, _selectedIndex);
        }

        Refresh();
    }

    private void DiscardSelected()
    {
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        if (_selection == SelectionKind.Bag)
        {
            itemManager.TryDiscard(playerId, _selectedIndex);
        }
        else if (_selection == SelectionKind.Equipment)
        {
            itemManager.TryDiscardEquipped(playerId, _selectedIndex);
        }

        Refresh();
    }

    private bool TryGetSelectedBag(out ItemManager itemManager, out int playerId, out InventoryStack stack)
    {
        stack = null;
        if (_selection != SelectionKind.Bag || !TryGetContext(out itemManager, out playerId))
        {
            itemManager = null;
            playerId = 0;
            return false;
        }

        var bag = itemManager.GetInventory(playerId);
        if (bag == null || _selectedIndex < 0 || _selectedIndex >= bag.Stacks.Count || bag.Stacks[_selectedIndex].Empty)
        {
            return false;
        }

        stack = bag.Stacks[_selectedIndex];
        return stack.Item != null;
    }

    private bool TryGetContext(out ItemManager itemManager, out int playerId)
    {
        itemManager = null;
        playerId = 0;
        if (Managers.Instance == null
            || !Managers.Instance.TryGetManager<ItemManager>(out itemManager)
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        playerId = playerManager.LocalPlayerId;
        return true;
    }

    private void HandleInventoryChanged(InventoryChanged changed)
    {
        if (!_open || !TryGetContext(out _, out var playerId) || changed.PlayerId != playerId)
        {
            return;
        }

        Refresh();
    }

    private void HandleStateChanged(GameStateChanged changed)
    {
        if (changed.Next == GameState.Playing)
        {
            if (_open)
            {
                _holdTime = true;
                Time.timeScale = 0f;
            }

            return;
        }

        if (changed.Next == GameState.Paused)
        {
            return;
        }

        if (_open)
        {
            _holdTime = false;
            Close();
        }
    }

    private void Subscribe()
    {
        if (_subscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            return;
        }

        eventManager.Subscribe<InventoryChanged>(HandleInventoryChanged);
        eventManager.Subscribe<GameStateChanged>(HandleStateChanged);
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            _subscribed = false;
            return;
        }

        eventManager.Unsubscribe<InventoryChanged>(HandleInventoryChanged);
        eventManager.Unsubscribe<GameStateChanged>(HandleStateChanged);
        _subscribed = false;
    }

    private static void AddAction(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            Debug.LogError("인벤토리 창 버튼이 없습니다.");
            return;
        }

        button.onClick.AddListener(action);
    }

    private static void ClearSlots(List<InventoryItem> slots)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                Destroy(slots[i].gameObject);
            }
        }

        slots.Clear();
    }
}
