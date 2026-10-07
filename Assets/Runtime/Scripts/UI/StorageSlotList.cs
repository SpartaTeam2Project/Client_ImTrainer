using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지금 보이는 스토리지 목록을 종류와 상관없이 다룰 때 쓴다.
/// </summary>
public interface IStorageSlotList
{
    int Count { get; }

    int FocusIndex { get; }

    RectTransform Content { get; }

    GridLayoutGroup Grid { get; }

    ScrollRect Scroll { get; }

    RectTransform RectAt(int index);

    /// <summary>
    /// 칸 하나에 포커스한다. 포커스 칸이 바뀌었으면 true.
    /// </summary>
    bool SetFocus(int index, bool showSlot);

    void ClearFocus();
}

/// <summary>
/// 스토리지 스크롤 하나의 칸 목록. 칸을 만들고 지우고, 포커스와 정보 패널 표시를 맡는다.
/// </summary>
public sealed class StorageSlotList<TView, TData> : IStorageSlotList
    where TView : MonoBehaviour, IStorageSlotView<TView, TData>
    where TData : class
{
    private readonly List<TView> _slots = new List<TView>();
    private readonly TView _prefab;
    private readonly Transform _content;
    private readonly GridLayoutGroup _grid;
    private readonly GameObject _scrollObject;
    private readonly string _missingMessage;
    private readonly Action<TView> _onFocus;
    private readonly Action<TView> _onConfirm;
    private readonly Action<TView> _showInfo;
    private ScrollRect _scroll;

    public StorageSlotList(
        TView prefab,
        Transform content,
        GridLayoutGroup grid,
        GameObject scrollObject,
        string missingMessage,
        Action<TView> onFocus,
        Action<TView> onConfirm,
        Action<TView> showInfo)
    {
        _prefab = prefab;
        _content = content;
        _grid = grid;
        _scrollObject = scrollObject;
        _missingMessage = missingMessage;
        _onFocus = onFocus;
        _onConfirm = onConfirm;
        _showInfo = showInfo;
    }

    public int Count => _slots.Count;

    public int FocusIndex { get; private set; }

    /// <summary>
    /// 마지막으로 포커스한 칸의 데이터. 칸을 다시 만들어도 같은 데이터에 포커스를 돌려줄 때 쓴다.
    /// </summary>
    public TData Focused { get; private set; }

    public TView FocusedView => FocusIndex >= 0 && FocusIndex < _slots.Count ? _slots[FocusIndex] : null;

    public RectTransform Content => _content as RectTransform;

    public GridLayoutGroup Grid => _grid;

    public ScrollRect Scroll
    {
        get
        {
            if (_scroll == null && _scrollObject != null)
            {
                _scroll = _scrollObject.GetComponent<ScrollRect>();
            }

            return _scroll;
        }
    }

    public void SetVisible(bool visible)
    {
        if (_scrollObject != null)
        {
            _scrollObject.SetActive(visible);
        }
    }

    /// <summary>
    /// 칸을 모두 지우고 조건에 맞는 데이터로 다시 만든다. 참조가 없으면 정보 패널을 비우고 false.
    /// </summary>
    public bool Fill(IReadOnlyList<TData> items, Func<TData, bool> include, Func<TData, bool> isUnlocked)
    {
        Clear();
        if (_prefab == null || _content == null)
        {
            Debug.LogError(_missingMessage);
            ShowInfo(null);
            return false;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null || !include(item))
            {
                continue;
            }

            var slot = UnityEngine.Object.Instantiate(_prefab, _content);
            slot.Bind(item, isUnlocked(item), _onFocus, _onConfirm);
            _slots.Add(slot);
        }

        return true;
    }

    /// <summary>
    /// 기억해 둔 포커스 데이터를 잊는다. 다음에 칸을 만들면 처음 칸부터 포커스한다.
    /// </summary>
    public void ForgetFocus()
    {
        Focused = null;
    }

    public int IndexOf(TView slot)
    {
        return _slots.IndexOf(slot);
    }

    /// <summary>
    /// 데이터가 들어 있는 칸 번호. 없으면 0.
    /// </summary>
    public int IndexOf(TData data)
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

    public RectTransform RectAt(int index)
    {
        if (index < 0 || index >= _slots.Count)
        {
            return null;
        }

        return _slots[index].transform as RectTransform;
    }

    public bool SetFocus(int index, bool showSlot)
    {
        if (_slots.Count == 0)
        {
            FocusIndex = 0;
            Focused = null;
            ShowInfo(null);
            return false;
        }

        var next = Mathf.Clamp(index, 0, _slots.Count - 1);
        var changed = next != FocusIndex;
        FocusIndex = next;
        Focused = _slots[FocusIndex].Data;
        for (var i = 0; i < _slots.Count; i++)
        {
            _slots[i].SetFocused(showSlot && i == FocusIndex);
        }

        ShowInfo(_slots[FocusIndex]);
        return changed;
    }

    public void ClearFocus()
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            _slots[i].SetFocused(false);
        }
    }

    private void Clear()
    {
        _slots.Clear();
        if (_content == null)
        {
            return;
        }

        for (var i = _content.childCount - 1; i >= 0; i--)
        {
            var child = _content.GetChild(i).gameObject;
            child.SetActive(false);
            UnityEngine.Object.Destroy(child);
        }
    }

    private void ShowInfo(TView slot)
    {
        _showInfo?.Invoke(slot);
    }
}
