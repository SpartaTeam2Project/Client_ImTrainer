using System;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIEffect 디졸브로 그림이 타들어가듯 사라지게 한다. 디졸브 모양과 가장자리 색은 UIEffect 값을 그대로 쓴다.
/// 칸 아이콘이면 아이콘 위의 덮개에 같은 그림을 넣어 덮개를 지우고, 아이콘은 CanvasRenderer 알파로 숨긴다.
/// 그사이 칸을 다시 그려 색이 바뀌어도 숨긴 채로 남는다.
/// 끌기 그림처럼 UIEffect가 그 그림에 바로 붙어 있으면 그 그림을 지운다.
/// </summary>
public sealed class IconDissolve
{
    private const float DISSOLVE_DURATION = 0.9f;

    private readonly Image _source;
    private readonly UIEffect _effect;
    private readonly Image _cover;
    private readonly GameObject _link;
    private Tween _tween;
    private Sprite _target;

    /// <summary>
    /// source는 지울 그림, effect는 디졸브를 거는 UIEffect다. effect가 source에 붙어 있으면 source를 바로 지운다.
    /// </summary>
    public IconDissolve(Image source, UIEffect effect, GameObject link)
    {
        _source = source;
        _effect = effect;
        _cover = effect != null ? effect.GetComponent<Image>() : null;
        _link = link;
    }

    private bool UsesCover => _cover != _source;

    /// <summary>
    /// 그림을 디졸브로 지운다. 다 사라지면 되돌리고 onFinished를 부른다.
    /// 그 자리에서 판매하고 칸을 다시 그리면 같은 프레임이라 아이콘이 다시 보이지 않는다. 도중에 Stop되면 부르지 않는다.
    /// </summary>
    public void Play(Action onFinished)
    {
        Stop();
        if (_source == null || _cover == null || _source.sprite == null)
        {
            onFinished?.Invoke();
            return;
        }

        _target = _source.sprite;
        if (UsesCover)
        {
            _cover.sprite = _source.sprite;
            _cover.preserveAspect = _source.preserveAspect;
            _source.canvasRenderer.SetAlpha(0f);
        }

        _effect.transitionRate = 0f;
        _effect.enabled = true;
        _effect.gameObject.SetActive(true);
        // 창이 열려 있으면 시간이 멈춰 있어서 시간 정지와 상관없이 돈다.
        _tween = DOVirtual.Float(0f, 1f, DISSOLVE_DURATION, rate => _effect.transitionRate = rate)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(_link)
            .OnComplete(() =>
            {
                _tween = null;
                Restore();
                onFinished?.Invoke();
            });
    }

    /// <summary>
    /// 디졸브 없이 칸 아이콘만 숨겨 둔다. 끌기 그림이 대신 디졸브되는 동안 쓴다.
    /// </summary>
    public void Hold()
    {
        Stop();
        if (_source == null || !UsesCover)
        {
            return;
        }

        _target = _source.sprite;
        _source.canvasRenderer.SetAlpha(0f);
    }

    /// <summary>
    /// 디졸브 중 다른 포켓몬이 그려지면 멈춘다. 가방이 다시 정렬된 경우다.
    /// </summary>
    public void NotifyShown(Sprite portrait)
    {
        if (_target != null && portrait != _target)
        {
            Stop();
        }
    }

    /// <summary>
    /// 디졸브를 멈추고 숨긴 아이콘을 되돌린다.
    /// </summary>
    public void Stop()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }

        Restore();
    }

    private void Restore()
    {
        _target = null;
        if (_source != null && UsesCover)
        {
            _source.canvasRenderer.SetAlpha(1f);
        }

        if (_effect != null)
        {
            _effect.gameObject.SetActive(false);
        }
    }
}
