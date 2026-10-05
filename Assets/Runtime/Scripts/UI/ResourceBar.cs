using System;
using UnityEngine;

/// <summary>
/// 표시할 재화 목록을 정하고 ResourceItem 칸을 관리한다. 재화를 보여 주는 화면은 이 바를 같이 쓴다.
/// </summary>
public class ResourceBar : MonoBehaviour
{
    [Serializable]
    private class Entry
    {
        public CurrencyId Currency;

        /// <summary>
        /// true면 판이 끝나도 남는 메타 잔액을 보여 준다.
        /// </summary>
        public bool Meta;

        /// <summary>
        /// true면 0개일 때 칸을 띄우지 않는다.
        /// </summary>
        public bool HideWhenZero;
    }

    [SerializeField] private ResourceItem _itemPrefab;
    [SerializeField] private RectTransform _content;
    [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

    private ResourceItem[] _items = Array.Empty<ResourceItem>();
    private int _playerId = -1;
    private bool _subscribed;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>
    /// 플레이어의 재화 수량으로 칸을 다시 그린다. 이후 수량이 바뀌면 알아서 갱신한다.
    /// </summary>
    public void Refresh(int playerId)
    {
        _playerId = playerId;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<CurrenciesManager>(out var currencies))
        {
            return;
        }

        if (_items.Length != _entries.Length)
        {
            Array.Resize(ref _items, _entries.Length);
        }

        var sibling = 0;
        for (var i = 0; i < _entries.Length; i++)
        {
            var entry = _entries[i];
            if (entry == null)
            {
                continue;
            }

            string id = entry.Currency;
            var save = currencies.GetCurrency(playerId, id, entry.Meta);
            var amount = save != null ? save.Amount : 0;
            if (entry.HideWhenZero && amount <= 0)
            {
                if (_items[i] != null)
                {
                    _items[i].gameObject.SetActive(false);
                }

                continue;
            }

            var item = GetOrCreate(i);
            if (item == null)
            {
                continue;
            }

            item.gameObject.SetActive(true);
            item.transform.SetSiblingIndex(sibling++);
            item.Show(currencies.GetIcon(id), amount);
        }
    }

    private ResourceItem GetOrCreate(int index)
    {
        if (_items[index] != null)
        {
            return _items[index];
        }

        if (_itemPrefab == null)
        {
            Debug.LogError("ResourceBar에 ResourceItem 프리팹이 없습니다.");
            return null;
        }

        var parent = _content != null ? _content : transform;
        _items[index] = Instantiate(_itemPrefab, parent);
        return _items[index];
    }

    private bool Contains(string currencyId)
    {
        for (var i = 0; i < _entries.Length; i++)
        {
            if (_entries[i] != null && (string)_entries[i].Currency == currencyId)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleCurrencyChanged(CurrencyAmountChanged changed)
    {
        if (_playerId < 0 || changed.PlayerId != _playerId || !Contains(changed.CurrencyId))
        {
            return;
        }

        Refresh(_playerId);
    }

    private void Subscribe()
    {
        if (_subscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            return;
        }

        eventManager.Subscribe<CurrencyAmountChanged>(HandleCurrencyChanged);
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || Managers.Instance == null || !Managers.Instance.TryGetManager<EventManager>(out var eventManager))
        {
            _subscribed = false;
            return;
        }

        eventManager.Unsubscribe<CurrencyAmountChanged>(HandleCurrencyChanged);
        _subscribed = false;
    }
}
