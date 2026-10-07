using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스토리지 오른쪽 메뉴(트레이닝, 엔트리 칸, 로그아웃, 게임 시작). 항목 목록, 하이라이트, 다음 항목 찾기를 맡는다.
/// </summary>
public sealed class StorageSideMenu
{
    private sealed class Item
    {
        public FilterOptionView View;
        public Button Button;
        public bool IsCharacter;
        public int MonsterIndex = -1;
    }

    private readonly List<Item> _items = new List<Item>();
    private readonly Button _trainingButton;
    private readonly GameObject _entryCharacter;
    private readonly Image[] _entryMonsters;
    private readonly Button _logoutButton;
    private readonly Button _gameStartButton;
    private readonly StorageEntrySlots _entry;
    private readonly Action<FilterOptionView> _onFocus;
    private readonly Action<FilterOptionView> _onRelease;

    public StorageSideMenu(
        Button trainingButton,
        GameObject entryCharacter,
        Image[] entryMonsters,
        Button logoutButton,
        Button gameStartButton,
        StorageEntrySlots entry,
        Action<FilterOptionView> onFocus,
        Action<FilterOptionView> onRelease)
    {
        _trainingButton = trainingButton;
        _entryCharacter = entryCharacter;
        _entryMonsters = entryMonsters;
        _logoutButton = logoutButton;
        _gameStartButton = gameStartButton;
        _entry = entry;
        _onFocus = onFocus;
        _onRelease = onRelease;
    }

    /// <summary>
    /// 하이라이트된 항목 번호. 없으면 -1.
    /// </summary>
    public int Index { get; private set; } = -1;

    /// <summary>
    /// 항목 목록을 다시 만들고 마우스 콜백을 연결한다.
    /// </summary>
    public void Prepare()
    {
        _items.Clear();
        Index = -1;
        AddItem(_trainingButton != null ? _trainingButton.gameObject : null, _trainingButton, false, -1);
        AddItem(_entryCharacter, null, true, -1);
        for (var i = 0; i < _entryMonsters.Length; i++)
        {
            AddItem(_entryMonsters[i] != null ? _entryMonsters[i].gameObject : null, null, false, i);
        }

        AddItem(_logoutButton != null ? _logoutButton.gameObject : null, _logoutButton, false, -1);
        AddItem(_gameStartButton != null ? _gameStartButton.gameObject : null, _gameStartButton, false, -1);
    }

    public int IndexOf(FilterOptionView view)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].View == view)
            {
                return i;
            }
        }

        return -1;
    }

    public int IndexOf(Button button)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].Button == button)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 게임 시작 버튼 항목 번호. 없으면 -1.
    /// </summary>
    public int GameStartIndex => _gameStartButton != null ? IndexOf(_gameStartButton) : -1;

    public bool IsCharacterEntry(int index)
    {
        return index >= 0 && index < _items.Count && _items[index].IsCharacter;
    }

    /// <summary>
    /// 포켓몬 엔트리 항목이면 엔트리 칸 번호, 아니면 -1.
    /// </summary>
    public int MonsterEntryAt(int index)
    {
        return index >= 0 && index < _items.Count ? _items[index].MonsterIndex : -1;
    }

    /// <summary>
    /// 엔트리 칸은 채워져 있을 때만 포커스할 수 있다. 잠긴 칸은 포커스하지 않는다.
    /// </summary>
    public bool IsAvailable(int index)
    {
        if (index < 0 || index >= _items.Count || !_items[index].View.isActiveAndEnabled)
        {
            return false;
        }

        var item = _items[index];
        if (item.IsCharacter)
        {
            return _entry.Character != null;
        }

        if (item.MonsterIndex >= 0)
        {
            return _entry.HasMonster(item.MonsterIndex);
        }

        return true;
    }

    /// <summary>
    /// from에서 direction 쪽으로 가며 포커스할 수 있는 첫 항목. 없으면 -1.
    /// </summary>
    public int Next(int from, int direction)
    {
        for (var i = from + direction; i >= 0 && i < _items.Count; i += direction)
        {
            if (IsAvailable(i))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// from 아래에서 포커스할 수 있는 항목, 없으면 위의 항목. 둘 다 없으면 -1.
    /// </summary>
    public int NextOrPrevious(int from)
    {
        var next = Next(from, 1);
        return next >= 0 ? next : Next(from, -1);
    }

    /// <summary>
    /// 칸과 높이가 가장 가까운 항목을 고른다. 칸이 없으면 맨 위 항목.
    /// </summary>
    public int Nearest(RectTransform slot)
    {
        if (slot == null)
        {
            return Next(-1, 1);
        }

        var slotY = slot.TransformPoint(slot.rect.center).y;
        var best = -1;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < _items.Count; i++)
        {
            if (!IsAvailable(i))
            {
                continue;
            }

            var rect = (RectTransform)_items[i].View.transform;
            var distance = Mathf.Abs(rect.TransformPoint(rect.rect.center).y - slotY);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    public void Highlight(int index)
    {
        Index = index;
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].View.SetFocused(i == Index);
        }
    }

    public void ClearHighlight()
    {
        Highlight(-1);
    }

    /// <summary>
    /// 하이라이트된 항목이 버튼이면 누른다.
    /// </summary>
    public void Submit()
    {
        if (!IsAvailable(Index))
        {
            return;
        }

        var button = _items[Index].Button;
        if (button != null && button.IsInteractable())
        {
            button.onClick.Invoke();
        }
    }

    private void AddItem(GameObject target, Button button, bool isCharacter, int monsterIndex)
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
        view.Bind(_onFocus, _onFocus, isEntry ? _onRelease : null);
        _items.Add(new Item
        {
            View = view,
            Button = button,
            IsCharacter = isCharacter,
            MonsterIndex = monsterIndex,
        });
    }
}
