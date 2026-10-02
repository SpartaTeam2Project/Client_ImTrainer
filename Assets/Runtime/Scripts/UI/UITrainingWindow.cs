using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스토리지에서 포켓달러로 트레이닝 레벨을 올린다.
/// </summary>
public class UITrainingWindow : MonoBehaviour
{
    private const int COLUMN_COUNT = 2;
    private const float ITEM_SCROLL_GAP = 16f;
    private const string MENU_MOVE_SOUND = "cursor";
    private const string BUTTON_CLICK_SOUND = "select";
    private const string OPEN_SOUND = "open";
    private const string CLOSE_SOUND = "close";

    [SerializeField] private TrainingDatabase _database;
    [SerializeField] private TrainingItemBehavior _itemPrefab;
    [SerializeField] private RectTransform _itemsParent;
    [SerializeField] private ScrollRect _scrollView;
    [SerializeField] private GridLayoutGroup _grid;
    [SerializeField] private Button _backButton;
    [SerializeField] private GameObject _backSelect;

    private readonly List<TrainingItemBehavior> _items = new List<TrainingItemBehavior>();
    private int _playerId;
    private int _selectedIndex;
    private bool _subscribed;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        if (_backButton != null)
        {
            _backButton.onClick.AddListener(Close);
            var navigation = _backButton.navigation;
            navigation.mode = Navigation.Mode.None;
            _backButton.navigation = navigation;
        }
    }

    private void OnDestroy()
    {
        if (_backButton != null)
        {
            _backButton.onClick.RemoveListener(Close);
        }
    }

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("트레이닝 창에 필요한 참조가 없습니다.");
        }

        _playerId = ResolvePlayerId();
        Rebuild();
        SubscribeCurrency();
        _selectedIndex = _items.Count > 0 ? 0 : BackIndex();
        ApplySelection(false);
    }

    private void OnDisable()
    {
        UnsubscribeCurrency();
        ClearItems();
    }

    private void Update()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<InputManager>(out var inputManager))
        {
            return;
        }

        var move = inputManager.ConsumeMenuMove();
        if (move != Vector2Int.zero)
        {
            MoveSelection(move);
        }

        if (inputManager.ConsumeMenuSubmit())
        {
            SubmitSelection();
        }

        if (inputManager.ConsumeMenuCancel())
        {
            Close();
        }
    }

    /// <summary>
    /// 트레이닝 창을 연다.
    /// </summary>
    public void Open()
    {
        if (gameObject.activeSelf)
        {
            return;
        }

        PlaySound(OPEN_SOUND);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 트레이닝 창을 닫는다.
    /// </summary>
    public void Close()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        PlaySound(CLOSE_SOUND);
        gameObject.SetActive(false);
    }

    private void Rebuild()
    {
        ClearItems();
        if (_database == null || _itemPrefab == null || _itemsParent == null)
        {
            return;
        }

        for (var i = 0; i < _database.Count; i++)
        {
            var data = _database.GetTraining(i);
            if (data == null || !data.ShowInUi)
            {
                continue;
            }

            var item = Instantiate(_itemPrefab, _itemsParent);
            item.gameObject.SetActive(true);
            var savedLevel = GetSavedLevel(data.TrainingType);
            item.Init(data, savedLevel + 1, _playerId, FocusItem, PurchaseItem);
            _items.Add(item);
        }

        ResizeContent();
    }

    private void ResizeContent()
    {
        if (_itemsParent == null || _grid == null)
        {
            return;
        }

        var rows = (_items.Count + COLUMN_COUNT - 1) / COLUMN_COUNT;
        var height = (float)(_grid.padding.top + _grid.padding.bottom);
        if (rows > 0)
        {
            height += rows * _grid.cellSize.y + (rows - 1) * _grid.spacing.y;
        }

        var size = _itemsParent.sizeDelta;
        size.y = height;
        _itemsParent.sizeDelta = size;
    }

    private void ClearItems()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null)
            {
                _items[i].transform.SetParent(null);
                Destroy(_items[i].gameObject);
            }
        }

        _items.Clear();
    }

    private void MoveSelection(Vector2Int move)
    {
        var previous = _selectedIndex;
        if (move.x > 0)
        {
            MoveHorizontal(1);
        }
        else if (move.x < 0)
        {
            MoveHorizontal(-1);
        }
        else if (move.y < 0)
        {
            MoveVertical(COLUMN_COUNT);
        }
        else if (move.y > 0)
        {
            MoveVertical(-COLUMN_COUNT);
        }

        if (_selectedIndex == previous)
        {
            return;
        }

        ApplySelection(true);
        PlaySound(MENU_MOVE_SOUND);
    }

    private void MoveHorizontal(int direction)
    {
        if (IsBackSelected())
        {
            return;
        }

        var next = _selectedIndex + direction;
        if (next >= 0 && next < _items.Count)
        {
            _selectedIndex = next;
            return;
        }

        if (direction > 0)
        {
            _selectedIndex = BackIndex();
        }
    }

    private void MoveVertical(int delta)
    {
        if (IsBackSelected())
        {
            if (delta < 0 && _items.Count > 0)
            {
                _selectedIndex = _items.Count - 1;
            }

            return;
        }

        var next = _selectedIndex + delta;
        if (next >= 0 && next < _items.Count)
        {
            _selectedIndex = next;
            return;
        }

        if (delta > 0)
        {
            _selectedIndex = BackIndex();
        }
    }

    private void SubmitSelection()
    {
        if (IsBackSelected())
        {
            Close();
            return;
        }

        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
        {
            return;
        }

        PurchaseItem(_items[_selectedIndex]);
    }

    private void FocusItem(TrainingItemBehavior item)
    {
        var index = _items.IndexOf(item);
        if (index < 0 || index == _selectedIndex)
        {
            return;
        }

        _selectedIndex = index;
        ApplySelection(true);
        PlaySound(MENU_MOVE_SOUND);
    }

    private void PurchaseItem(TrainingItemBehavior item)
    {
        if (item == null || item.Data == null || !item.CanPurchase)
        {
            return;
        }

        var level = item.Data.GetLevel(GetSavedLevel(item.Data.TrainingType) + 1);
        if (level == null)
        {
            return;
        }

        if (Managers.Instance == null
            || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies)
            || !Managers.Instance.TryGetManager<TrainingManager>(out var training))
        {
            Debug.LogError("트레이닝 구매에 필요한 매니저가 없습니다.");
            return;
        }

        if (!currencies.TryWithdraw(_playerId, CurrenciesManager.POCKET_DOLLAR_ID, level.Cost, true))
        {
            item.Redraw();
            return;
        }

        training.IncrementTrainingLevel(_playerId, item.Data.TrainingType);
        item.AdvanceLevel();
        PlaySound(BUTTON_CLICK_SOUND);
    }

    private void ApplySelection(bool scroll)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].SetFocused(i == _selectedIndex);
        }

        if (_backSelect != null)
        {
            _backSelect.SetActive(IsBackSelected());
        }

        if (scroll && !IsBackSelected())
        {
            ScrollToSelected();
        }
    }

    private void ScrollToSelected()
    {
        if (_scrollView == null || _selectedIndex < 0 || _selectedIndex >= _items.Count)
        {
            return;
        }

        var selected = _items[_selectedIndex];
        if (selected == null || selected.Rect == null || _scrollView.content == null)
        {
            return;
        }

        var local = (Vector2)_scrollView.transform.InverseTransformPoint(selected.Rect.position);
        var scrollHeight = ((RectTransform)_scrollView.transform).rect.height;
        var itemHeight = selected.Rect.rect.height;
        var content = _scrollView.content;
        if (local.y > scrollHeight / 2f)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, content.anchoredPosition.y - itemHeight - ITEM_SCROLL_GAP);
        }
        else if (local.y < -scrollHeight / 2f)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, content.anchoredPosition.y + itemHeight + ITEM_SCROLL_GAP);
        }
    }

    private void SubscribeCurrency()
    {
        if (_subscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            return;
        }

        eventManager.Subscribe<CurrencyAmountChanged>(HandleCurrencyChanged);
        _subscribed = true;
    }

    private void UnsubscribeCurrency()
    {
        if (!_subscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            _subscribed = false;
            return;
        }

        eventManager.Unsubscribe<CurrencyAmountChanged>(HandleCurrencyChanged);
        _subscribed = false;
    }

    private void HandleCurrencyChanged(CurrencyAmountChanged changed)
    {
        if (changed.PlayerId != _playerId || !changed.IsMetaBalance || changed.CurrencyId != CurrenciesManager.POCKET_DOLLAR_ID)
        {
            return;
        }

        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Redraw();
        }
    }

    private int GetSavedLevel(TrainingType trainingType)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<TrainingManager>(out var training))
        {
            return -1;
        }

        return training.GetTrainingLevel(_playerId, trainingType);
    }

    private int ResolvePlayerId()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return playerManager.LocalPlayerId;
        }

        return 1;
    }

    private int BackIndex()
    {
        return _items.Count;
    }

    private bool IsBackSelected()
    {
        return _selectedIndex >= _items.Count;
    }

    private bool HasRequiredReferences()
    {
        return _database != null
            && _itemPrefab != null
            && _itemsParent != null
            && _scrollView != null
            && _backButton != null;
    }

    private static void PlaySound(string soundName)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(soundName);
    }
}
