using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 스토리지에서 플레이어블 캐릭터를 고르고, 고른 뒤에는 포켓몬을 본다.
/// 입력과 포커스 모드 전환을 맡고, 칸 목록, 엔트리, 오른쪽 메뉴, 세대 필터, 골드 표시는 Storage* 클래스에 맡긴다.
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

    private StorageSlotList<StorageCharacterView, PlayableCharacterData> _characters;
    private StorageSlotList<StorageMonsterView, MonsterVisualData> _monsters;
    private StorageEntrySlots _entry;
    private StorageSideMenu _side;
    private StorageGenerationFilter _filter;
    private StorageGoldLabel _gold;
    private readonly StorageScrollFollow _scrollFollow = new StorageScrollFollow();
    private StorageFocus _mode = StorageFocus.Characters;
    private bool _showingMonsters;

    public bool IsOpen => isActiveAndEnabled;

    // 창이 한 번도 켜지지 않았어도 엔트리를 물어볼 수 있어서 처음 쓸 때 만든다.
    private StorageEntrySlots Entry
    {
        get
        {
            if (_entry == null)
            {
                _entry = new StorageEntrySlots(_entryMonsters, _entryLocks, _choose, _chooseImage, _notChosen);
            }

            return _entry;
        }
    }

    private IStorageSlotList ActiveList => _showingMonsters ? (IStorageSlotList)_monsters : _characters;

    /// <summary>
    /// 스토리지 창을 연다. 꺼져 있으면 켜면서 칸을 다시 채운다.
    /// </summary>
    public void Open()
    {
        StorageSounds.PlayMusic();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        PrepareFilters();
        Rebuild(false);
        _gold.Refresh();
    }

    /// <summary>
    /// 잠기지 않은 엔트리 칸 중 채워진 첫 포켓몬을 돌려준다.
    /// </summary>
    public bool TryGetFirstEntryMonster(out MonsterVisualData data)
    {
        return Entry.TryGetFirstMonster(out data);
    }

    private void Awake()
    {
        _characters = new StorageSlotList<StorageCharacterView, PlayableCharacterData>(
            _slotPrefab, _trainerContent, _grid, _trainerScroll, "트레이너 스토리지 슬롯 참조가 없습니다.",
            FocusSlot, ConfirmSlot, ShowInfo);
        _monsters = new StorageSlotList<StorageMonsterView, MonsterVisualData>(
            _monsterSlotPrefab, _monsterContent, _monsterGrid, _monsterScroll, "포켓몬 스토리지 슬롯 참조가 없습니다.",
            FocusMonsterSlot, ConfirmMonsterSlot, ShowInfo);
        _side = new StorageSideMenu(
            _trainingButton, _entryCharacter, _entryMonsters, _logoutButton, _gameStartButton, Entry,
            OnSideFocus, OnSideRelease);
        _filter = new StorageGenerationFilter(
            _generationCategory, _generationFilter,
            OnCategoryFocus, OnCategoryConfirm, OnGenerationFocus, OnGenerationConfirm);
        _gold = new StorageGoldLabel(_goldText);
    }

    private void OnEnable()
    {
        PrepareFilters();
        Rebuild(false);
        if (_trainingButton != null)
        {
            _trainingButton.onClick.AddListener(OpenTraining);
        }

        _gold.Subscribe();
        _gold.Refresh();
    }

    private void OnDisable()
    {
        if (_trainingButton != null)
        {
            _trainingButton.onClick.RemoveListener(OpenTraining);
        }

        _gold.Unsubscribe();
    }

    private void Update()
    {
        Entry.TickBob(Time.unscaledDeltaTime);

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
        _characters.ForgetFocus();
        _monsters.ForgetFocus();
        _showingMonsters = false;
        Entry.ClearMonsters();
        SetScrolls();
        _side.Prepare();
        _filter.Prepare();
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
                FocusSide(_side.Next(-1, 1), true);
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
            if (move.y != 0 && _filter.MoveList(move.y > 0 ? -1 : 1))
            {
                StorageSounds.PlayCursor();
            }

            return;
        }

        var list = ActiveList;
        if (move.y > 0 && StorageSlotLayout.IsTopRow(list))
        {
            if (!repeated)
            {
                FocusFilterBoard(true);
            }

            return;
        }

        // 줄 끝에서 오른쪽을 누르면 포커스 칸과 높이가 가장 가까운 오른쪽 항목으로 간다.
        if (move.x > 0
            && StorageSlotLayout.IsRowEnd(list)
            && (repeated || FocusSide(_side.Nearest(list.RectAt(list.FocusIndex)), true)))
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
            if (_filter.ApplyFocused())
            {
                StorageSounds.PlaySelect();
                Rebuild(true);
            }

            return;
        }

        if (_mode == StorageFocus.Side)
        {
            _side.Submit();
            return;
        }

        ConfirmFocused();
    }

    private void HandleCancel()
    {
        if (_mode == StorageFocus.Generation)
        {
            _filter.CloseList();
            _mode = StorageFocus.FilterBoard;
            _filter.SetBoardFocused(true);
            return;
        }

        if (_mode == StorageFocus.FilterBoard || _mode == StorageFocus.Side)
        {
            ReturnToCharacters(true);
            return;
        }

        if (_showingMonsters && Entry.ClearLastMonster())
        {
            StorageSounds.PlayEntryPickDown();
            return;
        }

        ClearEntry();
    }

    private void OnCategoryFocus(FilterOptionView option)
    {
        if (_mode == StorageFocus.Characters || _mode == StorageFocus.Side)
        {
            FocusFilterBoard(true);
        }
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
        if (_mode == StorageFocus.Generation && _filter.FocusOption(option))
        {
            StorageSounds.PlayCursor();
        }
    }

    private void OnGenerationConfirm(FilterOptionView option)
    {
        if (_mode != StorageFocus.Generation || !_filter.ConfirmOption(option))
        {
            return;
        }

        StorageSounds.PlaySelect();
        Rebuild(true);
    }

    private void FocusFilterBoard(bool playCursor)
    {
        if (!_filter.HasBoard)
        {
            return;
        }

        if (_mode == StorageFocus.Generation)
        {
            _filter.CloseList();
        }

        var entered = _mode != StorageFocus.FilterBoard;
        _mode = StorageFocus.FilterBoard;
        ClearSlotSelect();
        _side.ClearHighlight();
        _filter.SetBoardFocused(true);
        if (playCursor && entered)
        {
            StorageSounds.PlayCursor();
        }
    }

    private void OpenGeneration()
    {
        if (!_filter.CanOpen)
        {
            return;
        }

        _mode = StorageFocus.Generation;
        _filter.OpenList();
        StorageSounds.PlaySelect();
    }

    private void ReturnToCharacters(bool playCursor)
    {
        _filter.CloseList();
        _filter.SetBoardFocused(false);
        _mode = StorageFocus.Characters;
        _side.ClearHighlight();
        SetFocus(ActiveList.FocusIndex, false);
        if (playCursor)
        {
            StorageSounds.PlayCursor();
            ScrollToFocus();
        }
    }

    private void ClearSlotSelect()
    {
        _characters.ClearFocus();
        _monsters.ClearFocus();
    }

    private void OnSideFocus(FilterOptionView view)
    {
        if (_mode != StorageFocus.Generation)
        {
            FocusSide(_side.IndexOf(view), true);
        }
    }

    private void OnSideRelease(FilterOptionView view)
    {
        if (_mode == StorageFocus.Generation)
        {
            return;
        }

        var index = _side.IndexOf(view);
        if (!FocusSide(index, true))
        {
            return;
        }

        if (_side.IsCharacterEntry(index))
        {
            ClearEntry();
        }
        else if (Entry.ReleaseMonster(_side.MonsterEntryAt(index)))
        {
            StorageSounds.PlayEntryPickDown();
        }

        // 포커스된 엔트리 칸이 비면 아래, 없으면 위의 항목으로 옮기고, 둘 다 없으면 스토리지로 돌아간다.
        if (_mode != StorageFocus.Side || _side.IsAvailable(_side.Index))
        {
            return;
        }

        var next = _side.NextOrPrevious(_side.Index);
        if (next >= 0)
        {
            FocusSide(next, false);
            return;
        }

        ReturnToCharacters(false);
    }

    private bool FocusSide(int index, bool playCursor)
    {
        if (!_side.IsAvailable(index))
        {
            return false;
        }

        _filter.SetBoardFocused(false);
        var changed = _mode != StorageFocus.Side || index != _side.Index;
        _mode = StorageFocus.Side;
        ClearSlotSelect();
        _side.Highlight(index);
        if (playCursor && changed)
        {
            StorageSounds.PlayCursor();
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

        var next = _side.Next(_side.Index, move.y > 0 ? -1 : 1);
        if (next >= 0)
        {
            FocusSide(next, true);
        }
        else if (move.y > 0)
        {
            FocusFilterBoard(true);
        }
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
        var previousFocus = _characters.Focused;
        var account = GetAccount();
        var characters = account != null ? account.Playables : System.Array.Empty<PlayableCharacterData>();
        var filled = _characters.Fill(
            characters,
            character => _filter.Passes(character.Generation),
            character => account != null && account.IsPlayableUnlocked(character));
        if (!filled)
        {
            Entry.SetCharacter(null);
            return;
        }

        var selected = account != null ? account.ResolveSelectedPlayable() : null;
        SetFocus(_characters.IndexOf(previousFocus != null ? previousFocus : selected));
        Entry.SetCharacter(keepEntry ? selected : null);
        LayoutSlots();
    }

    private void RebuildMonsters()
    {
        var previousFocus = _monsters.Focused;
        var account = GetAccount();
        var monsters = account != null ? account.Monsters : System.Array.Empty<MonsterVisualData>();
        var filled = _monsters.Fill(
            monsters,
            monster => monster.Startable && _filter.Passes(monster.Generation),
            monster => account != null && account.IsMonsterUnlocked(monster));
        if (!filled)
        {
            return;
        }

        SetFocus(_monsters.IndexOf(previousFocus));
        LayoutSlots();
    }

    private void SetScrolls()
    {
        _characters.SetVisible(!_showingMonsters);
        _monsters.SetVisible(_showingMonsters);
    }

    private void LayoutSlots()
    {
        StorageSlotLayout.Layout(ActiveList);
    }

    private void SetFocus(int index, bool playCursor = false)
    {
        var showSlot = _mode == StorageFocus.Characters;
        var changed = ActiveList.SetFocus(index, showSlot);
        if (playCursor && changed && showSlot)
        {
            StorageSounds.PlayCursor();
        }
    }

    private void MoveFocus(Vector2Int move)
    {
        var list = ActiveList;
        if (list.Count == 0)
        {
            return;
        }

        SetFocus(StorageSlotLayout.MoveIndex(list, move), true);
        ScrollToFocus();
    }

    /// <summary>
    /// 키보드로 포커스한 칸이 뷰포트 밖이면 보이도록 스크롤한다.
    /// </summary>
    private void ScrollToFocus()
    {
        var list = ActiveList;
        _scrollFollow.ScrollTo(list.Scroll, list.RectAt(list.FocusIndex));
    }

    private void ShowInfo(StorageCharacterView slot)
    {
        if (_info != null)
        {
            _info.Show(slot);
        }
    }

    private void ShowInfo(StorageMonsterView slot)
    {
        if (_info != null)
        {
            _info.Show(slot);
        }
    }

    private void FocusSlot(StorageCharacterView slot)
    {
        FocusHoveredSlot(_characters, _characters.IndexOf(slot));
    }

    private void FocusMonsterSlot(StorageMonsterView slot)
    {
        FocusHoveredSlot(_monsters, _monsters.IndexOf(slot));
    }

    /// <summary>
    /// 마우스가 올라간 칸에 포커스한다. 보이는 목록이 아니거나 세대 목록이 열려 있으면 무시한다.
    /// </summary>
    private void FocusHoveredSlot(IStorageSlotList list, int index)
    {
        if (list != ActiveList || _mode == StorageFocus.Generation || _scrollFollow.IsHoverLocked())
        {
            return;
        }

        var fromFilter = _mode == StorageFocus.FilterBoard || _mode == StorageFocus.Side;
        if (fromFilter)
        {
            ReturnToCharacters(false);
        }

        if (index < 0)
        {
            return;
        }

        var sameSlot = fromFilter && index == list.FocusIndex;
        SetFocus(index, true);
        if (sameSlot)
        {
            StorageSounds.PlayCursor();
        }
    }

    private void ConfirmFocused()
    {
        if (_showingMonsters)
        {
            ConfirmMonsterSlot(_monsters.FocusedView);
            return;
        }

        ConfirmSlot(_characters.FocusedView);
    }

    private void ConfirmSlot(StorageCharacterView slot)
    {
        if (slot == null || slot.Data == null || !CanConfirm(slot))
        {
            return;
        }

        var account = GetAccount();
        if (account == null)
        {
            return;
        }

        account.SelectPlayable(slot.Data);
        Entry.SetCharacter(slot.Data);
        StorageSounds.PlayEntryPickUp();
        _showingMonsters = true;
        ClearSlotSelect();
        SetScrolls();
        RebuildMonsters();
        FocusGameStartIfFull();
    }

    private void ConfirmMonsterSlot(StorageMonsterView slot)
    {
        if (!_showingMonsters || slot == null || slot.Data == null || !CanConfirm(slot))
        {
            return;
        }

        if (!Entry.TryFill(slot.Data))
        {
            StorageSounds.PlayEntryLocked();
            return;
        }

        StorageSounds.PlayEntryPickUp();
        FocusGameStartIfFull();
    }

    /// <summary>
    /// 칸 목록에 포커스가 있을 때만 확정한다. 잠긴 칸은 에러 효과를 내고 막는다.
    /// </summary>
    private bool CanConfirm(IStorageSlotView slot)
    {
        if (_mode != StorageFocus.Characters)
        {
            return false;
        }

        if (!slot.IsUnlocked)
        {
            slot.PlayLockedSelect();
            return false;
        }

        return true;
    }

    // 엔트리가 다 차면 게임 시작 버튼으로 간다. 확정 소리가 이미 나므로 커서 소리는 내지 않는다.
    private void FocusGameStartIfFull()
    {
        if (Entry.IsFull())
        {
            FocusSide(_side.GameStartIndex, false);
        }
    }

    private void ClearEntry()
    {
        var account = GetAccount();
        if (account != null)
        {
            account.ClearSelectedPlayable();
        }

        // 트레이너가 빠지면 엔트리 전체가 비므로 빠지는 소리를 한 번만 낸다.
        if (Entry.Character != null)
        {
            StorageSounds.PlayEntryPickDown();
        }

        var restoreCharacters = _showingMonsters;
        _showingMonsters = false;
        Entry.ClearMonsters();
        SetScrolls();
        Entry.SetCharacter(null);
        if (restoreCharacters)
        {
            RebuildCharacters(false);
        }
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
