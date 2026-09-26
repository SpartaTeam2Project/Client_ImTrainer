using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 이동, 일시정지, 메뉴 선택 입력을 한곳으로 모은다.
/// </summary>
public class InputManager : BaseManager
{
    private Vector2 _movementValue;
    private bool _pausePressed;
    private Vector2Int _menuMove;
    private bool _menuSubmit;

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
    /// 자리표시. 입력 담당이 장치 바인딩으로 이 읽기만 교체한다.
    /// </summary>
    private void ReadKeyboardPlaceholder()
    {
        _pausePressed = false;
        _menuMove = Vector2Int.zero;
        _menuSubmit = false;
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            _movementValue = Vector2.zero;
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
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            _pausePressed = true;
        }

        ReadMenuKeys(keyboard);
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
        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
        {
            _menuSubmit = true;
        }
    }
}
