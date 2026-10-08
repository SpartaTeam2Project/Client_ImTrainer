using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼을 누른 느낌을 준다. Punch는 살짝 눌렸다 튀어 오르고, Play는 거기에 눌린 그림을 잠깐 덮는다.
/// 마우스로 누르면 버튼이 눌린 그림을 직접 보여 주니 Punch만, 단축키처럼 버튼을 거치지 않으면 Play를 쓴다.
/// 원래 그림은 건드리지 않고 overrideSprite만 쓴다. 시간이 멈춰 있어도 돈다.
/// </summary>
public sealed class PressFlash
{
    private const float DEFAULT_DURATION = 0.12f;
    private const float PUNCH_SCALE = -0.12f;
    private const float PUNCH_DURATION = 0.18f;
    private const int PUNCH_VIBRATO = 1;
    private const float PUNCH_ELASTICITY = 0.5f;

    private readonly Image _image;
    private readonly Sprite _pressed;
    private readonly GameObject _link;
    private readonly float _duration;
    private Tween _tween;
    private Tween _punch;

    public PressFlash(Image image, Sprite pressed, GameObject link, float duration = DEFAULT_DURATION)
    {
        _image = image;
        _pressed = pressed;
        _link = link;
        _duration = duration;
    }

    /// <summary>
    /// 버튼의 Sprite Swap에 넣은 Pressed 그림을 쓴다. 버튼이 없으면 아무것도 하지 않는다.
    /// </summary>
    public static PressFlash ForButton(Button button, float duration = DEFAULT_DURATION)
    {
        return button != null
            ? new PressFlash(button.image, button.spriteState.pressedSprite, button.gameObject, duration)
            : new PressFlash(null, null, null, duration);
    }

    /// <summary>
    /// 눌린 그림을 잠깐 덮고 눌리는 연출을 건다.
    /// </summary>
    public void Play()
    {
        Punch();
        if (_image == null || _pressed == null)
        {
            return;
        }

        StopSprite();
        _image.overrideSprite = _pressed;
        _tween = DOVirtual.DelayedCall(_duration, StopSprite, true).SetLink(_link);
    }

    /// <summary>
    /// 살짝 작아졌다 돌아오는 연출만 건다. 연달아 눌러도 크기가 어긋나지 않게 이전 연출을 끝낸 크기에서 다시 건다.
    /// </summary>
    public void Punch()
    {
        if (_link == null)
        {
            return;
        }

        StopPunch();
        _punch = _link.transform
            .DOPunchScale(Vector3.one * PUNCH_SCALE, PUNCH_DURATION, PUNCH_VIBRATO, PUNCH_ELASTICITY)
            .SetUpdate(true)
            .SetLink(_link);
    }

    /// <summary>
    /// 덮은 그림과 눌리는 연출을 바로 걷는다. 창이 꺼질 때 부른다.
    /// </summary>
    public void Release()
    {
        StopSprite();
        StopPunch();
    }

    private void StopSprite()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }

        if (_image != null)
        {
            _image.overrideSprite = null;
        }
    }

    private void StopPunch()
    {
        if (_punch != null)
        {
            _punch.Kill(true);
            _punch = null;
        }
    }
}
