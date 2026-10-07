using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 타이틀 스토리지에서 플레이어블 캐릭터를 고르고, 고른 뒤에는 포켓몬을 본다.
/// </summary>
public class UIStorageWindow : MonoBehaviour
{
    private enum StorageFocus
    {
        Characters,
        FilterBoard,
        Generation,
        Side,
    }

    private sealed class SideItem
    {
        public FilterOptionView View;
        public Button Button;
        public bool IsCharacter;
        public int MonsterIndex = -1;
    }

    private const string MENU_MOVE_SOUND = "cursor";
    private const string FILTER_APPLY_SOUND = "select";
    private const string ENTRY_LOCKED_SOUND = "error";
    private const string STORAGE_MUSIC_NAME = "storage";
    private const float SCROLL_FOLLOW_DURATION = 0.1f;
    // 포커스 테두리가 칸 밖으로 나오는 만큼 뷰포트 가장자리에서 띄운다.
    private const float SCROLL_FOLLOW_MARGIN = 10f;

    [SerializeField] private Transform _trainerContent;
    [SerializeField] private StorageCharacterView _slotPrefab;
    [SerializeField] private GridLayoutGroup _grid;
    [SerializeField] private GameObject _choose;
    [SerializeField] private Image _chooseImage;
    [SerializeField] private GameObject _notChosen;
    [SerializeField] private UIIntroScene _introScene;
    [SerializeField] private StorageInfoView _info;
    [SerializeField] private FilterOptionView _generationCategory;
    [SerializeField] private FilterGenerationView _generationFilter;
    [SerializeField] private GameObject _trainerScroll;
    [SerializeField] private GameObject _monsterScroll;
    [SerializeField] private Transform _monsterContent;
    [SerializeField] private StorageMonsterView _monsterSlotPrefab;
    [SerializeField] private GridLayoutGroup _monsterGrid;
    [SerializeField] private Image[] _entryMonsters = System.Array.Empty<Image>();
    [SerializeField] private GameObject[] _entryLocks = System.Array.Empty<GameObject>();
    [SerializeField] private Button _trainingButton;
    [SerializeField] private UITrainingWindow _trainingWindow;
    [SerializeField] private GameObject _entryCharacter;
    [SerializeField] private Button _logoutButton;
    [SerializeField] private Button _gameStartButton;
    [SerializeField] private TMP_Text _goldText;

    private readonly List<StorageCharacterView> _slots = new List<StorageCharacterView>();
    private readonly List<StorageMonsterView> _monsterSlots = new List<StorageMonsterView>();
    private readonly List<SideItem> _sideItems = new List<SideItem>();
    private StorageFocus _mode = StorageFocus.Characters;
    private PlayableCharacterData _focusedCharacter;
    private PlayableCharacterData _entryCharacterData;
    private MonsterVisualData _focusedMonster;
    private MonsterVisualData[] _entryVisuals = System.Array.Empty<MonsterVisualData>();
    private Sprite[] _entryDefaultSprites = System.Array.Empty<Sprite>();
    private Color[] _entryDefaultColors = System.Array.Empty<Color>();
    private bool _showingMonsters;
    private int _focusIndex;
    private int _sideIndex = -1;
    private bool _goldSubscribed;
    private ScrollRect _trainerScrollRect;
    private ScrollRect _monsterScrollRect;
    private Tween _scrollTween;
    // 키보드로 스크롤하면 가만히 있는 커서 아래로 칸이 지나가며 호버가 포커스를 빼앗는다. 커서가 움직일 때까지 호버를 막는다.
    private bool _hoverLocked;
    private Vector2 _hoverLockPointer;

    public bool IsOpen => isActiveAndEnabled;

    /// <summary>
    /// 스토리지 창을 연다. 꺼져 있으면 켜면서 칸을 다시 채운다.
    /// </summary>
    public void Open()
    {
        PlayStorageMusic();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        PrepareFilters();
        Rebuild(false);
        RefreshGold();
    }

    /// <summary>
    /// 잠기지 않은 엔트리 칸 중 채워진 첫 포켓몬을 돌려준다.
    /// </summary>
    public bool TryGetFirstEntryMonster(out MonsterVisualData data)
    {
        EnsureEntryState();
        for (var i = 0; i < _entryVisuals.Length; i++)
        {
            if (IsEntryLocked(i) || _entryVisuals[i] == null)
            {
                continue;
            }

            data = _entryVisuals[i];
            return true;
        }

        data = null;
        return false;
    }

    private void OnEnable()
    {
        PrepareFilters();
        Rebuild(false);
        if (_trainingButton != null)
        {
            _trainingButton.onClick.AddListener(OpenTraining);
        }

        SubscribeGold();
        RefreshGold();
    }

    private void OnDisable()
    {
        if (_trainingButton != null)
        {
            _trainingButton.onClick.RemoveListener(OpenTraining);
        }

        UnsubscribeGold();
        StopScrollFollow();
    }

    private void Update()
    {
        if (_introScene != null && _introScene.IsOpen)
        {
            return;
        }

        if (_trainingWindow != null && _trainingWindow.IsOpen)
        {
            return;
        }

        if (!TryGetInput(out var inputManager))
        {
            return;
        }

        if (inputManager.ConsumeStorageFilter() && (_mode == StorageFocus.Characters || _mode == StorageFocus.Side))
        {
            FocusFilterBoard(true);
        }

        var move = inputManager.ConsumeMenuMoveRepeat(out var repeated);
        if (move != Vector2Int.zero)
        {
            HandleMove(move, repeated);
        }

        if (inputManager.ConsumeMenuSubmit())
        {
            HandleSubmit();
        }

        if (inputManager.ConsumeMenuCancel())
        {
            HandleCancel();
        }
    }

    /// <summary>
    /// 트레이닝 창을 연다. 스토리지 목록은 그대로 둔다.
    /// </summary>
    private void OpenTraining()
    {
        if (_introScene != null && _introScene.IsOpen)
        {
            return;
        }

        if (_trainingWindow == null)
        {
            Debug.LogError("트레이닝 창이 없습니다.");
            return;
        }

        _trainingWindow.Open();
    }

    private void PrepareFilters()
    {
        _mode = StorageFocus.Characters;
        _focusedCharacter = null;
        _focusedMonster = null;
        _showingMonsters = false;
        ClearMonsterEntries();
        SetScrolls();
        PrepareSideItems();
        if (_generationCategory == null || _generationFilter == null)
        {
            Debug.LogError("스토리지 필터 참조가 없습니다.");
            return;
        }

        _generationCategory.Bind(OnCategoryFocus, OnCategoryConfirm);
        _generationCategory.SetFocused(false);
        _generationFilter.Bind(OnGenerationFocus, OnGenerationConfirm);
        _generationFilter.ResetFilter();
        _generationFilter.Close();
    }

    /// <summary>
    /// 방향키를 처리한다. 누르고 있어서 반복된 입력은 칸 목록과 세대 목록 안에서만 움직이고 다른 메뉴로 넘어가지 않는다.
    /// </summary>
    private void HandleMove(Vector2Int move, bool repeated)
    {
        if (repeated && _mode != StorageFocus.Characters && _mode != StorageFocus.Generation)
        {
            return;
        }

        if (_mode == StorageFocus.FilterBoard)
        {
            if (move.y < 0)
            {
                ReturnToCharacters(true);
            }
            else if (move.x > 0)
            {
                FocusSide(NextSideIndex(-1, 1), true);
            }

            return;
        }

        if (_mode == StorageFocus.Side)
        {
            MoveSide(move);
            return;
        }

        if (_mode == StorageFocus.Generation)
        {
            if (move.y == 0 || _generationFilter == null)
            {
                return;
            }

            var direction = move.y > 0 ? -1 : 1;
            if (_generationFilter.Move(direction))
            {
                PlayCursor();
            }

            return;
        }

        if (move.y > 0 && IsTopRow())
        {
            if (!repeated)
            {
                FocusFilterBoard(true);
            }

            return;
        }

        if (move.x > 0 && IsRowEnd() && (repeated || FocusSide(NearestSideIndex(), true)))
        {
            return;
        }

        MoveFocus(move);
    }

    private void HandleSubmit()
    {
        if (_mode == StorageFocus.FilterBoard)
        {
            OpenGeneration();
            return;
        }

        if (_mode == StorageFocus.Generation)
        {
            ApplyGenerationFilter();
            return;
        }

        if (_mode == StorageFocus.Side)
        {
            SubmitSide();
            return;
        }

        ConfirmFocused();
    }

    private void HandleCancel()
    {
        if (_mode == StorageFocus.Generation)
        {
            CloseGeneration();
            return;
        }

        if (_mode == StorageFocus.FilterBoard || _mode == StorageFocus.Side)
        {
            ReturnToCharacters(true);
            return;
        }

        if (_showingMonsters && ClearLastMonsterEntry())
        {
            return;
        }

        ClearEntry();
    }

    private void OnCategoryFocus(FilterOptionView option)
    {
        if (_mode != StorageFocus.Characters && _mode != StorageFocus.Side)
        {
            return;
        }

        FocusFilterBoard(true);
    }

    private void OnCategoryConfirm(FilterOptionView option)
    {
        if (_mode == StorageFocus.Characters || _mode == StorageFocus.Side)
        {
            FocusFilterBoard(false);
        }

        OpenGeneration();
    }

    private void OnGenerationFocus(FilterOptionView option)
    {
        if (_mode != StorageFocus.Generation || _generationFilter == null)
        {
            return;
        }

        if (_generationFilter.FocusOption(option))
        {
            PlayCursor();
        }
    }

    private void OnGenerationConfirm(FilterOptionView option)
    {
        if (_mode != StorageFocus.Generation || _generationFilter == null)
        {
            return;
        }

        _generationFilter.ConfirmOption(option);
        PlaySelect();
        Rebuild(true);
    }

    private void FocusFilterBoard(bool playCursor)
    {
        if (_generationCategory == null)
        {
            return;
        }

        if (_mode == StorageFocus.Generation && _generationFilter != null)
        {
            _generationFilter.Close();
        }

        var entered = _mode != StorageFocus.FilterBoard;
        _mode = StorageFocus.FilterBoard;
        ClearSlotSelect();
        ClearSideSelect();
        _generationCategory.SetFocused(true);
        if (playCursor && entered)
        {
            PlayCursor();
        }
    }

    private void OpenGeneration()
    {
        if (_generationFilter == null || _generationCategory == null)
        {
            return;
        }

        _mode = StorageFocus.Generation;
        _generationCategory.SetFocused(true);
        _generationFilter.Open();
        PlaySelect();
    }

    private void CloseGeneration()
    {
        if (_generationFilter != null)
        {
            _generationFilter.Close();
        }

        _mode = StorageFocus.FilterBoard;
        if (_generationCategory != null)
        {
            _generationCategory.SetFocused(true);
        }
    }

    private void ApplyGenerationFilter()
    {
        if (_generationFilter == null)
        {
            return;
        }

        _generationFilter.ApplyFocused();
        PlaySelect();
        Rebuild(true);
    }

    private void ReturnToCharacters(bool playCursor)
    {
        if (_generationFilter != null)
        {
            _generationFilter.Close();
        }

        if (_generationCategory != null)
        {
            _generationCategory.SetFocused(false);
        }

        _mode = StorageFocus.Characters;
        ClearSideSelect();
        SetFocus(_focusIndex, false);
        if (playCursor)
        {
            PlayCursor();
            ScrollToFocus();
        }
    }

    private void ClearSlotSelect()
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            _slots[i].SetFocused(false);
        }

        for (var i = 0; i < _monsterSlots.Count; i++)
        {
            _monsterSlots[i].SetFocused(false);
        }
    }

    private bool IsTopRow()
    {
        return ActiveSlotCount > 0 && _focusIndex < ColumnCount();
    }

    private bool IsRowEnd()
    {
        var count = ActiveSlotCount;
        if (count == 0)
        {
            return true;
        }

        Canvas.ForceUpdateCanvases();
        var columns = ColumnCount();
        return _focusIndex % columns == columns - 1 || _focusIndex >= count - 1;
    }

    private void PrepareSideItems()
    {
        _sideItems.Clear();
        _sideIndex = -1;
        AddSideItem(_trainingButton != null ? _trainingButton.gameObject : null, _trainingButton, false, -1);
        AddSideItem(_entryCharacter, null, true, -1);
        for (var i = 0; i < _entryMonsters.Length; i++)
        {
            AddSideItem(_entryMonsters[i] != null ? _entryMonsters[i].gameObject : null, null, false, i);
        }

        AddSideItem(_logoutButton != null ? _logoutButton.gameObject: null, _logoutButton, false, -1);

        AddSideItem(_gameStartButton != null ? _gameStartButton.gameObject : null, _gameStartButton, false, -1);
    }

    private void AddSideItem(GameObject target, Button button, bool isCharacter, int monsterIndex)
    {
        if (target == null)
        {
            return;
        }

        var view = target.GetComponent<FilterOptionView>();
        if (view == null)
        {
            view = target.AddComponent<FilterOptionView>();
        }

        // 그림이 자식에만 있는 칸도 마우스를 받도록 투명 이미지를 깐다.
        var graphic = target.GetComponent<Graphic>();
        if (graphic == null)
        {
            var image = target.AddComponent<Image>();
            image.color = Color.clear;
            graphic = image;
        }

        graphic.raycastTarget = true;
        var isEntry = isCharacter || monsterIndex >= 0;
        // 버튼 클릭은 Button.onClick이 처리하므로 확정 콜백은 포커스만 옮긴다.
        view.Bind(OnSideFocus, OnSideFocus, isEntry ? OnSideRelease : null);
        _sideItems.Add(new SideItem
        {
            View = view,
            Button = button,
            IsCharacter = isCharacter,
            MonsterIndex = monsterIndex,
        });
    }

    private int IndexOfSide(FilterOptionView view)
    {
        for (var i = 0; i < _sideItems.Count; i++)
        {
            if (_sideItems[i].View == view)
            {
                return i;
            }
        }

        return -1;
    }

    // 엔트리 칸은 채워져 있을 때만 포커스할 수 있다. 잠긴 칸은 포커스하지 않는다.
    private bool IsSideAvailable(int index)
    {
        if (index < 0 || index >= _sideItems.Count || !_sideItems[index].View.isActiveAndEnabled)
        {
            return false;
        }

        var item = _sideItems[index];
        if (item.IsCharacter)
        {
            return _entryCharacterData != null;
        }

        if (item.MonsterIndex >= 0)
        {
            var monster = item.MonsterIndex;
            return !IsEntryLocked(monster) && monster < _entryVisuals.Length && _entryVisuals[monster] != null;
        }

        return true;
    }

    // 포커스된 엔트리 칸이 비면 아래, 없으면 위의 항목으로 옮기고, 둘 다 없으면 스토리지로 돌아간다.
    private void RefreshSideFocus()
    {
        if (_mode != StorageFocus.Side || IsSideAvailable(_sideIndex))
        {
            return;
        }

        var next = NextSideIndex(_sideIndex, 1);
        if (next < 0)
        {
            next = NextSideIndex(_sideIndex, -1);
        }

        if (next >= 0)
        {
            FocusSide(next, false);
            return;
        }

        ReturnToCharacters(false);
    }

    private void OnSideFocus(FilterOptionView view)
    {
        if (_mode == StorageFocus.Generation)
        {
            return;
        }

        FocusSide(IndexOfSide(view), true);
    }

    private void OnSideRelease(FilterOptionView view)
    {
        if (_mode == StorageFocus.Generation)
        {
            return;
        }

        var index = IndexOfSide(view);
        if (!FocusSide(index, true))
        {
            return;
        }

        var item = _sideItems[index];
        if (item.IsCharacter)
        {
            ClearEntry();
        }
        else
        {
            ReleaseMonsterEntry(item.MonsterIndex);
        }

        RefreshSideFocus();
    }

    private bool FocusSide(int index, bool playCursor)
    {
        if (!IsSideAvailable(index))
        {
            return false;
        }

        if (_generationCategory != null)
        {
            _generationCategory.SetFocused(false);
        }

        var changed = _mode != StorageFocus.Side || index != _sideIndex;
        _mode = StorageFocus.Side;
        _sideIndex = index;
        ClearSlotSelect();
        for (var i = 0; i < _sideItems.Count; i++)
        {
            _sideItems[i].View.SetFocused(i == _sideIndex);
        }

        if (playCursor && changed)
        {
            PlayCursor();
        }

        return true;
    }

    private void MoveSide(Vector2Int move)
    {
        if (move.x < 0)
        {
            ReturnToCharacters(true);
            return;
        }

        if (move.y == 0)
        {
            return;
        }

        var next = NextSideIndex(_sideIndex, move.y > 0 ? -1 : 1);
        if (next >= 0)
        {
            FocusSide(next, true);
        }
        else if (move.y > 0)
        {
            FocusFilterBoard(true);
        }
    }

    private int NextSideIndex(int from, int direction)
    {
        for (var i = from + direction; i >= 0 && i < _sideItems.Count; i += direction)
        {
            if (IsSideAvailable(i))
            {
                return i;
            }
        }

        return -1;
    }

    // 포커스된 칸과 높이가 가장 가까운 오른쪽 항목을 고른다.
    private int NearestSideIndex()
    {
        var slot = ActiveSlotRect(_focusIndex);
        if (slot == null)
        {
            return NextSideIndex(-1, 1);
        }

        var slotY = slot.TransformPoint(slot.rect.center).y;
        var best = -1;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < _sideItems.Count; i++)
        {
            if (!IsSideAvailable(i))
            {
                continue;
            }

            var rect = (RectTransform)_sideItems[i].View.transform;
            var distance = Mathf.Abs(rect.TransformPoint(rect.rect.center).y - slotY);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    private void SubmitSide()
    {
        if (!IsSideAvailable(_sideIndex))
        {
            return;
        }

        var button = _sideItems[_sideIndex].Button;
        if (button != null && button.IsInteractable())
        {
            button.onClick.Invoke();
        }
    }

    private void ClearSideSelect()
    {
        _sideIndex = -1;
        for (var i = 0; i < _sideItems.Count; i++)
        {
            _sideItems[i].View.SetFocused(false);
        }
    }

    private bool PassesGeneration(int generation)
    {
        if (_generationFilter == null || _generationFilter.AppliedGeneration == FilterGenerationView.ALL_GENERATION)
        {
            return true;
        }

        return generation == _generationFilter.AppliedGeneration;
    }

    private void Rebuild(bool keepEntry)
    {
        if (_showingMonsters)
        {
            RebuildMonsters();
            return;
        }

        RebuildCharacters(keepEntry);
    }

    private void RebuildCharacters(bool keepEntry)
    {
        var previousFocus = _focusedCharacter;
        ClearContent();
        if (_slotPrefab == null || _trainerContent == null)
        {
            Debug.LogError("트레이너 스토리지 슬롯 참조가 없습니다.");
            ApplyEntry(null);
            ShowInfo(null);
            return;
        }

        var account = GetAccount();
        var characters = account != null ? account.Playables : System.Array.Empty<PlayableCharacterData>();
        for (var i = 0; i < characters.Length; i++)
        {
            var character = characters[i];
            if (character == null || !PassesGeneration(character.Generation))
            {
                continue;
            }

            var slot = Instantiate(_slotPrefab, _trainerContent);
            var unlocked = account != null && account.IsPlayableUnlocked(character);
            slot.Bind(character, unlocked, FocusSlot, ConfirmSlot);
            _slots.Add(slot);
        }

        var selected = account != null ? account.ResolveSelectedPlayable() : null;
        if (previousFocus != null)
        {
            SetFocus(IndexOfCharacter(previousFocus), false);
        }
        else
        {
            FocusInitial(selected);
        }

        ApplyEntry(keepEntry ? selected : null);
        LayoutSlots();
    }

    private void ClearContent()
    {
        _slots.Clear();
        if (_trainerContent == null)
        {
            return;
        }

        for (var i = _trainerContent.childCount - 1; i >= 0; i--)
        {
            var child = _trainerContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void FocusInitial(PlayableCharacterData selected)
    {
        var index = 0;
        if (selected != null)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Data == selected)
                {
                    index = i;
                    break;
                }
            }
        }

        SetFocus(index);
    }

    private void FocusSlot(StorageCharacterView slot)
    {
        if (_showingMonsters || _mode == StorageFocus.Generation || IsHoverLocked())
        {
            return;
        }

        var fromFilter = _mode == StorageFocus.FilterBoard || _mode == StorageFocus.Side;
        if (fromFilter)
        {
            ReturnToCharacters(false);
        }

        var index = _slots.IndexOf(slot);
        if (index < 0)
        {
            return;
        }

        var sameSlot = fromFilter && index == _focusIndex;
        SetFocus(index, true);
        if (sameSlot)
        {
            PlayCursor();
        }
    }

    private void MoveFocus(Vector2Int move)
    {
        var count = ActiveSlotCount;
        if (count == 0)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var columns = ColumnCount();
        var index = _focusIndex;
        if (move.x != 0)
        {
            var rowStart = index - index % columns;
            var rowEnd = Mathf.Min(rowStart + columns, count) - 1;
            index = Mathf.Clamp(index + move.x, rowStart, rowEnd);
        }
        else if (move.y != 0)
        {
            var next = index - move.y * columns;
            if (next >= 0 && next < count)
            {
                index = next;
            }
        }

        SetFocus(index, true);
        ScrollToFocus();
    }

    /// <summary>
    /// 키보드로 포커스한 칸이 뷰포트 밖이면 딱 보일 만큼만 스크롤을 부드럽게 옮긴다.
    /// 마우스로 고른 칸은 이미 보이고, 스크롤하면 호버가 바뀌어서 부르지 않는다.
    /// </summary>
    private void ScrollToFocus()
    {
        var scroll = ActiveScrollRect;
        var slot = ActiveSlotRect(_focusIndex);
        if (scroll == null || slot == null || scroll.content == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        var content = scroll.content;
        var corners = new Vector3[4];
        slot.GetWorldCorners(corners);
        var slotBottom = viewport.InverseTransformPoint(corners[0]).y;
        var slotTop = viewport.InverseTransformPoint(corners[1]).y;
        var view = viewport.rect;

        var delta = 0f;
        if (slotTop > view.yMax - SCROLL_FOLLOW_MARGIN)
        {
            delta = slotTop - (view.yMax - SCROLL_FOLLOW_MARGIN);
        }
        else if (slotBottom < view.yMin + SCROLL_FOLLOW_MARGIN)
        {
            delta = slotBottom - (view.yMin + SCROLL_FOLLOW_MARGIN);
        }

        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        // content를 올리면(y +) 아래 칸이 보인다. 칸이 위로 벗어났으면 delta가 양수라서 content를 내린다.
        var maxY = Mathf.Max(0f, content.rect.height - view.height);
        var targetY = Mathf.Clamp(content.anchoredPosition.y - delta, 0f, maxY);

        StopScrollFollow();
        scroll.StopMovement();
        LockHoverUntilPointerMoves();
        _scrollTween = content.DOAnchorPosY(targetY, SCROLL_FOLLOW_DURATION)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void LockHoverUntilPointerMoves()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        _hoverLocked = true;
        _hoverLockPointer = mouse.position.ReadValue();
    }

    /// <summary>
    /// 키보드 스크롤 뒤 커서가 그대로면 true. 커서가 움직이거나 클릭하면 잠금을 푼다.
    /// </summary>
    private bool IsHoverLocked()
    {
        if (!_hoverLocked)
        {
            return false;
        }

        var mouse = Mouse.current;
        if (mouse == null
            || mouse.leftButton.wasReleasedThisFrame
            || (mouse.position.ReadValue() - _hoverLockPointer).sqrMagnitude > 1f)
        {
            _hoverLocked = false;
            return false;
        }

        return true;
    }

    private void StopScrollFollow()
    {
        if (_scrollTween != null)
        {
            _scrollTween.Kill();
            _scrollTween = null;
        }
    }

    private void SetFocus(int index, bool playCursor = false)
    {
        if (_showingMonsters)
        {
            SetMonsterFocus(index, playCursor);
            return;
        }

        if (_slots.Count == 0)
        {
            _focusIndex = 0;
            _focusedCharacter = null;
            ShowInfo(null);
            return;
        }

        var next = Mathf.Clamp(index, 0, _slots.Count - 1);
        var changed = next != _focusIndex;
        _focusIndex = next;
        _focusedCharacter = _slots[_focusIndex].Data;
        var showSlot = _mode == StorageFocus.Characters;
        for (var i = 0; i < _slots.Count; i++)
        {
            _slots[i].SetFocused(showSlot && i == _focusIndex);
        }

        if (playCursor && changed && showSlot)
        {
            PlayCursor();
        }

        ShowInfo(_slots[_focusIndex]);
    }

    private int IndexOfCharacter(PlayableCharacterData data)
    {
        if (data == null)
        {
            return 0;
        }

        for (var i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Data == data)
            {
                return i;
            }
        }

        return 0;
    }

    private void ShowInfo(StorageCharacterView slot)
    {
        if (_info == null)
        {
            return;
        }

        _info.Show(slot);
    }

    private static void PlayStorageMusic()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlayMusic(STORAGE_MUSIC_NAME);
    }

    private static void PlayCursor()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(MENU_MOVE_SOUND);
    }

    private static void PlaySelect()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(FILTER_APPLY_SOUND);
    }

    private static void PlayEntryLocked()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(ENTRY_LOCKED_SOUND);
    }

    private void ConfirmFocused()
    {
        if (_showingMonsters)
        {
            if (_focusIndex < 0 || _focusIndex >= _monsterSlots.Count)
            {
                return;
            }

            ConfirmMonsterSlot(_monsterSlots[_focusIndex]);
            return;
        }

        if (_focusIndex < 0 || _focusIndex >= _slots.Count)
        {
            return;
        }

        ConfirmSlot(_slots[_focusIndex]);
    }

    private void ConfirmSlot(StorageCharacterView slot)
    {
        if (_mode != StorageFocus.Characters)
        {
            return;
        }

        if (slot == null || slot.Data == null)
        {
            return;
        }

        if (!slot.IsUnlocked)
        {
            slot.PlayLockedSelect();
            return;
        }

        var account = GetAccount();
        if (account == null)
        {
            return;
        }

        account.SelectPlayable(slot.Data);
        ApplyEntry(slot.Data);
        PlaySelect();
        EnterMonsterScroll();
        if (IsEntryFull())
        {
            FocusGameStart();
        }
    }

    private void ClearEntry()
    {
        var account = GetAccount();
        if (account != null)
        {
            account.ClearSelectedPlayable();
        }

        var restoreCharacters = _showingMonsters;
        _showingMonsters = false;
        ClearMonsterEntries();
        SetScrolls();
        ApplyEntry(null);
        if (restoreCharacters)
        {
            RebuildCharacters(false);
        }
    }

    private void ApplyEntry(PlayableCharacterData data)
    {
        _entryCharacterData = data;
        var chosen = data != null;
        if (_choose != null)
        {
            _choose.SetActive(chosen);
        }

        if (chosen && _chooseImage != null)
        {
            _chooseImage.sprite = data.InGameSprite;
        }

        if (_notChosen != null)
        {
            _notChosen.SetActive(!chosen);
        }
    }

    private void LayoutSlots()
    {
        StopScrollFollow();
        var grid = ActiveGrid;
        if (grid != null)
        {
            grid.enabled = false;
        }

        var content = ActiveContent;
        var count = ActiveSlotCount;
        if (content == null || count == 0)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var padding = grid != null ? grid.padding : new RectOffset();
        var spacing = grid != null ? grid.spacing : Vector2.zero;
        var contentWidth = content.rect.width;
        var innerRight = contentWidth - padding.right;
        var x = (float)padding.left;
        var y = (float)padding.top;
        var rowHeight = 0f;
        var rowStart = 0;

        for (var i = 0; i < count; i++)
        {
            var rect = ActiveSlotRect(i);
            if (rect == null)
            {
                continue;
            }

            var width = rect.sizeDelta.x;
            var height = rect.sizeDelta.y;
            if (x > padding.left && x + width > innerRight)
            {
                AlignRowBottom(rowStart, i, y, rowHeight);
                x = padding.left;
                y += rowHeight + spacing.y;
                rowHeight = 0f;
                rowStart = i;
            }

            rect.anchoredPosition = new Vector2(x, -y);
            x += width + spacing.x;
            rowHeight = Mathf.Max(rowHeight, height);
        }

        AlignRowBottom(rowStart, count, y, rowHeight);

        var size = content.sizeDelta;
        size.y = y + rowHeight + padding.bottom;
        content.sizeDelta = size;
    }

    /// <summary>
    /// 한 줄의 칸을 줄 아랫변에 붙인다. 칸 아랫변이 발끝이라 키가 달라도 발이 한 선에 선다.
    /// </summary>
    private void AlignRowBottom(int start, int end, float rowTop, float rowHeight)
    {
        for (var i = start; i < end; i++)
        {
            var rect = ActiveSlotRect(i);
            if (rect == null)
            {
                continue;
            }

            var position = rect.anchoredPosition;
            position.y = -(rowTop + rowHeight - rect.sizeDelta.y);
            rect.anchoredPosition = position;
        }
    }

    private int ColumnCount()
    {
        if (ActiveSlotCount == 0)
        {
            return 1;
        }

        var slot = ActiveSlotRect(0);
        var content = ActiveContent;
        var grid = ActiveGrid;
        if (slot == null || content == null)
        {
            return 1;
        }

        var spacing = grid != null ? grid.spacing.x : 0f;
        var padding = grid != null ? grid.padding.left + grid.padding.right : 0;
        var stride = slot.sizeDelta.x + spacing;
        if (stride <= 0.01f)
        {
            return 1;
        }

        var inner = content.rect.width - padding + spacing;
        return Mathf.Max(1, Mathf.FloorToInt((inner + 0.001f) / stride));
    }

    private int ActiveSlotCount => _showingMonsters ? _monsterSlots.Count : _slots.Count;

    private RectTransform ActiveContent => (_showingMonsters ? _monsterContent : _trainerContent) as RectTransform;

    private GridLayoutGroup ActiveGrid => _showingMonsters ? _monsterGrid : _grid;

    private ScrollRect ActiveScrollRect => _showingMonsters
        ? GetScrollRect(_monsterScroll, ref _monsterScrollRect)
        : GetScrollRect(_trainerScroll, ref _trainerScrollRect);

    private static ScrollRect GetScrollRect(GameObject scroll, ref ScrollRect cached)
    {
        if (cached == null && scroll != null)
        {
            cached = scroll.GetComponent<ScrollRect>();
        }

        return cached;
    }

    private RectTransform ActiveSlotRect(int index)
    {
        if (_showingMonsters)
        {
            if (index < 0 || index >= _monsterSlots.Count)
            {
                return null;
            }

            return _monsterSlots[index].transform as RectTransform;
        }

        if (index < 0 || index >= _slots.Count)
        {
            return null;
        }

        return _slots[index].transform as RectTransform;
    }

    private void SetScrolls()
    {
        StopScrollFollow();
        if (_trainerScroll != null)
        {
            _trainerScroll.SetActive(!_showingMonsters);
        }

        if (_monsterScroll != null)
        {
            _monsterScroll.SetActive(_showingMonsters);
        }
    }

    private void EnterMonsterScroll()
    {
        _showingMonsters = true;
        ClearSlotSelect();
        SetScrolls();
        RebuildMonsters();
    }

    private void RebuildMonsters()
    {
        var previousFocus = _focusedMonster;
        ClearMonsterContent();
        if (_monsterSlotPrefab == null || _monsterContent == null)
        {
            Debug.LogError("포켓몬 스토리지 슬롯 참조가 없습니다.");
            ShowMonsterInfo(null);
            return;
        }

        var account = GetAccount();
        var monsters = account != null ? account.Monsters : System.Array.Empty<MonsterVisualData>();
        for (var i = 0; i < monsters.Length; i++)
        {
            var monster = monsters[i];
            if (monster == null || !monster.Startable || !PassesGeneration(monster.Generation))
            {
                continue;
            }

            var slot = Instantiate(_monsterSlotPrefab, _monsterContent);
            var unlocked = account != null && account.IsMonsterUnlocked(monster);
            slot.Bind(monster, unlocked, FocusMonsterSlot, ConfirmMonsterSlot);
            _monsterSlots.Add(slot);
        }

        SetFocus(IndexOfMonster(previousFocus), false);
        LayoutSlots();
    }

    private void ClearMonsterContent()
    {
        _monsterSlots.Clear();
        if (_monsterContent == null)
        {
            return;
        }

        for (var i = _monsterContent.childCount - 1; i >= 0; i--)
        {
            var child = _monsterContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void FocusMonsterSlot(StorageMonsterView slot)
    {
        if (!_showingMonsters || _mode == StorageFocus.Generation || IsHoverLocked())
        {
            return;
        }

        var fromFilter = _mode == StorageFocus.FilterBoard || _mode == StorageFocus.Side;
        if (fromFilter)
        {
            ReturnToCharacters(false);
        }

        var index = _monsterSlots.IndexOf(slot);
        if (index < 0)
        {
            return;
        }

        var sameSlot = fromFilter && index == _focusIndex;
        SetFocus(index, true);
        if (sameSlot)
        {
            PlayCursor();
        }
    }

    private void SetMonsterFocus(int index, bool playCursor)
    {
        if (_monsterSlots.Count == 0)
        {
            _focusIndex = 0;
            _focusedMonster = null;
            ShowMonsterInfo(null);
            return;
        }

        var next = Mathf.Clamp(index, 0, _monsterSlots.Count - 1);
        var changed = next != _focusIndex;
        _focusIndex = next;
        _focusedMonster = _monsterSlots[_focusIndex].Data;
        var showSlot = _mode == StorageFocus.Characters;
        for (var i = 0; i < _monsterSlots.Count; i++)
        {
            _monsterSlots[i].SetFocused(showSlot && i == _focusIndex);
        }

        if (playCursor && changed && showSlot)
        {
            PlayCursor();
        }

        ShowMonsterInfo(_monsterSlots[_focusIndex]);
    }

    private int IndexOfMonster(MonsterVisualData data)
    {
        if (data == null)
        {
            return 0;
        }

        for (var i = 0; i < _monsterSlots.Count; i++)
        {
            if (_monsterSlots[i].Data == data)
            {
                return i;
            }
        }

        return 0;
    }

    private void ShowMonsterInfo(StorageMonsterView slot)
    {
        if (_info == null)
        {
            return;
        }

        _info.Show(slot);
    }

    private void ConfirmMonsterSlot(StorageMonsterView slot)
    {
        if (!_showingMonsters || _mode != StorageFocus.Characters)
        {
            return;
        }

        if (slot == null || slot.Data == null)
        {
            return;
        }

        if (!slot.IsUnlocked)
        {
            slot.PlayLockedSelect();
            return;
        }

        TryFillEntry(slot.Data);
    }

    private void EnsureEntryState()
    {
        var count = _entryMonsters != null ? _entryMonsters.Length : 0;
        if (_entryVisuals.Length == count
            && _entryDefaultSprites.Length == count
            && _entryDefaultColors.Length == count)
        {
            ShowEntryLocks();
            return;
        }

        _entryVisuals = new MonsterVisualData[count];
        _entryDefaultSprites = new Sprite[count];
        _entryDefaultColors = new Color[count];
        for (var i = 0; i < count; i++)
        {
            var image = _entryMonsters[i];
            _entryDefaultSprites[i] = image != null ? image.sprite : null;
            _entryDefaultColors[i] = image != null ? image.color : Color.white;
        }

        ShowEntryLocks();
    }

    private void ShowEntryLocks()
    {
        if (_entryLocks == null)
        {
            return;
        }

        for (var i = 0; i < _entryLocks.Length; i++)
        {
            if (_entryLocks[i] != null)
            {
                _entryLocks[i].SetActive(true);
            }
        }
    }

    private bool IsEntryLocked(int index)
    {
        return _entryLocks != null
            && index >= 0
            && index < _entryLocks.Length
            && _entryLocks[index] != null;
    }

    private void ClearMonsterEntries()
    {
        EnsureEntryState();
        for (var i = 0; i < _entryVisuals.Length; i++)
        {
            _entryVisuals[i] = null;
            ApplyEntrySprite(i, null);
        }
    }

    private bool ClearLastMonsterEntry()
    {
        EnsureEntryState();
        for (var i = _entryVisuals.Length - 1; i >= 0; i--)
        {
            if (IsEntryLocked(i) || _entryVisuals[i] == null)
            {
                continue;
            }

            _entryVisuals[i] = null;
            ApplyEntrySprite(i, null);
            return true;
        }

        return false;
    }

    private void ReleaseMonsterEntry(int index)
    {
        EnsureEntryState();
        if (IsEntryLocked(index) || index < 0 || index >= _entryVisuals.Length || _entryVisuals[index] == null)
        {
            return;
        }

        _entryVisuals[index] = null;
        ApplyEntrySprite(index, null);
    }

    private void TryFillEntry(MonsterVisualData data)
    {
        EnsureEntryState();
        for (var i = 0; i < _entryVisuals.Length; i++)
        {
            if (IsEntryLocked(i) || _entryVisuals[i] != null)
            {
                continue;
            }

            _entryVisuals[i] = data;
            ApplyEntrySprite(i, data != null ? MonsterVisualData.FirstFrame(data.Icon) : null);
            PlaySelect();
            if (IsEntryFull())
            {
                FocusGameStart();
            }

            return;
        }

        PlayEntryLocked();
    }

    // 트레이너가 골라져 있고 잠기지 않은 엔트리 칸이 모두 찼는지 본다.
    private bool IsEntryFull()
    {
        if (_entryCharacterData == null)
        {
            return false;
        }

        EnsureEntryState();
        for (var i = 0; i < _entryVisuals.Length; i++)
        {
            if (!IsEntryLocked(i) && _entryVisuals[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    // 확정 소리가 이미 나므로 커서 소리는 내지 않는다.
    private void FocusGameStart()
    {
        if (_gameStartButton == null)
        {
            return;
        }

        for (var i = 0; i < _sideItems.Count; i++)
        {
            if (_sideItems[i].Button == _gameStartButton)
            {
                FocusSide(i, false);
                return;
            }
        }
    }

    private void ApplyEntrySprite(int index, Sprite portrait)
    {
        if (IsEntryLocked(index) || _entryMonsters == null || index < 0 || index >= _entryMonsters.Length)
        {
            return;
        }

        var image = _entryMonsters[index];
        if (image == null)
        {
            return;
        }

        var filled = portrait != null;
        image.sprite = filled ? portrait : _entryDefaultSprites[index];
        image.preserveAspect = filled;
        image.color = filled ? Color.white : _entryDefaultColors[index];
    }

    private static bool TryGetInput(out InputManager inputManager)
    {
        inputManager = null;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<InputManager>(out inputManager))
        {
            return false;
        }

        return inputManager != null;
    }

    private void SubscribeGold()
    {
        if (_goldSubscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            return;
        }

        eventManager.Subscribe<CurrencyAmountChanged>(HandleGoldChanged);
        _goldSubscribed = true;
    }

    private void UnsubscribeGold()
    {
        if (!_goldSubscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            _goldSubscribed = false;
            return;
        }

        eventManager.Unsubscribe<CurrencyAmountChanged>(HandleGoldChanged);
        _goldSubscribed = false;
    }

    private void HandleGoldChanged(CurrencyAmountChanged changed)
    {
        if (!changed.IsMetaBalance || changed.CurrencyId != CurrenciesManager.POCKET_DOLLAR_ID || changed.PlayerId != ResolvePlayerId())
        {
            return;
        }

        SetGoldText(changed.Amount);
    }

    private void RefreshGold()
    {
        var amount = 0;
        if (Managers.Instance != null && Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            var save = currencies.GetCurrency(ResolvePlayerId(), CurrenciesManager.POCKET_DOLLAR_ID, true);
            if (save != null)
            {
                amount = save.Amount;
            }
        }

        SetGoldText(amount);
    }

    private static int ResolvePlayerId()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return playerManager.LocalPlayerId;
        }

        return 1;
    }

    private void SetGoldText(int amount)
    {
        if (_goldText != null)
        {
            _goldText.text = amount.ToString();
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용 임시 버튼. 보관함이 열려 있을 때만 오른쪽 위에 뜬다. 정식 UI가 생기면 지운다.
    private static readonly Rect TEST_UNLOCK_BUTTON_RECT = new Rect(-220f, 10f, 210f, 40f);

    private void OnGUI()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            return;
        }

        var rect = TEST_UNLOCK_BUTTON_RECT;
        rect.x += Screen.width;
        var label = account.UnlockAllForTest ? "[테스트] 전체 해금 끄기" : "[테스트] 전체 해금";
        if (!GUI.Button(rect, label))
        {
            return;
        }

        account.SetUnlockAllForTest(!account.UnlockAllForTest);
        Rebuild(false);
    }
#endif

    private static AccountManager GetAccount()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            Debug.LogError("AccountManager를 찾을 수 없습니다.");
            return null;
        }

        return account;
    }
}
