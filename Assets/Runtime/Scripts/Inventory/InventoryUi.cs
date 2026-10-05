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
        Equipment
    }

    [SerializeField] private Canvas _canvas;
    [SerializeField] private EquipmentUi _equipmentUi;
    [SerializeField] private ShopUi _shopUi;
    [SerializeField] private InventoryItem _slotPrefab;
    [SerializeField] private InventoryItem _balanceSlot;
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
            _shopUi.Bind(Refresh);
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

        if (!Managers.Instance.TryGetManager<InputManager>(out var inputManager) || !inputManager.ConsumeInventoryPressed())
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
        RefreshBalls(playerId);
        if (_status != null)
        {
            _status.text = itemManager.LastMessage;
        }

        RebuildBag(itemManager, playerId);
        if (_shopUi != null)
        {
            _shopUi.Refresh(playerId);
        }
        if (_equipmentUi != null)
        {
            var selectedSlot = _selection == SelectionKind.Equipment ? _selectedIndex : -1;
            _equipmentUi.Refresh(playerId, selectedSlot, SelectEquipment);
        }
    }

    private void RefreshBalls(int playerId)
    {
        if (_balanceSlot == null)
        {
            return;
        }

        Sprite icon = null;
        var amount = 0;
        if (Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            icon = currencies.GetIcon(CurrenciesManager.MONSTER_BALL_ID);
            var save = currencies.GetCurrency(playerId, CurrenciesManager.MONSTER_BALL_ID, false);
            amount = save != null ? save.Amount : 0;
        }

        _balanceSlot.ShowBalance(icon, amount);
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
                slot.HidePrice();
                slot.SetSelected(false);
                continue;
            }

            var visual = itemManager.GetVisual(stack.Item.uid);
            var portrait = visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
            slot.ShowPokemon(portrait, stack.Item.name, stack.Item.upgradeLevel, stack.Number, true, selected);
            slot.HidePrice();
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

    private void HandleCurrencyChanged(CurrencyAmountChanged changed)
    {
        if (!_open || !TryGetContext(out _, out var playerId) || changed.PlayerId != playerId)
        {
            return;
        }

        RefreshBalls(playerId);
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
        eventManager.Subscribe<CurrencyAmountChanged>(HandleCurrencyChanged);
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
        eventManager.Unsubscribe<CurrencyAmountChanged>(HandleCurrencyChanged);
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
