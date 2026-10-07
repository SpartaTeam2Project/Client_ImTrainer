using System;
using UnityEngine;

/// <summary>
/// 스토리지 세대 필터. 필터 보드(카테고리 칸)와 세대 목록을 묶어 열고 닫고, 적용된 세대로 거른다.
/// </summary>
public sealed class StorageGenerationFilter
{
    private readonly FilterOptionView _category;
    private readonly FilterGenerationView _list;
    private readonly Action<FilterOptionView> _onCategoryFocus;
    private readonly Action<FilterOptionView> _onCategoryConfirm;
    private readonly Action<FilterOptionView> _onGenerationFocus;
    private readonly Action<FilterOptionView> _onGenerationConfirm;

    public StorageGenerationFilter(
        FilterOptionView category,
        FilterGenerationView list,
        Action<FilterOptionView> onCategoryFocus,
        Action<FilterOptionView> onCategoryConfirm,
        Action<FilterOptionView> onGenerationFocus,
        Action<FilterOptionView> onGenerationConfirm)
    {
        _category = category;
        _list = list;
        _onCategoryFocus = onCategoryFocus;
        _onCategoryConfirm = onCategoryConfirm;
        _onGenerationFocus = onGenerationFocus;
        _onGenerationConfirm = onGenerationConfirm;
    }

    public bool HasBoard => _category != null;

    public bool CanOpen => _category != null && _list != null;

    /// <summary>
    /// 콜백을 연결하고 필터를 전체 세대로 되돌린 뒤 닫는다.
    /// </summary>
    public void Prepare()
    {
        if (!CanOpen)
        {
            Debug.LogError("스토리지 필터 참조가 없습니다.");
            return;
        }

        _category.Bind(_onCategoryFocus, _onCategoryConfirm);
        _category.SetFocused(false);
        _list.Bind(_onGenerationFocus, _onGenerationConfirm);
        _list.ResetFilter();
        _list.Close();
    }

    public void SetBoardFocused(bool focused)
    {
        if (_category != null)
        {
            _category.SetFocused(focused);
        }
    }

    public void OpenList()
    {
        if (!CanOpen)
        {
            return;
        }

        _category.SetFocused(true);
        _list.Open();
    }

    public void CloseList()
    {
        if (_list != null)
        {
            _list.Close();
        }
    }

    /// <summary>
    /// 세대 목록 포커스를 옮긴다. 옮겼으면 true.
    /// </summary>
    public bool MoveList(int direction)
    {
        return _list != null && _list.Move(direction);
    }

    public bool FocusOption(FilterOptionView option)
    {
        return _list != null && _list.FocusOption(option);
    }

    /// <summary>
    /// 마우스로 고른 세대를 적용한다. 목록이 없으면 false.
    /// </summary>
    public bool ConfirmOption(FilterOptionView option)
    {
        if (_list == null)
        {
            return false;
        }

        _list.ConfirmOption(option);
        return true;
    }

    /// <summary>
    /// 키보드로 포커스한 세대를 적용한다. 목록이 없으면 false.
    /// </summary>
    public bool ApplyFocused()
    {
        if (_list == null)
        {
            return false;
        }

        _list.ApplyFocused();
        return true;
    }

    public bool Passes(int generation)
    {
        if (_list == null || _list.AppliedGeneration == FilterGenerationView.ALL_GENERATION)
        {
            return true;
        }

        return generation == _list.AppliedGeneration;
    }
}
