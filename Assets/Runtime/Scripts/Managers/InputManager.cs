using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 이동과 일시정지 입력을 한곳으로 모은다.
/// </summary>
public class InputManager : BaseManager
{
    private Vector2 _movementValue;
    private bool _pausePressed;

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
    /// 자리표시. 입력 담당이 장치 바인딩으로 이 읽기만 교체한다.
    /// </summary>
    private void ReadKeyboardPlaceholder()
    {
        _pausePressed = false;
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
    }
}
