using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 이동, 일시정지, 메뉴 선택 입력을 한곳으로 모은다.
/// </summary>
public class InputManager : BaseManager
{
    // 메뉴 방향키를 누르고 있으면 이 시간 뒤부터 반복 간격마다 한 번씩 다시 움직인다.
    private const float MENU_REPEAT_DELAY = 0.35f;
    private const float MENU_REPEAT_INTERVAL = 0.08f;

    private Vector2 _movementValue;
    private bool _pausePressed;
    private Vector2Int _menuMove;
    private Vector2Int _menuRepeatMove;
    private Vector2Int _menuHeld;
    private float _menuRepeatTimer;
    private bool _menuSubmit;
    private bool _menuCancel;
    private bool _storageFilter;
    private bool _actionPressed;
    private bool _inventoryPressed;
    private bool _shopRefreshPressed;
    private bool _damageWindowToggle;
    private int _numberSlot = -1;
    private InventoryHotkey _inventoryHotkey;

    public Vector2 MovementValue => _movementValue;

    /// <summary>
    /// 입력 매니저 초기화
    /// </summary>
    public override UniTask InitializeAsync()
    {
        return base.InitializeAsync();
    }

    private void Update()
    {
        ReadKeyboardPlaceholder();
    }

    /// <summary>
    /// 이번 프레임에 일시정지가 눌렸으면 true를 반환하고 소비한다.
    /// </summary>
    public bool ConsumePausePressed()
    {
        if (!_pausePressed)
        {
            return false;
        }

        _pausePressed = false;
        return true;
    }

    /// <summary>
    /// 이번 프레임의 메뉴 방향키를 반환하고 소비한다. x는 좌우, y는 상하다.
    /// </summary>
    public Vector2Int ConsumeMenuMove()
    {
        var move = _menuMove;
        _menuMove = Vector2Int.zero;
        _menuRepeatMove = Vector2Int.zero;
        return move;
    }

    /// <summary>
    /// 메뉴 방향키를 반환하고 소비한다. 누르고 있으면 반복 입력도 돌려주고, 그때 repeated가 true다.
    /// </summary>
    public Vector2Int ConsumeMenuMoveRepeat(out bool repeated)
    {
        repeated = _menuMove == Vector2Int.zero && _menuRepeatMove != Vector2Int.zero;
        var move = repeated ? _menuRepeatMove : _menuMove;
        _menuMove = Vector2Int.zero;
        _menuRepeatMove = Vector2Int.zero;
        return move;
    }

    /// <summary>
    /// 이번 프레임에 메뉴 확인이 눌렸으면 true를 반환하고 소비한다.
    /// </summary>
    public bool ConsumeMenuSubmit()
    {
        if (!_menuSubmit)
        {
            return false;
        }

        _menuSubmit = false;
        return true;
    }

    /// <summary>
    /// 이번 프레임에 메뉴 취소가 눌렸으면 true를 반환하고 소비한다. 백스페이스와 Esc다.
    /// </summary>
    public bool ConsumeMenuCancel()
    {
        if (!_menuCancel)
        {
            return false;
        }

        _menuCancel = false;
        return true;
    }

    /// <summary>
    /// 이번 프레임에 이동이나 확인을 눌렀으면 true를 반환하고 소비한다.
    /// </summary>
    public bool ConsumeActionPressed()
    {
        if (!_actionPressed)
        {
            return false;
        }

        _actionPressed = false;
        return true;
    }

    /// <summary>
    /// 이번 프레임에 스토리지 필터 바로가기가 눌렸으면 true를 반환하고 소비한다. C키다.
    /// </summary>
    public bool ConsumeStorageFilter()
    {
        if (!_storageFilter)
        {
            return false;
        }

        _storageFilter = false;
        return true;
    }

    /// <summary>
    /// 이번 프레임에 인벤토리 키가 눌렸으면 true를 반환하고 소비한다. I키다.
    /// </summary>
    public bool ConsumeInventoryPressed()
    {
        if (!_inventoryPressed)
        {
            return false;
        }

        _inventoryPressed = false;
        return true;
    }

    /// <summary>
    /// 이번 프레임에 상점 새로고침 키가 눌렸으면 true를 반환하고 소비한다. F키다.
    /// </summary>
    public bool ConsumeShopRefreshPressed()
    {
        if (!_shopRefreshPressed)
        {
            return false;
        }

        _shopRefreshPressed = false;
        return true;
    }

    /// <summary>
    /// 이번 프레임에 결과 화면 피해 통계 전환 키가 눌렸으면 true를 반환하고 소비한다. R키다.
    /// </summary>
    public bool ConsumeDamageWindowToggle()
    {
        if (!_damageWindowToggle)
        {
            return false;
        }

        _damageWindowToggle = false;
        return true;
    }

    /// <summary>
    /// 이번 프레임에 눌린 숫자키 칸 번호(0~3)를 반환하고 소비한다. 1~4키와 넘버패드 1~4다. 없으면 -1이다.
    /// </summary>
    public int ConsumeNumberSlot()
    {
        var slot = _numberSlot;
        _numberSlot = -1;
        return slot;
    }

    /// <summary>
    /// 이번 프레임에 눌린 인벤토리 단축키를 반환하고 소비한다. Z 합성, X 장착, C 해제, V 판매다. 없으면 None이다.
    /// </summary>
    public InventoryHotkey ConsumeInventoryHotkey()
    {
        var hotkey = _inventoryHotkey;
        _inventoryHotkey = InventoryHotkey.None;
        return hotkey;
    }

    /// <summary>
    /// 자리표시. 입력 담당이 장치 바인딩으로 이 읽기만 교체한다.
    /// </summary>
    private void ReadKeyboardPlaceholder()
    {
        _pausePressed = false;
        _menuMove = Vector2Int.zero;
        _menuRepeatMove = Vector2Int.zero;
        _menuSubmit = false;
        _menuCancel = false;
        _storageFilter = false;
        _actionPressed = false;
        _inventoryPressed = false;
        _shopRefreshPressed = false;
        _damageWindowToggle = false;
        _numberSlot = -1;
        _inventoryHotkey = InventoryHotkey.None;
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            _movementValue = Vector2.zero;
            _menuHeld = Vector2Int.zero;
            return;
        }

        var horizontal = 0f;
        var vertical = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        _movementValue = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        if (WasMovePressed(keyboard) || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
        {
            _actionPressed = true;
        }
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            _pausePressed = true;
        }

        ReadMenuKeys(keyboard);
    }

    private static bool WasMovePressed(Keyboard keyboard)
    {
        return keyboard.aKey.wasPressedThisFrame
            || keyboard.dKey.wasPressedThisFrame
            || keyboard.wKey.wasPressedThisFrame
            || keyboard.sKey.wasPressedThisFrame
            || keyboard.leftArrowKey.wasPressedThisFrame
            || keyboard.rightArrowKey.wasPressedThisFrame
            || keyboard.upArrowKey.wasPressedThisFrame
            || keyboard.downArrowKey.wasPressedThisFrame;
    }

    /// <summary>
    /// 메뉴 이동은 화살표 한 번 눌림만 본다. 대각선이면 좌우를 우선한다.
    /// </summary>
    private void ReadMenuKeys(Keyboard keyboard)
    {
        var moveX = 0;
        var moveY = 0;
        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            moveX -= 1;
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            moveX += 1;
        }

        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            moveY -= 1;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            moveY += 1;
        }

        if (moveX != 0)
        {
            moveY = 0;
        }

        _menuMove = new Vector2Int(moveX, moveY);
        ReadMenuRepeat(keyboard);
        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
        {
            _menuSubmit = true;
        }

        if (keyboard.backspaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
        {
            _menuCancel = true;
        }

        if (keyboard.cKey.wasPressedThisFrame)
        {
            _storageFilter = true;
        }

        if (keyboard.iKey.wasPressedThisFrame)
        {
            _inventoryPressed = true;
        }

        if (keyboard.fKey.wasPressedThisFrame)
        {
            _shopRefreshPressed = true;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            _damageWindowToggle = true;
        }

        // 한 프레임에 여러 키를 눌러도 앞 번호 하나만 받는다.
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) _numberSlot = 0;
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) _numberSlot = 1;
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) _numberSlot = 2;
        else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) _numberSlot = 3;

        // C는 스토리지 필터와 같은 키다. 둘은 따로 소비돼서 열린 창이 자기 것만 가져간다.
        if (keyboard.zKey.wasPressedThisFrame) _inventoryHotkey = InventoryHotkey.Synthesize;
        else if (keyboard.xKey.wasPressedThisFrame) _inventoryHotkey = InventoryHotkey.Equip;
        else if (keyboard.cKey.wasPressedThisFrame) _inventoryHotkey = InventoryHotkey.Unequip;
        else if (keyboard.vKey.wasPressedThisFrame) _inventoryHotkey = InventoryHotkey.Sell;
    }

    /// <summary>
    /// 새로 누른 방향을 기억하고, 그 방향을 계속 누르고 있으면 반복 입력을 만든다. 메뉴는 화면이 멈춰도 움직여서 실제 시간을 쓴다.
    /// </summary>
    private void ReadMenuRepeat(Keyboard keyboard)
    {
        if (_menuMove != Vector2Int.zero)
        {
            _menuHeld = _menuMove;
            _menuRepeatTimer = MENU_REPEAT_DELAY;
            return;
        }

        if (_menuHeld == Vector2Int.zero || !IsMenuHeld(keyboard, _menuHeld))
        {
            _menuHeld = Vector2Int.zero;
            return;
        }

        _menuRepeatTimer -= Time.unscaledDeltaTime;
        if (_menuRepeatTimer > 0f)
        {
            return;
        }

        _menuRepeatTimer += MENU_REPEAT_INTERVAL;
        _menuRepeatMove = _menuHeld;
    }

    private static bool IsMenuHeld(Keyboard keyboard, Vector2Int direction)
    {
        if (direction.x < 0)
        {
            return keyboard.leftArrowKey.isPressed;
        }

        if (direction.x > 0)
        {
            return keyboard.rightArrowKey.isPressed;
        }

        if (direction.y < 0)
        {
            return keyboard.downArrowKey.isPressed;
        }

        return direction.y > 0 && keyboard.upArrowKey.isPressed;
    }
}
