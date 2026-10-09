using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 칸 전체를 덮는 덮개에 UIEffect 샤이니를 걸어, 빛줄기가 칸을 대각선으로 한 번 훑게 한다.
/// 블렌드, 색 필터, 빛줄기 모양은 프리팹 UIEffect 값을 그대로 쓴다. 칸의 다른 머티리얼은 건드리지 않는다.
/// </summary>
public sealed class SlotShiny
{
    private const float SHINY_DURATION = 0.6f;

    private readonly UIEffect _effect;
    private readonly GameObject _link;
    private Tween _tween;

    public SlotShiny(UIEffect effect, GameObject link)
    {
        _effect = effect;
        _link = link;
    }

    /// <summary>
    /// 덮개를 켜고 빛줄기를 한 번 지나가게 한 뒤 끈다. 창이 열려 있으면 시간이 멈춰 있어서 시간 정지와 상관없이 돈다.
    /// </summary>
    public void Play()
    {
        if (_effect == null)
        {
            return;
        }

        Stop();
        _effect.transitionRate = 0f;
        _effect.gameObject.SetActive(true);
        _tween = DOVirtual.Float(0f, 1f, SHINY_DURATION, rate => _effect.transitionRate = rate)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(_link)
            .OnComplete(() =>
            {
                _tween = null;
                Hide();
            });
    }

    public void Stop()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }

        Hide();
    }

    private void Hide()
    {
        if (_effect != null)
        {
            _effect.gameObject.SetActive(false);
        }
    }
}
