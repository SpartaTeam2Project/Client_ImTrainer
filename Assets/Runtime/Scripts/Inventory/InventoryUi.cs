using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 판 안에서 가방과 장착을 다루는 창. 구매 목록은 ShopUi가 그린다.
/// </summary>
public class InventoryUi : MonoBehaviour, IInventoryKeyboardTarget
{
    /// <summary>
    /// 버튼 그림 한 벌. Normal은 평소, Focus는 마우스를 올리거나 누를 때 그림이다.
    /// </summary>
    [System.Serializable]
    private struct ButtonSkin
    {
        public Sprite Normal;
        public Sprite Focus;
    }

    private enum SelectionKind
    {
        None,
        Bag,
        Equipment,
        Shop
    }

    private const float OPEN_FADE_DURATION = 0.2f;
    private const float CLOSE_FADE_DURATION = 0.15f;
    private const string OPEN_SOUND = "inventory_open";
    private const string CLOSE_SOUND = "inventory_close";
    private const string EQUIP_SOUND = "inventory_equip";
    private const string UNEQUIP_SOUND = "inventory_unequip";
    private const string ERROR_SOUND = "error";
    private const string PRESS_SOUND = "cursor";
    private const string SELL_SOUND = "inventory_sell";
    private const string STAR_UP_SOUND = "inventory_star_up";
    private const string EVOLUTION_START_SOUND = "inventory_star_evolution_start";
    private const string EVOLUTION_END_SOUND = "inventory_star_evolution_end";
    // 키보드로 합성 대상을 고르는 동안 합성, 해제 버튼이 확정, 취소로 바뀐다.
    private const string CONFIRM_LABEL = "확정";
    private const string CANCEL_LABEL = "취소";
    // 확정, 취소로 바뀔 때 글자를 튕겨서 눈에 띄게 한다.
    private const float LABEL_PUNCH_SCALE = 0.3f;
    private const float LABEL_PUNCH_DURATION = 0.3f;
    private const int LABEL_PUNCH_VIBRATO = 8;
    private const float LABEL_PUNCH_ELASTICITY = 0.8f;

    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _panelGroup;
    [SerializeField] private ScreenBackdrop _backdrop;
    [SerializeField] private EquipmentUi _equipmentUi;
    [SerializeField] private ShopUi _shopUi;
    [SerializeField] private HoverInformation _information;
    [SerializeField] private InventoryItem _slotPrefab;
    [SerializeField] private ResourceBar _resourceBar;
    [SerializeField] private InventoryToast _toast;
    [SerializeField] private RectTransform _bagContent;
    [SerializeField] private PurchaseBeam _purchaseBeam;

    [Header("Actions")]
    [SerializeField] private Button _synthesizeButton;
    [SerializeField] private Button _equipButton;
    [SerializeField] private Button _unequipButton;
    [SerializeField] private Button _sellButton;
    [SerializeField] private Button _discardButton;
    [SerializeField] private Button _closeButton;

    [Header("Synthesis Pick")]
    // 키보드로 합성 대상을 고르는 동안 합성 버튼은 확정, 해제 버튼은 취소 그림으로 바뀐다.
    [SerializeField] private ButtonSkin _confirmSkin;
    [SerializeField] private ButtonSkin _cancelSkin;

    [Header("Drag")]
    [SerializeField] private InventoryDropZone _trashZone;
    [SerializeField] private Vector2 _dragGhostSize = new Vector2(96f, 96f);

    private readonly List<InventoryItem> _bagSlots = new List<InventoryItem>();
    private Image _dragGhost;
    private InventorySlotRef _drag = InventorySlotRef.None;
    private InventoryFocusNavigator _navigator;
    private InventoryKeyboard _keyboard;
    private PurchaseBeamDirector _purchaseDirector;
    private InventorySellDirector _sellDirector;
    private ScrollRect _bagScroll;
    private InventorySlotRef _synthesized = InventorySlotRef.None;
    private SelectionKind _selection = SelectionKind.None;
    private int _selectedIndex = -1;
    private int _selectedUid = -1;
    private int _selectedStar;
    private bool _open;
    private bool _holdTime;
    private bool _subscribed;
    private Coroutine _showRoutine;
    private Tween _fadeTween;
    private bool _closing;
    private PressFlash _synthesizePress;
    private PressFlash _equipPress;
    private PressFlash _unequipPress;
    private PressFlash _sellPress;
    private TMP_Text _synthesizeLabel;
    private TMP_Text _unequipLabel;
    private string _synthesizeText;
    private string _unequipText;
    private ButtonSkin _synthesizeSkin;
    private ButtonSkin _unequipSkin;
    private bool _pickShown;

    private void Awake()
    {
        _navigator = new InventoryFocusNavigator();
        _keyboard = new InventoryKeyboard(this, _navigator);
        _purchaseDirector = new PurchaseBeamDirector(_purchaseBeam, _bagSlots, _navigator);
        _sellDirector = new InventorySellDirector(_bagSlots, _equipmentUi, _slotPrefab != null ? _slotPrefab.DissolveTemplate : null, Refresh);
        _bagScroll = _bagContent != null ? _bagContent.GetComponentInParent<ScrollRect>() : null;
        AddAction(_synthesizeButton, () => PressAction(InventoryHotkey.Synthesize, false, SynthesizeSelected));
        AddAction(_equipButton, () => PressAction(InventoryHotkey.Equip, false, EquipSelected));
        AddAction(_unequipButton, () => PressAction(InventoryHotkey.Unequip, false, UnequipSelected));
        AddAction(_sellButton, () => PressAction(InventoryHotkey.Sell, false, SellSelected));
        AddAction(_discardButton, DiscardSelected);
        AddAction(_closeButton, Close);
        _synthesizePress = PressFlash.ForButton(_synthesizeButton);
        _equipPress = PressFlash.ForButton(_equipButton);
        _unequipPress = PressFlash.ForButton(_unequipButton);
        _sellPress = PressFlash.ForButton(_sellButton);
        _synthesizeLabel = FindLabel(_synthesizeButton, out _synthesizeText);
        _unequipLabel = FindLabel(_unequipButton, out _unequipText);
        _synthesizeSkin = ReadSkin(_synthesizeButton);
        _unequipSkin = ReadSkin(_unequipButton);
        if (_shopUi != null)
        {
            _shopUi.Bind(Refresh, SelectShop, _purchaseDirector);
        }

        if (_equipmentUi != null)
        {
            _equipmentUi.BindDrag(this);
        }

        CreateDragGhost();
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
        EndDrag();
        _sellDirector?.FinishAll();
        _purchaseDirector?.Stop();
        if (_holdTime && Managers.Instance != null && Managers.Instance.CurrentState == GameState.Playing)
        {
            Time.timeScale = 1f;
        }

        _holdTime = false;
        _open = false;
        _showRoutine = null;
        _closing = false;
        ResetFade();
        if (_backdrop != null)
        {
            _backdrop.Release();
        }
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

        // 창이 열려 있으면 ESC를 먼저 가져가서 일시정지 대신 창을 닫는다. 합성할 칸을 고르는 중이면 합성만 취소한다.
        if (_open && inputManager.ConsumePausePressed())
        {
            if (!_keyboard.TryCancel())
            {
                Close();
            }

            return;
        }

        if (_open && _shopUi != null && inputManager.ConsumeShopRefreshPressed())
        {
            _shopUi.PressRefresh();
        }

        if (_open && _shopUi != null)
        {
            var slot = inputManager.ConsumeNumberSlot();
            if (slot >= 0)
            {
                _shopUi.Purchase(slot);
            }
        }

        // 창이 다 열린 뒤에만 받는다. 끄는 중에는 칸 선택이 바뀌지 않게 한다.
        if (_open && !_closing && _showRoutine == null && _drag.IsEmpty)
        {
            _keyboard.Tick(inputManager);
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

        // 닫히는 중에 다시 열면 남은 페이드를 건너뛰고 끈 다음 새로 연다.
        if (_closing)
        {
            FinishClose();
        }

        _open = true;
        _holdTime = Managers.Instance.CurrentState == GameState.Playing;
        if (_holdTime)
        {
            Time.timeScale = 0f;
        }

        PlaySound(OPEN_SOUND);
        _showRoutine = StartCoroutine(ShowAfterCapture());
    }

    // 창이 그려지기 전 화면을 배경으로 담아야 해서 캡처한 뒤에 창을 켠다.
    private IEnumerator ShowAfterCapture()
    {
        if (_backdrop != null)
        {
            yield return new WaitForEndOfFrame();
            _backdrop.Capture();
        }

        _showRoutine = null;
        _canvas.gameObject.SetActive(true);
        Refresh();
        PlayOpen();
    }

    /// <summary>
    /// 패널을 서서히 나타낸다. 창이 열려 있는 동안 시간이 멈춰 있어서 시간 정지와 상관없이 돈다.
    /// </summary>
    private void PlayOpen()
    {
        if (_panelGroup == null)
        {
            return;
        }

        ResetFade();
        _panelGroup.alpha = 0f;
        _fadeTween = _panelGroup.DOFade(1f, OPEN_FADE_DURATION).SetUpdate(true).SetLink(gameObject);
    }

    /// <summary>
    /// 패널을 서서히 감춘 뒤 창을 끈다. 사라지는 동안에는 버튼이 눌리지 않는다.
    /// </summary>
    private void Close()
    {
        _open = false;
        _keyboard.Reset();
        EndDrag();
        // 디졸브 중이던 판매는 기다리지 않고 바로 처리한다.
        _sellDirector.FinishAll();
        _purchaseDirector.Stop();
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        if (_closing)
        {
            return;
        }

        PlaySound(CLOSE_SOUND);
        if (_panelGroup == null || _canvas == null || !_canvas.gameObject.activeSelf)
        {
            FinishClose();
            return;
        }

        if (_fadeTween != null)
        {
            _fadeTween.Kill();
        }

        _closing = true;
        _panelGroup.blocksRaycasts = false;
        _fadeTween = _panelGroup.DOFade(0f, CLOSE_FADE_DURATION).SetUpdate(true).SetLink(gameObject).OnComplete(FinishClose);
    }

    /// <summary>
    /// 페이드가 끝나면 배경 캡처를 돌려주고 창을 끈 뒤 멈춰 둔 시간을 되돌린다.
    /// </summary>
    private void FinishClose()
    {
        _closing = false;
        ResetFade();
        _synthesizePress.Release();
        _equipPress.Release();
        _unequipPress.Release();
        _sellPress.Release();
        if (_toast != null)
        {
            _toast.Hide();
        }

        if (_backdrop != null)
        {
            _backdrop.Release();
        }

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

    /// <summary>
    /// 페이드 도중에 끊겨도 다음에 열 때 패널이 흐리거나 눌리지 않는 채로 남지 않게 되돌린다.
    /// </summary>
    private void ResetFade()
    {
        if (_fadeTween != null)
        {
            _fadeTween.Kill();
            _fadeTween = null;
        }

        if (_panelGroup != null)
        {
            _panelGroup.alpha = 1f;
            _panelGroup.blocksRaycasts = true;
        }
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

        // 증강 선택 중에 열면 숫자키가 겹치고, 닫을 때 멈춘 시간이 풀린다.
        if (Managers.Instance.TryGetManager<UpgradeManager>(out var upgradeManager) && upgradeManager.IsChoosing)
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

        RebuildBag(itemManager, playerId);
        _purchaseDirector.Reapply();
        _keyboard.Validate(itemManager.GetInventory(playerId), itemManager.GetEquipment(playerId));
        RefreshPickMode();
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

        RefreshSynthesisPick(itemManager, playerId);
        RefreshInformation(itemManager, playerId);
        _sellDirector.Reapply();
    }

    /// <summary>
    /// 키보드로 합성할 칸을 고르는 중이면 후보 칸을 깜빡이고 고르는 칸은 선택 표시로 둔다. 끌고 있으면 끌기 표시를 그대로 둔다.
    /// </summary>
    private void RefreshSynthesisPick(ItemManager itemManager, int playerId)
    {
        if (!_drag.IsEmpty)
        {
            return;
        }

        if (!_keyboard.Synthesizing)
        {
            ClearMergeHints();
            return;
        }

        ShowMergeHints(itemManager, playerId, _keyboard.Source);
        var cursor = GetSlotView(_keyboard.CursorKind, _keyboard.CursorIndex);
        if (cursor != null)
        {
            cursor.SetMergeHint(false);
            cursor.SetSelected(true);
        }
    }

    private InventoryItem GetSlotView(InventorySlotDrag.SlotKind kind, int index)
    {
        if (kind == InventorySlotDrag.SlotKind.Bag)
        {
            return index >= 0 && index < _bagSlots.Count ? _bagSlots[index] : null;
        }

        return kind == InventorySlotDrag.SlotKind.Equipment && _equipmentUi != null ? _equipmentUi.GetSlot(index) : null;
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
            var item = offer != null && !offer.Sold ? itemManager.CreateOfferItem(offer) : null;
            if (item == null)
            {
                _information.Clear();
                return;
            }

            _information.Show(item);
            return;
        }

        // 합성할 칸을 고르는 중이면 고르는 칸 포켓몬을 보여 준다.
        var kind = _selection;
        var index = _selectedIndex;
        if (_keyboard.Synthesizing)
        {
            kind = _keyboard.CursorKind == InventorySlotDrag.SlotKind.Bag ? SelectionKind.Bag : SelectionKind.Equipment;
            index = _keyboard.CursorIndex;
        }

        var holder = kind == SelectionKind.Bag ? itemManager.GetInventory(playerId)
            : kind == SelectionKind.Equipment ? itemManager.GetEquipment(playerId)
            : null;
        if (holder == null || index < 0 || index >= holder.Stacks.Count || holder.Stacks[index].Empty)
        {
            _information.Clear();
            return;
        }

        _information.Show(holder.Stacks[index].Item);
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

        _navigator.SetSlots(_bagSlots, bag.CountFilled(), _equipmentUi, _bagScroll);
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
            slot.Drag.Bind(this, InventorySlotDrag.SlotKind.Bag, index);
            _bagSlots.Add(slot);
        }
    }

    private void ResolveBagSelection(InventoryHolder bag)
    {
        if (_selection != SelectionKind.Bag || _selectedUid < 0)
        {
            return;
        }

        // 같은 종, 같은 성이 여러 칸이면 첫 칸으로 끌려가지 않게 고른 칸이 그대로면 그 칸을 둔다.
        var index = InventoryMerge.ResolveBagIndex(bag,
            new InventorySlotRef(InventorySlotDrag.SlotKind.Bag, _selectedIndex, _selectedUid, _selectedStar));
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
        if (_sellDirector.IsSelling(InventorySlotDrag.SlotKind.Bag, index))
        {
            return;
        }

        _keyboard.Reset();
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
        if (_sellDirector.IsSelling(InventorySlotDrag.SlotKind.Equipment, slot))
        {
            return;
        }

        _keyboard.Reset();
        _selection = SelectionKind.Equipment;
        _selectedIndex = slot;
        Refresh();
    }

    /// <summary>
    /// 상점 칸을 고른다. 정보 창만 보여 주고 장착, 판매 같은 동작은 하지 않는다.
    /// </summary>
    private void SelectShop(int index)
    {
        _keyboard.Reset();
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
        if (_keyboard.TryConfirm())
        {
            return;
        }

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

        if (itemManager.TryEquip(playerId, _selectedIndex))
        {
            PlaySound(EQUIP_SOUND);
        }

        Refresh();
    }

    private void UnequipSelected()
    {
        if (_keyboard.TryCancel())
        {
            return;
        }

        if (_selection != SelectionKind.Equipment || !TryGetContext(out var itemManager, out var playerId)
            || !HasEquipped(itemManager, playerId, _selectedIndex))
        {
            return;
        }

        PlaySoundIf(TryRemoveEquipped(itemManager, playerId, _selectedIndex, itemManager.TryUnequip), UNEQUIP_SOUND, ERROR_SOUND);

        Refresh();
    }

    private void SellSelected()
    {
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        if (TryBeginSell(GetSelectedRef(itemManager, playerId), null))
        {
            return;
        }

        if (_selection == SelectionKind.Bag)
        {
            PlaySoundIf(itemManager.TrySell(playerId, _selectedIndex), SELL_SOUND, ERROR_SOUND);
        }
        else if (_selection == SelectionKind.Equipment && HasEquipped(itemManager, playerId, _selectedIndex))
        {
            PlaySoundIf(TryRemoveEquipped(itemManager, playerId, _selectedIndex, itemManager.TrySellEquipped), SELL_SOUND, ERROR_SOUND);
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
        else if (_selection == SelectionKind.Equipment && HasEquipped(itemManager, playerId, _selectedIndex))
        {
            PlaySoundIf(TryRemoveEquipped(itemManager, playerId, _selectedIndex, itemManager.TryDiscardEquipped), null, ERROR_SOUND);
        }

        Refresh();
    }

    /// <summary>
    /// 칸 끌기를 시작한다. 빈 칸이면 false를 돌려줘서 칸이 스크롤로 넘기게 한다.
    /// </summary>
    public bool BeginDrag(InventorySlotDrag.SlotKind kind, int index, PointerEventData eventData)
    {
        if (!_open || _closing || !TryGetContext(out var itemManager, out var playerId))
        {
            return false;
        }

        var holder = kind == InventorySlotDrag.SlotKind.Bag ? itemManager.GetInventory(playerId)
            : kind == InventorySlotDrag.SlotKind.Equipment ? itemManager.GetEquipment(playerId)
            : null;
        if (holder == null || index < 0 || index >= holder.Stacks.Count || holder.Stacks[index].Empty || holder.Stacks[index].Item == null
            || _sellDirector.IsSelling(kind, index))
        {
            return false;
        }

        var item = holder.Stacks[index].Item;
        _drag = new InventorySlotRef(kind, index, item.uid, item.upgradeLevel);
        if (kind == InventorySlotDrag.SlotKind.Bag)
        {
            SelectBag(index);
        }
        else
        {
            SelectEquipment(index);
        }

        ShowDragGhost(itemManager, item.uid);
        ShowMergeHints(itemManager, playerId, _drag);
        MoveDrag(eventData);
        if (_trashZone != null)
        {
            _trashZone.SetHighlight(true);
        }

        return true;
    }

    /// <summary>
    /// 끌고 있는 그림을 포인터 위치로 옮긴다.
    /// </summary>
    public void MoveDrag(PointerEventData eventData)
    {
        if (_dragGhost == null || !_dragGhost.gameObject.activeSelf || _canvas == null)
        {
            return;
        }

        var canvasRect = (RectTransform)_canvas.transform;
        var eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out var local))
        {
            _dragGhost.rectTransform.localPosition = local;
        }
    }

    /// <summary>
    /// 끌기를 끝낸다. 놓은 자리가 없으면 아무것도 하지 않고 취소된다.
    /// </summary>
    public void EndDrag()
    {
        ClearMergeHints();
        _drag = InventorySlotRef.None;
        if (_dragGhost != null)
        {
            _dragGhost.gameObject.SetActive(false);
        }

        if (_trashZone != null)
        {
            _trashZone.SetHighlight(false);
        }
    }

    /// <summary>
    /// 가방이나 장착 칸 위에 놓았다.
    /// </summary>
    public void DropOnSlot(InventorySlotDrag.SlotKind targetKind, int targetIndex)
    {
        if (_drag.IsEmpty || !TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        if (_drag.Kind == InventorySlotDrag.SlotKind.Bag)
        {
            DropBag(itemManager, playerId, targetKind, targetIndex);
        }
        else
        {
            DropEquipment(itemManager, playerId, targetKind, targetIndex);
        }

        EndDrag();
        Refresh();
    }

    /// <summary>
    /// 칸이 아닌 영역에 놓았다. 가방 영역이면 해제, 휴지통이면 판매한다.
    /// </summary>
    public void DropOnZone(InventoryDropZone.ZoneKind zone)
    {
        if (_drag.IsEmpty || !TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        // 휴지통에 놓으면 끌고 있던 그림이 그 자리에서 디졸브된다.
        if (zone == InventoryDropZone.ZoneKind.Trash && TryBeginSell(ResolveDrag(itemManager, playerId), _dragGhost))
        {
            EndDrag();
            return;
        }

        if (zone == InventoryDropZone.ZoneKind.Trash)
        {
            if (_drag.Kind == InventorySlotDrag.SlotKind.Bag)
            {
                var bagIndex = InventoryMerge.ResolveBagIndex(itemManager.GetInventory(playerId), _drag);
                if (bagIndex >= 0)
                {
                    PlaySoundIf(itemManager.TrySell(playerId, bagIndex), SELL_SOUND, ERROR_SOUND);
                }
            }
            else
            {
                PlaySoundIf(TryRemoveEquipped(itemManager, playerId, _drag.Index, itemManager.TrySellEquipped), SELL_SOUND, ERROR_SOUND);
            }
        }
        else if (zone == InventoryDropZone.ZoneKind.Bag && _drag.Kind == InventorySlotDrag.SlotKind.Equipment)
        {
            PlaySoundIf(TryRemoveEquipped(itemManager, playerId, _drag.Index, itemManager.TryUnequip), UNEQUIP_SOUND, ERROR_SOUND);
        }

        EndDrag();
        Refresh();
    }

    /// <summary>
    /// 판매를 디졸브로 시작한다. 시작했으면 선택을 풀고 true. 디졸브로 팔 수 없으면 false라서 부른 쪽이 바로 판다.
    /// dragGhost가 있으면 칸 아이콘 대신 끌기 그림이 디졸브된다.
    /// </summary>
    private bool TryBeginSell(InventorySlotRef target, Image dragGhost)
    {
        // 디졸브가 끝나면 칸이 비어 이름을 알 수 없어서 시작하기 전에 읽어 둔다.
        var name = GetItemName(target);
        if (!_sellDirector.TryBegin(target, dragGhost))
        {
            return false;
        }

        // 판매 대사는 디졸브가 끝날 때가 아니라 시작할 때 띄운다.
        ShowNotice(SellMessage.Get(name));
        PlaySound(SELL_SOUND);
        ClearSelection();
        Refresh();
        return true;
    }

    private string GetItemName(InventorySlotRef target)
    {
        if (target.IsEmpty || !TryGetContext(out var itemManager, out var playerId))
        {
            return null;
        }

        var holder = target.Kind == InventorySlotDrag.SlotKind.Bag ? itemManager.GetInventory(playerId) : itemManager.GetEquipment(playerId);
        var index = target.Kind == InventorySlotDrag.SlotKind.Bag ? InventoryMerge.ResolveBagIndex(holder, target) : target.Index;
        if (holder == null || index < 0 || index >= holder.Stacks.Count || holder.Stacks[index].Item == null)
        {
            return null;
        }

        return holder.Stacks[index].Item.name;
    }

    private InventorySlotRef GetSelectedRef(ItemManager itemManager, int playerId)
    {
        var kind = _selection == SelectionKind.Bag ? InventorySlotDrag.SlotKind.Bag
            : _selection == SelectionKind.Equipment ? InventorySlotDrag.SlotKind.Equipment
            : InventorySlotDrag.SlotKind.None;
        var holder = kind == InventorySlotDrag.SlotKind.Bag ? itemManager.GetInventory(playerId)
            : kind == InventorySlotDrag.SlotKind.Equipment ? itemManager.GetEquipment(playerId)
            : null;
        if (holder == null || _selectedIndex < 0 || _selectedIndex >= holder.Stacks.Count
            || holder.Stacks[_selectedIndex].Empty || holder.Stacks[_selectedIndex].Item == null)
        {
            return InventorySlotRef.None;
        }

        var item = holder.Stacks[_selectedIndex].Item;
        return new InventorySlotRef(kind, _selectedIndex, item.uid, item.upgradeLevel);
    }

    /// <summary>
    /// 끌고 있는 칸의 지금 번호. 가방은 끄는 사이 정렬될 수 있어서 종과 성으로 다시 찾는다.
    /// </summary>
    private InventorySlotRef ResolveDrag(ItemManager itemManager, int playerId)
    {
        if (_drag.Kind != InventorySlotDrag.SlotKind.Bag)
        {
            return _drag;
        }

        var bagIndex = InventoryMerge.ResolveBagIndex(itemManager.GetInventory(playerId), _drag);
        return bagIndex >= 0 ? new InventorySlotRef(_drag.Kind, bagIndex, _drag.Uid, _drag.Star) : InventorySlotRef.None;
    }

    private void DropBag(ItemManager itemManager, int playerId, InventorySlotDrag.SlotKind targetKind, int targetIndex)
    {
        var bag = itemManager.GetInventory(playerId);
        var bagIndex = InventoryMerge.ResolveBagIndex(bag, _drag);
        if (bagIndex < 0)
        {
            return;
        }

        if (InventoryMerge.IsTarget(bag, itemManager.GetEquipment(playerId), _drag, targetKind, targetIndex, false))
        {
            InventoryMerge.Merge(itemManager, playerId, _drag, targetKind, targetIndex);
            return;
        }

        // 가방은 자동 정렬이라 자리만 옮기는 동작은 없다. 같은 종, 같은 성 칸에 놓을 때만 합성한다.
        if (targetKind == InventorySlotDrag.SlotKind.Equipment)
        {
            if (itemManager.TryEquipToSlot(playerId, bagIndex, targetIndex))
            {
                PlaySound(EQUIP_SOUND);
            }
        }
    }

    private void DropEquipment(ItemManager itemManager, int playerId, InventorySlotDrag.SlotKind targetKind, int targetIndex)
    {
        // 같은 종, 같은 성 칸에 놓으면 합성하고 결과는 놓은 장착 칸, 가방이면 끈 장착 칸에 남긴다.
        if (InventoryMerge.IsTarget(itemManager.GetInventory(playerId), itemManager.GetEquipment(playerId), _drag, targetKind, targetIndex, false))
        {
            InventoryMerge.Merge(itemManager, playerId, _drag, targetKind, targetIndex);
            return;
        }

        if (targetKind == InventorySlotDrag.SlotKind.Equipment)
        {
            if (targetIndex != _drag.Index)
            {
                itemManager.TrySwapEquipped(playerId, _drag.Index, targetIndex);
            }

            return;
        }

        if (targetKind == InventorySlotDrag.SlotKind.Bag)
        {
            PlaySoundIf(TryRemoveEquipped(itemManager, playerId, _drag.Index, itemManager.TryUnequip), UNEQUIP_SOUND, ERROR_SOUND);
        }
    }

    /// <summary>
    /// 가방과 장착에서 출발 칸 포켓몬과 같은 종, 같은 성인 칸 테두리를 깜빡인다. 출발 칸은 뺀다.
    /// </summary>
    private void ShowMergeHints(ItemManager itemManager, int playerId, InventorySlotRef source)
    {
        var bag = itemManager.GetInventory(playerId);
        var equipment = itemManager.GetEquipment(playerId);
        for (var i = 0; i < _bagSlots.Count; i++)
        {
            _bagSlots[i].SetMergeHint(InventoryMerge.IsTarget(bag, equipment, source, InventorySlotDrag.SlotKind.Bag, i, false));
        }

        if (_equipmentUi != null)
        {
            var equipmentExcept = source.Kind == InventorySlotDrag.SlotKind.Equipment ? source.Index : -1;
            _equipmentUi.SetMergeHints(equipment, source.Uid, source.Star, equipmentExcept);
        }
    }

    private void ClearMergeHints()
    {
        for (var i = 0; i < _bagSlots.Count; i++)
        {
            if (_bagSlots[i] != null)
            {
                _bagSlots[i].SetMergeHint(false);
            }
        }

        if (_equipmentUi != null)
        {
            _equipmentUi.SetMergeHints(null, -1, 0, -1);
        }
    }

    private void CreateDragGhost()
    {
        if (_canvas == null)
        {
            return;
        }

        var ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ghost.layer = _canvas.gameObject.layer;
        ghost.transform.SetParent(_canvas.transform, false);
        _dragGhost = ghost.GetComponent<Image>();
        _dragGhost.raycastTarget = false;
        _dragGhost.preserveAspect = true;
        _dragGhost.rectTransform.sizeDelta = _dragGhostSize;
        ghost.SetActive(false);
    }

    private void ShowDragGhost(ItemManager itemManager, int uid)
    {
        if (_dragGhost == null)
        {
            return;
        }

        var visual = itemManager.GetVisual(uid);
        _dragGhost.sprite = visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
        _dragGhost.enabled = _dragGhost.sprite != null;
        _dragGhost.transform.SetAsLastSibling();
        _dragGhost.gameObject.SetActive(true);
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

    bool IInventoryKeyboardTarget.TryGetSelection(out InventorySlotDrag.SlotKind kind, out int index)
    {
        kind = _selection == SelectionKind.Bag ? InventorySlotDrag.SlotKind.Bag
            : _selection == SelectionKind.Equipment ? InventorySlotDrag.SlotKind.Equipment
            : InventorySlotDrag.SlotKind.None;
        index = _selectedIndex;
        return kind != InventorySlotDrag.SlotKind.None && index >= 0;
    }

    bool IInventoryKeyboardTarget.TryGetHolders(out InventoryHolder bag, out InventoryHolder equipment)
    {
        bag = null;
        equipment = null;
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return false;
        }

        bag = itemManager.GetInventory(playerId);
        equipment = itemManager.GetEquipment(playerId);
        return true;
    }

    void IInventoryKeyboardTarget.Select(InventorySlotDrag.SlotKind kind, int index)
    {
        if (kind == InventorySlotDrag.SlotKind.Bag)
        {
            SelectBag(index);
        }
        else if (kind == InventorySlotDrag.SlotKind.Equipment)
        {
            SelectEquipment(index);
        }
    }

    void IInventoryKeyboardTarget.Equip()
    {
        var index = _selectedIndex;
        EquipSelected();
        KeepBagFocus(index);
    }

    void IInventoryKeyboardTarget.Unequip()
    {
        UnequipSelected();
    }

    void IInventoryKeyboardTarget.Sell()
    {
        var wasBag = _selection == SelectionKind.Bag;
        var index = _selectedIndex;
        SellSelected();
        if (wasBag)
        {
            KeepBagFocus(index);
        }
    }

    void IInventoryKeyboardTarget.Synthesize(InventorySlotRef source, InventorySlotDrag.SlotKind targetKind, int targetIndex)
    {
        if (!TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        _synthesized = InventorySlotRef.None;
        if (!InventoryMerge.Merge(itemManager, playerId, source, targetKind, targetIndex) || _synthesized.IsEmpty)
        {
            Refresh();
            return;
        }

        // 결과 칸을 골라서 이어서 키보드로 다룰 수 있게 한다.
        var resultIndex = _synthesized.Kind == InventorySlotDrag.SlotKind.Bag
            ? InventoryMerge.ResolveBagIndex(itemManager.GetInventory(playerId), _synthesized)
            : _synthesized.Index;
        ((IInventoryKeyboardTarget)this).Select(_synthesized.Kind, resultIndex);
    }

    void IInventoryKeyboardTarget.Refresh()
    {
        Refresh();
    }

    void IInventoryKeyboardTarget.Notify(string message)
    {
        ShowNotice(message);
    }

    void IInventoryKeyboardTarget.ShowHotkeyPress(InventoryHotkey hotkey)
    {
        PressAction(hotkey, true, null);
    }

    /// <summary>
    /// 버튼을 누른 연출과 소리를 낸 뒤 동작한다. 단축키면 눌린 그림도 잠깐 덮는다. 판매 단축키는 휴지통이 반응한다.
    /// 누르면 cursor 소리를 내고, 합성 확정은 키보드가 결과에 따라 select나 error를, 판매는 결과에 따라 판매 소리나 error를 낸다.
    /// </summary>
    private void PressAction(InventoryHotkey hotkey, bool fromHotkey, System.Action action)
    {
        var sound = PRESS_SOUND;
        switch (hotkey)
        {
            case InventoryHotkey.Synthesize:
                PlayPress(_synthesizePress, fromHotkey);
                if (_keyboard.Synthesizing)
                {
                    sound = null;
                }

                break;
            case InventoryHotkey.Equip:
                PlayPress(_equipPress, fromHotkey);
                break;
            case InventoryHotkey.Unequip:
                PlayPress(_unequipPress, fromHotkey);
                break;
            case InventoryHotkey.Sell:
                if (fromHotkey && _trashZone != null)
                {
                    _trashZone.PlayPress();
                }
                else
                {
                    PlayPress(_sellPress, fromHotkey);
                }

                sound = null;
                break;
        }

        if (sound != null)
        {
            PlaySound(sound);
        }

        action?.Invoke();
    }

    private static void PlayPress(PressFlash press, bool fromHotkey)
    {
        if (fromHotkey)
        {
            press.Play();
        }
        else
        {
            press.Punch();
        }
    }

    /// <summary>
    /// 키보드로 가방 포켓몬을 장착하거나 팔아 고른 칸이 비면 같은 자리 칸을 다시 고른다. 연달아 누르기 좋게 한다.
    /// </summary>
    private void KeepBagFocus(int index)
    {
        if (_selection != SelectionKind.None || !TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        var bag = itemManager.GetInventory(playerId);
        var count = bag != null ? bag.CountFilled() : 0;
        if (count > 0)
        {
            SelectBag(Mathf.Min(index, count - 1));
        }
    }

    private void HandleInventoryNotice(InventoryNotice notice)
    {
        if (!_open || !TryGetContext(out _, out var playerId) || notice.PlayerId != playerId)
        {
            return;
        }

        ShowNotice(notice.Message);
    }

    private void ShowNotice(string message)
    {
        if (_toast != null)
        {
            _toast.Show(message);
        }
    }

    /// <summary>
    /// 키보드로 합성 대상을 고르는 동안에는 합성, 해제 버튼을 확정, 취소 글자와 그림으로 바꾸고 장착 버튼을 숨긴다.
    /// 모드가 바뀔 때만 바꾼다.
    /// </summary>
    private void RefreshPickMode()
    {
        var picking = _keyboard.Synthesizing;
        if (picking == _pickShown)
        {
            return;
        }

        _pickShown = picking;
        SetLabel(_synthesizeLabel, picking ? CONFIRM_LABEL : _synthesizeText, picking);
        SetLabel(_unequipLabel, picking ? CANCEL_LABEL : _unequipText, picking);
        // 단축키로 덮어 둔 눌린 그림이 남으면 새 그림이 늦게 보여서 바로 걷는다.
        _synthesizePress.ClearSprite();
        _unequipPress.ClearSprite();
        ApplySkin(_synthesizeButton, picking ? _confirmSkin : _synthesizeSkin);
        ApplySkin(_unequipButton, picking ? _cancelSkin : _unequipSkin);
        if (_equipButton != null)
        {
            _equipButton.gameObject.SetActive(!picking);
        }
    }

    private static ButtonSkin ReadSkin(Button button)
    {
        return button != null && button.image != null
            ? new ButtonSkin { Normal = button.image.sprite, Focus = button.spriteState.highlightedSprite }
            : default;
    }

    /// <summary>
    /// 버튼 그림과 Sprite Swap의 포커스 그림(Highlighted, Pressed)을 바꾼다. 그림이 비어 있으면 그대로 둔다.
    /// </summary>
    private static void ApplySkin(Button button, ButtonSkin skin)
    {
        if (button == null || button.image == null || skin.Normal == null)
        {
            return;
        }

        button.image.sprite = skin.Normal;
        var state = button.spriteState;
        state.highlightedSprite = skin.Focus;
        state.pressedSprite = skin.Focus;
        button.spriteState = state;
    }

    private static TMP_Text FindLabel(Button button, out string text)
    {
        var label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        text = label != null ? label.text : null;
        return label;
    }

    /// <summary>
    /// 글자가 바뀌면 바꾸고, punch면 튕기는 연출을 건다. 창이 열려 있는 동안 시간이 멈춰 있어서 시간 정지와 상관없이 돈다.
    /// </summary>
    private static void SetLabel(TMP_Text label, string text, bool punch)
    {
        if (label == null || label.text == text)
        {
            return;
        }

        label.text = text;
        if (!punch)
        {
            return;
        }

        // 연달아 바뀌어도 크기가 어긋나지 않게 이전 연출을 끝낸 크기에서 다시 건다.
        label.transform.DOKill(true);
        label.transform
            .DOPunchScale(Vector3.one * LABEL_PUNCH_SCALE, LABEL_PUNCH_DURATION, LABEL_PUNCH_VIBRATO, LABEL_PUNCH_ELASTICITY)
            .SetUpdate(true)
            .SetLink(label.gameObject);
    }

    private void HandleInventoryChanged(InventoryChanged changed)
    {
        if (!_open || !TryGetContext(out _, out var playerId) || changed.PlayerId != playerId)
        {
            return;
        }

        Refresh();
    }

    /// <summary>
    /// 합성 결과 칸에 연출을 건다. 성이 오르면 별을 튕기고, 진화하면 이전 그림이 빛나며 새 그림으로 바뀐다.
    /// </summary>
    private void HandleItemSynthesized(ItemSynthesized synthesized)
    {
        if (!_open || !TryGetContext(out var itemManager, out var playerId) || synthesized.PlayerId != playerId)
        {
            return;
        }

        _synthesized = synthesized.EquipmentSlot >= 0
            ? new InventorySlotRef(InventorySlotDrag.SlotKind.Equipment, synthesized.EquipmentSlot, synthesized.Uid, synthesized.UpgradeLevel)
            : new InventorySlotRef(InventorySlotDrag.SlotKind.Bag, -1, synthesized.Uid, synthesized.UpgradeLevel);
        var slot = FindSynthesizedSlot(itemManager, synthesized);
        if (!synthesized.Evolved)
        {
            PlaySound(STAR_UP_SOUND);
            if (slot != null)
            {
                slot.PlayStarUp(synthesized.UpgradeLevel);
            }

            return;
        }

        var startSound = PlaySound(EVOLUTION_START_SOUND);
        if (slot != null)
        {
            slot.PlayEvolution(GetPortrait(itemManager, synthesized.PreviousUid), GetPortrait(itemManager, synthesized.Uid),
                GetLength(startSound), () => PlaySound(EVOLUTION_END_SOUND));
        }
    }

    private InventoryItem FindSynthesizedSlot(ItemManager itemManager, ItemSynthesized synthesized)
    {
        if (synthesized.EquipmentSlot >= 0)
        {
            return _equipmentUi != null ? _equipmentUi.GetSlot(synthesized.EquipmentSlot) : null;
        }

        var bag = itemManager.GetInventory(synthesized.PlayerId);
        var index = bag != null ? bag.FindIndex(synthesized.Uid, synthesized.UpgradeLevel) : -1;
        return index >= 0 && index < _bagSlots.Count ? _bagSlots[index] : null;
    }

    private static Sprite GetPortrait(ItemManager itemManager, int uid)
    {
        var visual = itemManager.GetVisual(uid);
        return visual != null ? MonsterVisualData.FirstFrame(visual.Icon) : null;
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
        eventManager.Subscribe<InventoryNotice>(HandleInventoryNotice);
        eventManager.Subscribe<ItemSynthesized>(HandleItemSynthesized);
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
        eventManager.Unsubscribe<InventoryNotice>(HandleInventoryNotice);
        eventManager.Unsubscribe<ItemSynthesized>(HandleItemSynthesized);
        eventManager.Unsubscribe<GameStateChanged>(HandleStateChanged);
        _subscribed = false;
    }

    private static AudioSource PlaySound(string name)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return null;
        }

        return audioManager.PlaySound(name);
    }

    /// <summary>
    /// 장착 칸을 바로 팔거나 버리거나 해제한다. 디졸브로 판매 중인 장착 칸까지 빼면 최소 장착 수가 안 남을 때는 막는다.
    /// 막지 않으면 이 칸이 먼저 빠지고, 디졸브가 끝난 판매가 마지막 한 마리라서 실패한다.
    /// </summary>
    /// <summary>
    /// 장착 칸에 포켓몬이 있으면 true. 빈 칸에서 해제, 판매, 버리기를 누르면 아무 반응 없이 넘기려고 먼저 본다.
    /// </summary>
    private static bool HasEquipped(ItemManager itemManager, int playerId, int slot)
    {
        var equipment = itemManager.GetEquipment(playerId);
        return equipment != null && slot >= 0 && slot < equipment.Stacks.Count && !equipment.Stacks[slot].Empty;
    }

    private bool TryRemoveEquipped(ItemManager itemManager, int playerId, int slot, System.Func<int, int, bool> remove)
    {
        if (!_sellDirector.CanSellEquipped(itemManager, playerId))
        {
            ShowNotice(ItemManager.MIN_EQUIPPED_MESSAGE);
            return false;
        }

        return remove(playerId, slot);
    }

    /// <summary>
    /// 성공하면 success, 실패하면 failure 효과음을 낸다. null이면 그쪽은 소리를 내지 않는다.
    /// </summary>
    private static void PlaySoundIf(bool succeeded, string success, string failure)
    {
        var name = succeeded ? success : failure;
        if (name != null)
        {
            PlaySound(name);
        }
    }

    /// <summary>
    /// 재생 중인 효과음이 끝날 때까지 걸리는 실제 시간. 재생되지 않았으면 0.
    /// </summary>
    private static float GetLength(AudioSource source)
    {
        if (source == null || source.clip == null || Mathf.Approximately(source.pitch, 0f))
        {
            return 0f;
        }

        return source.clip.length / Mathf.Abs(source.pitch);
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
