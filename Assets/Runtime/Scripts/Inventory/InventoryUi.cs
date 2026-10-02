using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 판 안에서 포켓몬을 사고, 합성하고, 장착하는 창.
/// </summary>
public class InventoryUi : MonoBehaviour
{
    private const int CANVAS_SORT_ORDER = 600;
    private const int GRID_COLUMNS = 3;
    private static readonly Vector2 CELL_SIZE = new Vector2(108f, 132f);

    private enum SelectionKind
    {
        None,
        Bag,
        Equipment
    }

    private Canvas _canvas;
    private EquipmentUi _equipmentUi;
    private InventoryItem _slotPrefab;
    private InventoryItem _balanceSlot;
    private TextMeshProUGUI _status;
    private RectTransform _catalogContent;
    private RectTransform _bagContent;
    private readonly List<InventoryItem> _catalogSlots = new List<InventoryItem>();
    private readonly List<InventoryItem> _bagSlots = new List<InventoryItem>();
    private SelectionKind _selection = SelectionKind.None;
    private int _selectedIndex = -1;
    private int _selectedUid = -1;
    private int _selectedStar;
    private bool _open;
    private bool _holdTime;
    private bool _subscribed;

    /// <summary>
    /// 장착 칸 창을 연결한다.
    /// </summary>
    public void Bind(EquipmentUi equipmentUi, InventoryItem slotPrefab)
    {
        _equipmentUi = equipmentUi;
        _slotPrefab = slotPrefab;
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

    /// <summary>
    /// 가로로 꽉 채운 사각형을 만든다.
    /// </summary>
    public static RectTransform CreateRect(string name, Transform parent)
    {
        var rectObject = new GameObject(name);
        rectObject.transform.SetParent(parent, false);
        return rectObject.AddComponent<RectTransform>();
    }

    /// <summary>
    /// 기본 글꼴로 글자를 만든다.
    /// </summary>
    public static TextMeshProUGUI CreateText(string name, RectTransform parent, string value, int fontSize)
    {
        var rect = CreateRect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    /// <summary>
    /// 부모 칸에 맞춘다.
    /// </summary>
    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Open()
    {
        EnsureUi();
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

    private void EnsureUi()
    {
        if (_canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("Inventory Window");
        canvasObject.transform.SetParent(transform, false);
        _canvas = canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = CANVAS_SORT_ORDER;
        canvasObject.AddComponent<GraphicRaycaster>();
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = CreateRect("Panel", canvasObject.transform);
        Stretch(panel);
        var panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        var title = CreateText("Title", panel, "포켓몬", 36);
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -16f);
        title.rectTransform.sizeDelta = new Vector2(400f, 48f);

        if (_slotPrefab != null)
        {
            _balanceSlot = Instantiate(_slotPrefab, panel);
            var balanceRect = _balanceSlot.transform as RectTransform;
            if (balanceRect != null)
            {
                balanceRect.anchorMin = new Vector2(0f, 1f);
                balanceRect.anchorMax = new Vector2(0f, 1f);
                balanceRect.pivot = new Vector2(0f, 1f);
                balanceRect.anchoredPosition = new Vector2(24f, -16f);
                balanceRect.sizeDelta = new Vector2(220f, 44f);
            }
        }
        else
        {
            Debug.LogError("인벤토리 칸 프리팹이 없습니다.");
        }

        _status = CreateText("Status", panel, string.Empty, 20);
        _status.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        _status.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        _status.rectTransform.pivot = new Vector2(0.5f, 0f);
        _status.rectTransform.anchoredPosition = new Vector2(0f, 78f);
        _status.rectTransform.sizeDelta = new Vector2(1200f, 32f);

        _catalogContent = CreateScroll("Catalog", panel, new Vector2(0.02f, 0.18f), new Vector2(0.34f, 0.84f));
        _bagContent = CreateScroll("Bag", panel, new Vector2(0.36f, 0.18f), new Vector2(0.7f, 0.84f));
        if (_equipmentUi != null)
        {
            _equipmentUi.Build(panel, _slotPrefab);
        }

        CreateAction(panel, "합성", 0, SynthesizeSelected);
        CreateAction(panel, "장착", 1, EquipSelected);
        CreateAction(panel, "해제", 2, UnequipSelected);
        CreateAction(panel, "판매", 3, SellSelected);
        CreateAction(panel, "버리기", 4, DiscardSelected);
        CreateAction(panel, "닫기", 5, Close);
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

        RebuildCatalog(itemManager, playerId);
        RebuildBag(itemManager, playerId);
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

    private void RebuildCatalog(ItemManager itemManager, int playerId)
    {
        EnsureCatalogSlots(itemManager, playerId);
        var icon = MonsterBallIcon();
        var slotIndex = 0;
        for (var i = 0; i < itemManager.Items.Count; i++)
        {
            var item = itemManager.Items[i];
            if (item == null || slotIndex >= _catalogSlots.Count)
            {
                continue;
            }

            var visual = itemManager.GetVisual(item.uid);
            var portrait = visual != null ? visual.Portrait : null;
            var slot = _catalogSlots[slotIndex];
            slot.ShowPokemon(portrait, item.name, Item.STAR_MIN, 1, false, false);
            slot.ShowPrice(icon, item.price);
            slotIndex++;
        }
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
            var portrait = visual != null ? visual.Portrait : null;
            slot.ShowPokemon(portrait, stack.Item.name, stack.Item.upgradeLevel, stack.Number, true, selected);
            slot.HidePrice();
        }
    }

    private void EnsureCatalogSlots(ItemManager itemManager, int playerId)
    {
        if (_slotPrefab == null || _catalogContent == null)
        {
            return;
        }

        var count = 0;
        for (var i = 0; i < itemManager.Items.Count; i++)
        {
            if (itemManager.Items[i] != null)
            {
                count++;
            }
        }

        if (_catalogSlots.Count == count)
        {
            return;
        }

        ClearSlots(_catalogSlots);
        for (var i = 0; i < itemManager.Items.Count; i++)
        {
            var item = itemManager.Items[i];
            if (item == null)
            {
                continue;
            }

            var uid = item.uid;
            var slot = Instantiate(_slotPrefab, _catalogContent);
            slot.Button.onClick.AddListener(() => Purchase(playerId, uid));
            _catalogSlots.Add(slot);
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

    private static Sprite MonsterBallIcon()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return null;
        }

        return currencies.GetIcon(CurrenciesManager.MONSTER_BALL_ID);
    }

    private void Purchase(int playerId, int uid)
    {
        if (!Managers.Instance.TryGetManager<ItemManager>(out var itemManager))
        {
            return;
        }

        itemManager.TryPurchase(playerId, uid);
        Refresh();
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

    private RectTransform CreateScroll(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var root = CreateRect(name, parent);
        root.anchorMin = anchorMin;
        root.anchorMax = anchorMax;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        var scroll = root.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        var viewport = CreateRect("Viewport", root);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        var viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0.08f, 0.09f, 0.12f, 0.9f);
        scroll.viewport = viewport;
        var content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        var layout = content.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = CELL_SIZE;
        layout.spacing = new Vector2(8f, 8f);
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = GRID_COLUMNS;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        return content;
    }

    private void CreateAction(RectTransform parent, string label, int index, UnityEngine.Events.UnityAction action)
    {
        var rect = CreateRect(label, parent);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(140f, 42f);
        rect.anchoredPosition = new Vector2(-380f + index * 152f, 18f);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.28f, 0.38f, 1f);
        var button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(action);
        var text = CreateText("Label", rect, label, 22);
        Stretch(text.rectTransform);
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
