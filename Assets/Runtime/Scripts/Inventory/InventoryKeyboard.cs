using UnityEngine;

/// <summary>
/// 키보드 조작이 인벤토리 창에 요청하는 동작.
/// </summary>
public interface IInventoryKeyboardTarget
{
    /// <summary>
    /// 지금 고른 가방이나 장착 칸. 상점 칸을 골랐거나 고른 칸이 없으면 false.
    /// </summary>
    bool TryGetSelection(out InventorySlotDrag.SlotKind kind, out int index);

    bool TryGetHolders(out InventoryHolder bag, out InventoryHolder equipment);

    void Select(InventorySlotDrag.SlotKind kind, int index);

    void Equip();

    void Unequip();

    void Sell();

    /// <summary>
    /// 출발 칸 포켓몬을 대상 칸으로 합성하고 결과 칸을 고른다.
    /// </summary>
    void Synthesize(InventorySlotRef source, InventorySlotDrag.SlotKind targetKind, int targetIndex);

    void Refresh();

    /// <summary>
    /// 안내 문구를 잠깐 띄운다.
    /// </summary>
    void Notify(string message);
}

/// <summary>
/// 인벤토리 창 키보드 조작. 방향키로 칸을 옮기고 Z 합성, X 장착, C 해제, V 판매를 한다.
/// Z를 누르면 합성 모드가 되어 방향키로 재료 칸을 고르고 Z로 확정, C나 Esc로 취소한다.
/// </summary>
public sealed class InventoryKeyboard
{
    private const string WRONG_TARGET_MESSAGE = "같은 종, 같은 성 포켓몬을 고르세요.";
    private const string CURSOR_SOUND = "cursor";
    private const string PICK_SOUND = "select";
    private const string ERROR_SOUND = "error";

    private readonly IInventoryKeyboardTarget _target;
    private readonly InventoryFocusNavigator _navigator;
    private InventorySlotRef _source = InventorySlotRef.None;
    private InventorySlotDrag.SlotKind _cursorKind = InventorySlotDrag.SlotKind.None;
    private int _cursorIndex = -1;

    public InventoryKeyboard(IInventoryKeyboardTarget target, InventoryFocusNavigator navigator)
    {
        _target = target;
        _navigator = navigator;
    }

    /// <summary>
    /// 합성할 칸을 고르는 중이면 true.
    /// </summary>
    public bool Synthesizing => !_source.IsEmpty;

    /// <summary>
    /// 합성 모드를 시작한 칸. 이 칸은 선택 표시로 남는다.
    /// </summary>
    public InventorySlotRef Source => _source;

    public InventorySlotDrag.SlotKind CursorKind => _cursorKind;

    public int CursorIndex => _cursorIndex;

    /// <summary>
    /// 창이 열려 있는 동안 매 프레임 입력을 읽는다.
    /// </summary>
    public void Tick(InputManager inputManager)
    {
        var move = inputManager.ConsumeMenuMoveRepeat(out _);
        if (move != Vector2Int.zero)
        {
            Move(move);
        }

        switch (inputManager.ConsumeInventoryHotkey())
        {
            case InventoryHotkey.Synthesize:
                if (!TryConfirm())
                {
                    Begin();
                }

                break;
            case InventoryHotkey.Equip:
                if (!Synthesizing)
                {
                    _target.Equip();
                }

                break;
            case InventoryHotkey.Unequip:
                if (Synthesizing)
                {
                    Cancel();
                }
                else
                {
                    _target.Unequip();
                }

                break;
            case InventoryHotkey.Sell:
                if (!Synthesizing)
                {
                    _target.Sell();
                }

                break;
        }
    }

    /// <summary>
    /// 합성 모드면 고르는 칸으로 합성을 확정하고 true를 돌려준다. 합성 버튼도 합성 모드에서는 이걸 부른다.
    /// </summary>
    public bool TryConfirm()
    {
        if (!Synthesizing)
        {
            return false;
        }

        Confirm();
        return true;
    }

    /// <summary>
    /// 합성 모드면 취소하고 true를 돌려준다. Esc가 창을 닫기 전에 먼저 부른다. 해제 버튼도 합성 모드에서는 이걸 부른다.
    /// </summary>
    public bool TryCancel()
    {
        if (!Synthesizing)
        {
            return false;
        }

        Cancel();
        return true;
    }

    /// <summary>
    /// 합성 모드를 끈다. 다시 그리지는 않는다. 창을 닫거나 마우스로 칸을 고를 때 부른다.
    /// </summary>
    public void Reset()
    {
        _source = InventorySlotRef.None;
        _cursorKind = InventorySlotDrag.SlotKind.None;
        _cursorIndex = -1;
    }

    /// <summary>
    /// 창을 다시 그리기 전에 부른다. 출발 포켓몬이 사라졌으면 합성 모드를 풀고, 고르던 칸이 사라졌으면 출발 칸으로 되돌린다.
    /// </summary>
    public void Validate(InventoryHolder bag, InventoryHolder equipment)
    {
        if (!Synthesizing)
        {
            return;
        }

        if (_source.Kind == InventorySlotDrag.SlotKind.Bag)
        {
            _source.Index = InventoryMerge.ResolveBagIndex(bag, _source);
        }

        var holder = _source.Kind == InventorySlotDrag.SlotKind.Bag ? bag : equipment;
        if (!InventoryMerge.IsSame(holder, _source.Index, _source))
        {
            Reset();
            return;
        }

        if (!_navigator.Contains(_cursorKind, _cursorIndex))
        {
            _cursorKind = _source.Kind;
            _cursorIndex = _source.Index;
        }
    }

    /// <summary>
    /// 평소에는 고른 칸을 옮기고, 합성 모드에서는 출발 칸 선택을 두고 고르는 칸만 옮긴다.
    /// </summary>
    private void Move(Vector2Int move)
    {
        var kind = _cursorKind;
        var index = _cursorIndex;
        if (!Synthesizing && !_target.TryGetSelection(out kind, out index))
        {
            kind = InventorySlotDrag.SlotKind.None;
            index = -1;
        }

        if (!_navigator.TryMove(kind, index, move, out var nextKind, out var nextIndex) || (nextKind == kind && nextIndex == index))
        {
            return;
        }

        PlaySound(CURSOR_SOUND);
        _navigator.ScrollTo(nextKind, nextIndex);
        if (!Synthesizing)
        {
            _target.Select(nextKind, nextIndex);
            return;
        }

        _cursorKind = nextKind;
        _cursorIndex = nextIndex;
        _target.Refresh();
    }

    private void Begin()
    {
        if (!_target.TryGetSelection(out var kind, out var index) || !_target.TryGetHolders(out var bag, out var equipment))
        {
            return;
        }

        var holder = kind == InventorySlotDrag.SlotKind.Bag ? bag : equipment;
        if (holder == null || index < 0 || index >= holder.Stacks.Count || holder.Stacks[index].Empty || holder.Stacks[index].Item == null)
        {
            PlaySound(ERROR_SOUND);
            return;
        }

        var item = holder.Stacks[index].Item;
        _source = new InventorySlotRef(kind, index, item.uid, item.upgradeLevel);
        _cursorKind = kind;
        _cursorIndex = index;
        PlaySound(PICK_SOUND);
        _target.Refresh();
    }

    /// <summary>
    /// 고르는 칸이 같은 종, 같은 성이면 합성한다. 출발 칸 자신을 고르면 남은 재료를 가방과 장착에서 모은다.
    /// </summary>
    private void Confirm()
    {
        if (!_target.TryGetHolders(out var bag, out var equipment))
        {
            return;
        }

        if (!InventoryMerge.IsTarget(bag, equipment, _source, _cursorKind, _cursorIndex, true))
        {
            PlaySound(ERROR_SOUND);
            _target.Notify(WRONG_TARGET_MESSAGE);
            return;
        }

        var source = _source;
        var targetKind = _cursorKind;
        var targetIndex = _cursorIndex;
        Reset();
        _target.Synthesize(source, targetKind, targetIndex);
    }

    private void Cancel()
    {
        Reset();
        _target.Refresh();
    }

    private static void PlaySound(string name)
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(name);
    }
}
