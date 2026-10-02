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
    private const float ROW_HEIGHT = 36f;

    private enum SelectionKind
    {
        None,
        Bag,
        Equipment
    }

    private Canvas _canvas;
    private EquipmentUi _equipmentUi;
    private TextMeshProUGUI _status;
    private TextMeshProUGUI _ballText;
    private RectTransform _catalogContent;
    private RectTransform _bagContent;
    private readonly List<Button> _catalogButtons = new List<Button>();
    private readonly List<Button> _bagButtons = new List<Button>();
    private SelectionKind _selection = SelectionKind.None;
    private int _selectedIndex = -1;
    private bool _open;
    private bool _holdTime;
    private bool _subscribed;

    /// <summary>
    /// 장착 칸 창을 연결한다.
    /// </summary>
    public void Bind(EquipmentUi equipmentUi)
    {
        _equipmentUi = equipmentUi;
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

        _ballText = CreateText("Balls", panel, string.Empty, 22);
        _ballText.alignment = TextAlignmentOptions.MidlineLeft;
        _ballText.rectTransform.anchorMin = new Vector2(0f, 1f);
        _ballText.rectTransform.anchorMax = new Vector2(0f, 1f);
        _ballText.rectTransform.pivot = new Vector2(0f, 1f);
        _ballText.rectTransform.anchoredPosition = new Vector2(24f, -20f);
        _ballText.rectTransform.sizeDelta = new Vector2(360f, 36f);

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
            _equipmentUi.Build(panel);
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
        if (_ballText == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return;
        }

        var save = currencies.GetCurrency(playerId, CurrenciesManager.MONSTER_BALL_ID, false);
        var amount = save != null ? save.Amount : 0;
        _ballText.text = "몬스터볼 " + amount;
    }

    private void RebuildCatalog(ItemManager itemManager, int playerId)
    {
        ClearButtons(_catalogButtons);
        for (var i = 0; i < itemManager.Items.Count; i++)
        {
            var item = itemManager.Items[i];
            if (item == null)
            {
                continue;
            }

            var uid = item.uid;
            var button = CreateRow(_catalogContent, item.name + "  " + item.price);
            button.onClick.AddListener(() => Purchase(playerId, uid));
            _catalogButtons.Add(button);
        }
    }

    private void RebuildBag(ItemManager itemManager, int playerId)
    {
        ClearButtons(_bagButtons);
        var bag = itemManager.GetInventory(playerId);
        if (bag == null)
        {
            return;
        }

        for (var i = 0; i < bag.Stacks.Count; i++)
        {
            var stack = bag.Stacks[i];
            if (stack.Empty || stack.Item == null)
            {
                continue;
            }

            var index = i;
            var button = CreateRow(_bagContent, stack.Item.name + " " + stack.Item.upgradeLevel + "성 x" + stack.Number);
            button.onClick.AddListener(() => SelectBag(index));
            if (_selection == SelectionKind.Bag && _selectedIndex == index)
            {
                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.25f, 0.45f, 0.3f, 1f);
                }
            }

            _bagButtons.Add(button);
        }
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
        _selection = SelectionKind.Bag;
        _selectedIndex = index;
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
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        return content;
    }

    private Button CreateRow(RectTransform parent, string label)
    {
        var rect = CreateRect("Row", parent);
        var element = rect.gameObject.AddComponent<LayoutElement>();
        element.minHeight = ROW_HEIGHT;
        element.preferredHeight = ROW_HEIGHT;
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.18f, 0.22f, 1f);
        var button = rect.gameObject.AddComponent<Button>();
        var text = CreateText("Label", rect, label, 20);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(10f, 2f, 10f, 2f);
        return button;
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

    private static void ClearButtons(List<Button> buttons)
    {
        for (var i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                Destroy(buttons[i].gameObject);
            }
        }

        buttons.Clear();
    }
}
