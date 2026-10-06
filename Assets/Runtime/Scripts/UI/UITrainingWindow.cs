using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스토리지에서 포켓달러로 트레이닝 레벨을 올린다. 카테고리 열로 나눈 스킬트리 형태다.
/// </summary>
public class UITrainingWindow : MonoBehaviour
{
    private const string MENU_MOVE_SOUND = "cursor";
    private const string BUTTON_CLICK_SOUND = "select";
    private const string OPEN_SOUND = "open";
    private const string CLOSE_SOUND = "close";
    private const string RESET_LABEL = "리셋";
    private const string RESET_CONFIRM_LABEL = "정말 리셋?";
    private const string LOCK_FORMAT = "<color=#{0}>{1}</color> Lv {2} 필요";
    private const float PERPENDICULAR_WEIGHT = 2f;

    [SerializeField] private TrainingDatabase _database;
    [SerializeField] private TrainingSlotBehavior _slotPrefab;
    [SerializeField] private TrainingCategoryColumn[] _columns;
    [SerializeField] private TrainingDetailPanel _detail;
    [SerializeField] private TMP_Text _currencyLabel;
    [SerializeField] private Image _currencyIcon;
    [SerializeField] private Button _resetButton;
    [SerializeField] private GameObject _resetSelect;
    [SerializeField] private TMP_Text _resetLabel;
    [SerializeField] private Button _backButton;
    [SerializeField] private GameObject _backSelect;

    private readonly List<TrainingSlotBehavior> _slots = new List<TrainingSlotBehavior>();
    private int _playerId;
    private int _selectedIndex;
    private int _lastSlotIndex;
    private bool _resetArmed;
    private bool _subscribed;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        if (_backButton != null)
        {
            _backButton.onClick.AddListener(Close);
            DisableNavigation(_backButton);
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.AddListener(HandleResetClick);
            DisableNavigation(_resetButton);
        }
    }

    private void OnDestroy()
    {
        if (_backButton != null)
        {
            _backButton.onClick.RemoveListener(Close);
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.RemoveListener(HandleResetClick);
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
        _selectedIndex = _slots.Count > 0 ? 0 : BackIndex();
        _lastSlotIndex = 0;
        SetResetArmed(false);
        ApplySelection();
    }

    private void OnDisable()
    {
        UnsubscribeCurrency();
        ClearSlots();
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
        ClearSlots();
        if (_database == null || _slotPrefab == null)
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

            var column = FindColumn(data.Category);
            if (column == null)
            {
                Debug.LogError($"트레이닝 창에 {data.Category} 열이 없습니다.");
                continue;
            }

            var slot = Instantiate(_slotPrefab);
            slot.gameObject.SetActive(true);
            column.Place(slot, data.Row, data.Column);
            slot.Init(data, column.Color, FocusSlot, PurchaseSlot);
            _slots.Add(slot);
        }

        SortSlotsByLayout();
        RefreshAll();
    }

    private void SortSlotsByLayout()
    {
        // 처음 고를 칸이 왼쪽 위가 되도록 열, 행, 칸 순서로 정렬한다.
        _slots.Sort((a, b) =>
        {
            var category = a.Data.Category.CompareTo(b.Data.Category);
            if (category != 0)
            {
                return category;
            }

            var row = a.Data.Row.CompareTo(b.Data.Row);
            return row != 0 ? row : a.Data.Column.CompareTo(b.Data.Column);
        });
    }

    private void ClearSlots()
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i].transform.SetParent(null);
                Destroy(_slots[i].gameObject);
            }
        }

        _slots.Clear();
    }

    private void RefreshAll()
    {
        if (!TryGetTraining(out var training))
        {
            return;
        }

        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var level = training.GetTrainingLevel(_playerId, slot.Data.TrainingType) + 1;
            slot.Redraw(level, training.IsUnlocked(_playerId, slot.Data));
        }

        if (_columns != null)
        {
            for (var i = 0; i < _columns.Length; i++)
            {
                if (_columns[i] != null)
                {
                    _columns[i].SetLevel(training.GetCategoryLevel(_playerId, _columns[i].Category));
                }
            }
        }

        RefreshCurrency();
        RefreshDetail();
    }

    private void RefreshCurrency()
    {
        if (!TryGetCurrencies(out var currencies))
        {
            return;
        }

        if (_currencyLabel != null)
        {
            var save = currencies.GetCurrency(_playerId, CurrenciesManager.POCKET_DOLLAR_ID, true);
            _currencyLabel.text = save == null ? "0" : save.Amount.ToString();
        }

        if (_currencyIcon != null)
        {
            _currencyIcon.sprite = currencies.GetIcon(CurrenciesManager.POCKET_DOLLAR_ID);
            _currencyIcon.enabled = _currencyIcon.sprite != null;
        }
    }

    private void RefreshDetail()
    {
        if (_detail == null || _slots.Count == 0)
        {
            return;
        }

        // 버튼을 고른 동안에는 마지막으로 본 칸을 계속 보여 준다.
        var slot = _slots[Mathf.Clamp(IsSlotSelected() ? _selectedIndex : _lastSlotIndex, 0, _slots.Count - 1)];
        var cost = slot.IsMaxed ? null : slot.Data.GetLevel(slot.Level);
        _detail.Show(slot, BuildLockText(slot.Data), cost != null && CanAfford(cost.Cost), GetPocketDollarIcon());
    }

    private string BuildLockText(TrainingData data)
    {
        var column = FindColumn(data.RequiredCategory);
        var title = column != null ? column.Title : data.RequiredCategory.ToString();
        var color = column != null ? column.Color : Color.white;
        return string.Format(LOCK_FORMAT, ColorUtility.ToHtmlStringRGB(color), title, data.RequiredLevel);
    }

    private void MoveSelection(Vector2Int move)
    {
        var next = FindNeighbor(_selectedIndex, new Vector2(move.x, move.y));
        if (next < 0 || next == _selectedIndex)
        {
            return;
        }

        Select(next);
    }

    /// <summary>
    /// 화면 위치 기준으로 그 방향에서 가장 가까운 대상을 찾는다. 없으면 -1.
    /// </summary>
    private int FindNeighbor(int from, Vector2 direction)
    {
        var origin = GetTarget(from);
        if (origin == null)
        {
            return -1;
        }

        var best = -1;
        var bestScore = float.MaxValue;
        var count = BackIndex() + 1;
        for (var i = 0; i < count; i++)
        {
            var target = GetTarget(i);
            if (i == from || target == null)
            {
                continue;
            }

            var delta = (Vector2)(target.position - origin.position);
            var along = Vector2.Dot(delta, direction);
            if (along <= 0f)
            {
                continue;
            }

            var perpendicular = Mathf.Abs(direction.x * delta.y - direction.y * delta.x);
            if (perpendicular > along * PERPENDICULAR_WEIGHT)
            {
                continue;
            }

            var score = along + perpendicular * PERPENDICULAR_WEIGHT;
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        return best;
    }

    private void Select(int index)
    {
        _selectedIndex = index;
        if (IsSlotSelected())
        {
            _lastSlotIndex = index;
        }

        if (!IsResetSelected())
        {
            SetResetArmed(false);
        }

        ApplySelection();
        PlaySound(MENU_MOVE_SOUND);
    }

    private void SubmitSelection()
    {
        if (IsBackSelected())
        {
            Close();
            return;
        }

        if (IsResetSelected())
        {
            HandleResetClick();
            return;
        }

        if (IsSlotSelected())
        {
            PurchaseSlot(_slots[_selectedIndex]);
        }
    }

    private void FocusSlot(TrainingSlotBehavior slot)
    {
        var index = _slots.IndexOf(slot);
        if (index < 0 || index == _selectedIndex)
        {
            return;
        }

        Select(index);
    }

    private void PurchaseSlot(TrainingSlotBehavior slot)
    {
        if (slot == null || slot.Data == null || !slot.Unlocked || slot.IsMaxed)
        {
            return;
        }

        var level = slot.Data.GetLevel(slot.Level);
        if (level == null)
        {
            return;
        }

        if (!TryGetCurrencies(out var currencies) || !TryGetTraining(out var training))
        {
            Debug.LogError("트레이닝 구매에 필요한 매니저가 없습니다.");
            return;
        }

        if (!currencies.TryWithdraw(_playerId, CurrenciesManager.POCKET_DOLLAR_ID, level.Cost, true))
        {
            return;
        }

        training.IncrementTrainingLevel(_playerId, slot.Data.TrainingType);
        PlaySound(BUTTON_CLICK_SOUND);
        RefreshAll();
    }

    private void HandleResetClick()
    {
        if (!IsResetSelected())
        {
            _selectedIndex = ResetIndex();
            ApplySelection();
        }

        // 실수로 지우지 않도록 두 번 눌러야 실행한다.
        if (!_resetArmed)
        {
            SetResetArmed(true);
            PlaySound(MENU_MOVE_SOUND);
            return;
        }

        SetResetArmed(false);
        ResetTrainings();
    }

    private void ResetTrainings()
    {
        if (!TryGetCurrencies(out var currencies) || !TryGetTraining(out var training))
        {
            Debug.LogError("트레이닝 리셋에 필요한 매니저가 없습니다.");
            return;
        }

        var refund = training.ResetAll(_playerId);
        currencies.DepositMeta(_playerId, CurrenciesManager.POCKET_DOLLAR_ID, refund);
        PlaySound(BUTTON_CLICK_SOUND);
        RefreshAll();
    }

    private void SetResetArmed(bool armed)
    {
        _resetArmed = armed;
        if (_resetLabel != null)
        {
            _resetLabel.text = armed ? RESET_CONFIRM_LABEL : RESET_LABEL;
        }
    }

    private void ApplySelection()
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            _slots[i].SetFocused(i == _selectedIndex);
        }

        if (_resetSelect != null)
        {
            _resetSelect.SetActive(IsResetSelected());
        }

        if (_backSelect != null)
        {
            _backSelect.SetActive(IsBackSelected());
        }

        RefreshDetail();
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

        RefreshCurrency();
        RefreshDetail();
    }

    private TrainingCategoryColumn FindColumn(TrainingCategory category)
    {
        if (_columns == null)
        {
            return null;
        }

        for (var i = 0; i < _columns.Length; i++)
        {
            if (_columns[i] != null && _columns[i].Category == category)
            {
                return _columns[i];
            }
        }

        return null;
    }

    private RectTransform GetTarget(int index)
    {
        if (index >= 0 && index < _slots.Count)
        {
            return _slots[index].Rect;
        }

        if (index == ResetIndex())
        {
            return _resetButton == null ? null : (RectTransform)_resetButton.transform;
        }

        if (index == BackIndex())
        {
            return _backButton == null ? null : (RectTransform)_backButton.transform;
        }

        return null;
    }

    private bool CanAfford(int cost)
    {
        if (cost <= 0 || !TryGetCurrencies(out var currencies))
        {
            return false;
        }

        var save = currencies.GetCurrency(_playerId, CurrenciesManager.POCKET_DOLLAR_ID, true);
        return save != null && save.CanAfford(cost);
    }

    private Sprite GetPocketDollarIcon()
    {
        return TryGetCurrencies(out var currencies)
            ? currencies.GetIcon(CurrenciesManager.POCKET_DOLLAR_ID)
            : null;
    }

    private static bool TryGetTraining(out TrainingManager training)
    {
        training = null;
        return Managers.Instance != null && Managers.Instance.TryGetManager(out training);
    }

    private static bool TryGetCurrencies(out CurrenciesManager currencies)
    {
        currencies = null;
        return Managers.Instance != null && Managers.Instance.TryGetManager(out currencies);
    }

    private int ResolvePlayerId()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return playerManager.LocalPlayerId;
        }

        return 1;
    }

    private int ResetIndex()
    {
        return _slots.Count;
    }

    private int BackIndex()
    {
        return _slots.Count + 1;
    }

    private bool IsSlotSelected()
    {
        return _selectedIndex >= 0 && _selectedIndex < _slots.Count;
    }

    private bool IsResetSelected()
    {
        return _selectedIndex == ResetIndex();
    }

    private bool IsBackSelected()
    {
        return _selectedIndex == BackIndex();
    }

    private bool HasRequiredReferences()
    {
        return _database != null
            && _slotPrefab != null
            && _columns != null
            && _columns.Length > 0
            && _detail != null
            && _resetButton != null
            && _backButton != null;
    }

    private static void DisableNavigation(Button button)
    {
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
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
