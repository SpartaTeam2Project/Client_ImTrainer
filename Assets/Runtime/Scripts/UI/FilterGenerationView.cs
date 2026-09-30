using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 세대 필터 창. 위쪽부터 전체, 1세대~9세대다.
/// </summary>
public class FilterGenerationView : MonoBehaviour
{
    public const int ALL_GENERATION = 0;
    private const int OPTION_COUNT = 10;

    private readonly List<FilterOptionView> _options = new List<FilterOptionView>();
    private Action<FilterOptionView> _onFocus;
    private Action<FilterOptionView> _onConfirm;
    private int _focusIndex;

    public int AppliedGeneration { get; private set; } = ALL_GENERATION;

    /// <summary>
    /// 행 콜백을 연결하고 항목을 위쪽부터 모은다.
    /// </summary>
    public void Bind(Action<FilterOptionView> onFocus, Action<FilterOptionView> onConfirm)
    {
        _onFocus = onFocus;
        _onConfirm = onConfirm;
        EnsureOptions();
        for (var i = 0; i < _options.Count; i++)
        {
            _options[i].Bind(_onFocus, _onConfirm);
        }
    }

    /// <summary>
    /// 적용 세대를 전체로 되돌리고 포커스 표시를 끈다.
    /// </summary>
    public void ResetFilter()
    {
        EnsureOptions();
        AppliedGeneration = ALL_GENERATION;
        _focusIndex = ALL_GENERATION;
        RefreshApplied();
        ClearFocus();
    }

    /// <summary>
    /// 창을 열고 적용된 세대에 포커스를 둔다.
    /// </summary>
    public void Open()
    {
        EnsureOptions();
        gameObject.SetActive(true);
        _focusIndex = Mathf.Clamp(AppliedGeneration, 0, Mathf.Max(0, _options.Count - 1));
        RefreshFocus();
    }

    /// <summary>
    /// 창을 끄고 포커스 표시를 지운다. 적용된 세대는 유지한다.
    /// </summary>
    public void Close()
    {
        ClearFocus();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 포커스를 한 칸 옮긴다. 방향은 아래가 양수다. 끝이거나 그대로면 false다.
    /// </summary>
    public bool Move(int direction)
    {
        if (_options.Count == 0 || direction == 0)
        {
            return false;
        }

        var next = Mathf.Clamp(_focusIndex + direction, 0, _options.Count - 1);
        if (next == _focusIndex)
        {
            return false;
        }

        _focusIndex = next;
        RefreshFocus();
        return true;
    }

    /// <summary>
    /// 마우스가 올라온 행으로 포커스를 옮긴다. 이미 그 행이면 false다.
    /// </summary>
    public bool FocusOption(FilterOptionView option)
    {
        var index = _options.IndexOf(option);
        if (index < 0 || index == _focusIndex)
        {
            return false;
        }

        _focusIndex = index;
        RefreshFocus();
        return true;
    }

    /// <summary>
    /// 행을 적용하고 그 세대 값을 반환한다. 0은 전체다.
    /// </summary>
    public int ConfirmOption(FilterOptionView option)
    {
        var index = _options.IndexOf(option);
        if (index >= 0)
        {
            _focusIndex = index;
        }

        return ApplyFocused();
    }

    /// <summary>
    /// 포커스된 행을 적용하고 그 세대 값을 반환한다. 0은 전체다.
    /// </summary>
    public int ApplyFocused()
    {
        AppliedGeneration = _focusIndex;
        RefreshApplied();
        RefreshFocus();
        return AppliedGeneration;
    }

    private void EnsureOptions()
    {
        if (_options.Count > 0)
        {
            return;
        }

        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            var option = child.GetComponent<FilterOptionView>();
            if (option == null)
            {
                option = child.AddComponent<FilterOptionView>();
            }

            _options.Add(option);
        }

        if (_options.Count != OPTION_COUNT)
        {
            Debug.LogError("세대 필터 항목은 전체와 1세대부터 9세대까지 열 개여야 합니다.");
        }
    }

    private void RefreshFocus()
    {
        for (var i = 0; i < _options.Count; i++)
        {
            _options[i].SetFocused(i == _focusIndex);
        }
    }

    private void ClearFocus()
    {
        for (var i = 0; i < _options.Count; i++)
        {
            _options[i].SetFocused(false);
        }
    }

    private void RefreshApplied()
    {
        for (var i = 0; i < _options.Count; i++)
        {
            _options[i].SetApplied(i == AppliedGeneration);
        }
    }
}
