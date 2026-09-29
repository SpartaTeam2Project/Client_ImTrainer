using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 스토리지에서 플레이어블 캐릭터를 고른다.
/// </summary>
public class UIStorageWindow : MonoBehaviour
{
    private enum StorageFocus
    {
        Characters,
        FilterBoard,
        Generation,
    }

    private const string MENU_MOVE_SOUND = "cursor";
    private const string FILTER_APPLY_SOUND = "select";

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

    private readonly List<StorageCharacterView> _slots = new List<StorageCharacterView>();
    private StorageFocus _mode = StorageFocus.Characters;
    private PlayableCharacterData _focusedCharacter;
    private int _focusIndex;

    public bool IsOpen => isActiveAndEnabled;

    /// <summary>
    /// 스토리지 창을 연다. 꺼져 있으면 켜면서 칸을 다시 채운다.
    /// </summary>
    public void Open()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        PrepareFilters();
        Rebuild(false);
    }

    private void OnEnable()
    {
        PrepareFilters();
        Rebuild(false);
    }

    private void Update()
    {
        if (_introScene != null && _introScene.IsOpen)
        {
            return;
        }

        if (!TryGetInput(out var inputManager))
        {
            return;
        }

        if (inputManager.ConsumeStorageFilter() && _mode == StorageFocus.Characters)
        {
            FocusFilterBoard(true);
        }

        var move = inputManager.ConsumeMenuMove();
        if (move != Vector2Int.zero)
        {
            HandleMove(move);
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

    private void PrepareFilters()
    {
        _mode = StorageFocus.Characters;
        _focusedCharacter = null;
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

    private void HandleMove(Vector2Int move)
    {
        if (_mode == StorageFocus.FilterBoard)
        {
            if (move.y < 0)
            {
                ReturnToCharacters(true);
            }

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
            FocusFilterBoard(true);
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

        ConfirmFocused();
    }

    private void HandleCancel()
    {
        if (_mode == StorageFocus.Generation)
        {
            CloseGeneration();
            return;
        }

        if (_mode == StorageFocus.FilterBoard)
        {
            ReturnToCharacters(true);
            return;
        }

        ClearEntry();
    }

    private void OnCategoryFocus(FilterOptionView option)
    {
        if (_mode != StorageFocus.Characters)
        {
            return;
        }

        FocusFilterBoard(true);
    }

    private void OnCategoryConfirm(FilterOptionView option)
    {
        if (_mode == StorageFocus.Characters)
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
        SetFocus(_focusIndex, false);
        if (playCursor)
        {
            PlayCursor();
        }
    }

    private void ClearSlotSelect()
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            _slots[i].SetFocused(false);
        }
    }

    private bool IsTopRow()
    {
        return _slots.Count > 0 && _focusIndex < ColumnCount();
    }

    private bool PassesGeneration(PlayableCharacterData character)
    {
        if (_generationFilter == null || _generationFilter.AppliedGeneration == FilterGenerationView.ALL_GENERATION)
        {
            return true;
        }

        return character.Generation == _generationFilter.AppliedGeneration;
    }

    private void Rebuild(bool keepEntry)
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
            if (character == null || !PassesGeneration(character))
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
        if (_mode != StorageFocus.Characters)
        {
            return;
        }

        var index = _slots.IndexOf(slot);
        if (index < 0)
        {
            return;
        }

        SetFocus(index, true);
    }

    private void MoveFocus(Vector2Int move)
    {
        if (_slots.Count == 0)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var columns = ColumnCount();
        var index = _focusIndex;
        if (move.x != 0)
        {
            var rowStart = index - index % columns;
            var rowEnd = Mathf.Min(rowStart + columns, _slots.Count) - 1;
            index = Mathf.Clamp(index + move.x, rowStart, rowEnd);
        }
        else if (move.y != 0)
        {
            var next = index - move.y * columns;
            if (next >= 0 && next < _slots.Count)
            {
                index = next;
            }
        }

        SetFocus(index, true);
    }

    private void SetFocus(int index, bool playCursor = false)
    {
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

    private void ConfirmFocused()
    {
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
    }

    private void ClearEntry()
    {
        var account = GetAccount();
        if (account != null)
        {
            account.ClearSelectedPlayable();
        }

        ApplyEntry(null);
    }

    private void ApplyEntry(PlayableCharacterData data)
    {
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
        if (_grid != null)
        {
            _grid.enabled = false;
        }

        var content = _trainerContent as RectTransform;
        if (content == null || _slots.Count == 0)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var padding = _grid != null ? _grid.padding : new RectOffset();
        var spacing = _grid != null ? _grid.spacing : Vector2.zero;
        var contentWidth = content.rect.width;
        var innerRight = contentWidth - padding.right;
        var x = (float)padding.left;
        var y = (float)padding.top;
        var rowHeight = 0f;

        for (var i = 0; i < _slots.Count; i++)
        {
            var rect = _slots[i].transform as RectTransform;
            if (rect == null)
            {
                continue;
            }

            var width = rect.sizeDelta.x;
            var height = rect.sizeDelta.y;
            if (x > padding.left && x + width > innerRight)
            {
                x = padding.left;
                y += rowHeight + spacing.y;
                rowHeight = 0f;
            }

            rect.anchoredPosition = new Vector2(x, -y);
            x += width + spacing.x;
            rowHeight = Mathf.Max(rowHeight, height);
        }

        var size = content.sizeDelta;
        size.y = y + rowHeight + padding.bottom;
        content.sizeDelta = size;
    }

    private int ColumnCount()
    {
        if (_slots.Count == 0)
        {
            return 1;
        }

        var slot = _slots[0].transform as RectTransform;
        var content = _trainerContent as RectTransform;
        if (slot == null || content == null)
        {
            return 1;
        }

        var spacing = _grid != null ? _grid.spacing.x : 0f;
        var padding = _grid != null ? _grid.padding.left + _grid.padding.right : 0;
        var stride = slot.sizeDelta.x + spacing;
        if (stride <= 0.01f)
        {
            return 1;
        }

        var inner = content.rect.width - padding + spacing;
        return Mathf.Max(1, Mathf.FloorToInt((inner + 0.001f) / stride));
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
