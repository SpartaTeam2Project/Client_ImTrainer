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
public class InventoryUi : MonoBehaviour
{
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
    private const string STAR_UP_SOUND = "inventory_star_up";
    private const string EVOLUTION_START_SOUND = "inventory_star_evolution_start";
    private const string EVOLUTION_END_SOUND = "inventory_star_evolution_end";

    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _panelGroup;
    [SerializeField] private ScreenBackdrop _backdrop;
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

    [Header("Drag")]
    [SerializeField] private InventoryDropZone _trashZone;
    [SerializeField] private Vector2 _dragGhostSize = new Vector2(96f, 96f);

    private readonly List<InventoryItem> _bagSlots = new List<InventoryItem>();
    private Image _dragGhost;
    private InventorySlotDrag.SlotKind _dragKind = InventorySlotDrag.SlotKind.None;
    private int _dragIndex = -1;
    private int _dragUid = -1;
    private int _dragStar;
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

        // 창이 열려 있으면 ESC를 먼저 가져가서 일시정지 대신 창을 닫는다.
        if (_open && inputManager.ConsumePausePressed())
        {
            Close();
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
        EndDrag();
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
            var item = offer != null && !offer.Sold ? itemManager.CreateOfferItem(offer) : null;
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
        if (holder == null || index < 0 || index >= holder.Stacks.Count || holder.Stacks[index].Empty || holder.Stacks[index].Item == null)
        {
            return false;
        }

        var item = holder.Stacks[index].Item;
        _dragKind = kind;
        _dragIndex = index;
        _dragUid = item.uid;
        _dragStar = item.upgradeLevel;
        if (kind == InventorySlotDrag.SlotKind.Bag)
        {
            SelectBag(index);
        }
        else
        {
            SelectEquipment(index);
        }

        ShowDragGhost(itemManager, item.uid);
        ShowMergeHints(itemManager, playerId);
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
        _dragKind = InventorySlotDrag.SlotKind.None;
        _dragIndex = -1;
        _dragUid = -1;
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
        if (_dragKind == InventorySlotDrag.SlotKind.None || !TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        if (_dragKind == InventorySlotDrag.SlotKind.Bag)
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
        if (_dragKind == InventorySlotDrag.SlotKind.None || !TryGetContext(out var itemManager, out var playerId))
        {
            return;
        }

        if (zone == InventoryDropZone.ZoneKind.Trash)
        {
            if (_dragKind == InventorySlotDrag.SlotKind.Bag)
            {
                var bagIndex = ResolveDragBagIndex(itemManager, playerId);
                if (bagIndex >= 0)
                {
                    itemManager.TrySell(playerId, bagIndex);
                }
            }
            else
            {
                itemManager.TrySellEquipped(playerId, _dragIndex);
            }
        }
        else if (zone == InventoryDropZone.ZoneKind.Bag && _dragKind == InventorySlotDrag.SlotKind.Equipment)
        {
            itemManager.TryUnequip(playerId, _dragIndex);
        }

        EndDrag();
        Refresh();
    }

    private void DropBag(ItemManager itemManager, int playerId, InventorySlotDrag.SlotKind targetKind, int targetIndex)
    {
        var bagIndex = ResolveDragBagIndex(itemManager, playerId);
        if (bagIndex < 0)
        {
            return;
        }

        if (targetKind == InventorySlotDrag.SlotKind.Equipment)
        {
            if (IsSameAsDragged(itemManager.GetEquipment(playerId), targetIndex))
            {
                itemManager.TrySynthesize(playerId, _dragUid, _dragStar, targetIndex, -1);
                return;
            }

            itemManager.TryEquipToSlot(playerId, bagIndex, targetIndex);
            return;
        }

        // 가방은 자동 정렬이라 자리만 옮기는 동작은 없다. 같은 종, 같은 성 칸에 놓을 때만 합성한다.
        if (targetKind != InventorySlotDrag.SlotKind.Bag || targetIndex == _dragIndex)
        {
            return;
        }

        if (IsSameAsDragged(itemManager.GetInventory(playerId), targetIndex))
        {
            itemManager.TrySynthesize(playerId, _dragUid, _dragStar);
        }
    }

    private void DropEquipment(ItemManager itemManager, int playerId, InventorySlotDrag.SlotKind targetKind, int targetIndex)
    {
        if (targetKind == InventorySlotDrag.SlotKind.Equipment)
        {
            if (targetIndex == _dragIndex)
            {
                return;
            }

            if (IsSameAsDragged(itemManager.GetEquipment(playerId), targetIndex))
            {
                itemManager.TrySynthesize(playerId, _dragUid, _dragStar, targetIndex, _dragIndex);
                return;
            }

            itemManager.TrySwapEquipped(playerId, _dragIndex, targetIndex);
            return;
        }

        if (targetKind != InventorySlotDrag.SlotKind.Bag)
        {
            return;
        }

        // 같은 종, 같은 성 가방 칸에 놓으면 합성하고 결과는 끈 장착 칸에 남긴다. 그 밖에는 해제한다.
        if (IsSameAsDragged(itemManager.GetInventory(playerId), targetIndex))
        {
            itemManager.TrySynthesize(playerId, _dragUid, _dragStar, -1, _dragIndex);
            return;
        }

        itemManager.TryUnequip(playerId, _dragIndex);
    }

    private bool IsSameAsDragged(InventoryHolder holder, int index)
    {
        return holder != null && index >= 0 && index < holder.Stacks.Count && holder.Stacks[index].isSameItem(_dragUid, _dragStar);
    }

    /// <summary>
    /// 가방은 바뀔 때마다 정렬돼서 끌기 시작한 칸 번호 대신 종과 성으로 다시 찾는다.
    /// </summary>
    private int ResolveDragBagIndex(ItemManager itemManager, int playerId)
    {
        var bag = itemManager.GetInventory(playerId);
        if (bag == null)
        {
            return -1;
        }

        if (_dragIndex >= 0 && _dragIndex < bag.Stacks.Count && bag.Stacks[_dragIndex].isSameItem(_dragUid, _dragStar))
        {
            return _dragIndex;
        }

        return bag.FindIndex(_dragUid, _dragStar);
    }

    /// <summary>
    /// 가방과 장착에서 끌고 있는 포켓몬과 같은 종, 같은 성인 칸 테두리를 깜빡인다.
    /// </summary>
    private void ShowMergeHints(ItemManager itemManager, int playerId)
    {
        var bag = itemManager.GetInventory(playerId);
        var equipment = itemManager.GetEquipment(playerId);
        var bagExcept = _dragKind == InventorySlotDrag.SlotKind.Bag ? _dragIndex : -1;
        for (var i = 0; i < _bagSlots.Count; i++)
        {
            _bagSlots[i].SetMergeHint(i != bagExcept && IsSameAsDragged(bag, i));
        }

        if (_equipmentUi != null)
        {
            var equipmentExcept = _dragKind == InventorySlotDrag.SlotKind.Equipment ? _dragIndex : -1;
            _equipmentUi.SetMergeHints(equipment, _dragUid, _dragStar, equipmentExcept);
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

        PlaySound(EVOLUTION_START_SOUND);
        if (slot != null)
        {
            slot.PlayEvolution(GetPortrait(itemManager, synthesized.PreviousUid), GetPortrait(itemManager, synthesized.Uid),
                () => PlaySound(EVOLUTION_END_SOUND));
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
        eventManager.Unsubscribe<ItemSynthesized>(HandleItemSynthesized);
        eventManager.Unsubscribe<GameStateChanged>(HandleStateChanged);
        _subscribed = false;
    }

    private static void PlaySound(string name)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(name);
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
