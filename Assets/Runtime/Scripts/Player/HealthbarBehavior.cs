using DG.Tweening;
using UnityEngine;

/// <summary>
/// 캐릭터 머리 위 체력바. 숫자는 플레이어가 갖고, 이 컴포넌트는 게이지와 표시만 갱신한다.
/// </summary>
public class HealthbarBehavior : MonoBehaviour
{
    private const float FADE_DURATION = 0.3f;
    private const float HP_MEDIUM_THRESHOLD = 0.5f;
    private const float HP_LOW_THRESHOLD = 0.25f;
    private const int HP_BAR_PIXELS = 48;
    private const float HP_TWEEN_DURATION = 0.3f;
    private const string DANGER_SOUND = "danger";

    [SerializeField] private SpriteRenderer _fillImage;
    [SerializeField] private SpriteRenderer _backgroundImage;
    [SerializeField] private Sprite _hpHigh;
    [SerializeField] private Sprite _hpMedium;
    [SerializeField] private Sprite _hpLow;

    private float _maxHealth = 1f;
    private float _currentHealth = 1f;
    private float _shownHealth = 1f;
    private bool _hpReady;
    private bool _autoShowOnChanged;
    private bool _autoHideWhenMax;
    private bool _isShown;
    private Tweener _fade;
    private Tweener _hpTween;

    #region Unity Methods

    private void OnDisable()
    {
        KillTween(ref _fade);
        KillTween(ref _hpTween);
        _hpReady = false;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 체력이 변하면 바를 보여 준다.
    /// </summary>
    public void SetAutoShowOnChanged(bool value)
    {
        _autoShowOnChanged = value;
    }

    /// <summary>
    /// 최대 체력이면 바를 숨긴다. 이미 풀피면 바로 숨긴다.
    /// </summary>
    public void SetAutoHideWhenMax(bool value)
    {
        _autoHideWhenMax = value;
        if (value && _currentHealth >= _maxHealth)
        {
            ForceHide();
        }
    }

    /// <summary>
    /// 현재 체력과 최대 체력으로 게이지를 다시 그린다. 처음 한 번은 바로, 이후에는 트윈으로 바꾼다.
    /// </summary>
    public void Apply(float current, float max)
    {
        _maxHealth = Mathf.Max(1f, max);
        _currentHealth = Mathf.Clamp(current, 0f, _maxHealth);
        RefreshGauge();

        if (_currentHealth <= 0f)
        {
            Hide(true);
            return;
        }

        if (_autoHideWhenMax && _currentHealth >= _maxHealth)
        {
            if (_isShown)
            {
                Hide(false);
                return;
            }

            ForceHide();
            return;
        }

        if (_autoShowOnChanged && !_isShown && _currentHealth < _maxHealth)
        {
            Show();
        }
    }

    #endregion

    #region Private Methods

    private void RefreshGauge()
    {
        KillTween(ref _hpTween);
        if (!_hpReady)
        {
            _hpReady = true;
            ApplyShown(_currentHealth);
            return;
        }

        _hpTween = DOTween.To(() => _shownHealth, ApplyShown, _currentHealth, HP_TWEEN_DURATION)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void ApplyShown(float shown)
    {
        _shownHealth = shown;
        if (_fillImage == null)
        {
            return;
        }

        var ratio = Mathf.Clamp01(shown / _maxHealth);
        // 스프라이트 픽셀 단위로 끊어 바 끝이 픽셀 중간에서 잘리지 않게 한다.
        var fillTransform = _fillImage.transform;
        var scale = fillTransform.localScale;
        scale.x = Mathf.Ceil(ratio * HP_BAR_PIXELS) / HP_BAR_PIXELS;
        fillTransform.localScale = scale;

        var sprite = ratio > HP_MEDIUM_THRESHOLD ? _hpHigh : ratio > HP_LOW_THRESHOLD ? _hpMedium : _hpLow;
        if (sprite == null || _fillImage.sprite == sprite)
        {
            return;
        }

        _fillImage.sprite = sprite;
        // 빨간 구간에 들어설 때 한 번만 울린다. 사망으로 떨어지는 중이면 울리지 않는다.
        if (sprite == _hpLow && _currentHealth > 0f)
        {
            PlayDangerSound();
        }
    }

    private static void PlayDangerSound()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(DANGER_SOUND);
    }

    private void Show()
    {
        _isShown = true;
        PlayFade(1f, false);
    }

    private void Hide(bool ignoreTimeScale)
    {
        _isShown = false;
        PlayFade(0f, ignoreTimeScale);
    }

    private void ForceHide()
    {
        _isShown = false;
        KillTween(ref _fade);
        SetAlpha(0f);
    }

    private void PlayFade(float targetAlpha, bool ignoreTimeScale)
    {
        KillTween(ref _fade);
        _fade = DOTween.To(() => GetAlpha(), value => SetAlpha(value), targetAlpha, FADE_DURATION)
            .SetEase(Ease.OutSine);
        if (ignoreTimeScale)
        {
            _fade.SetUpdate(true);
        }
    }

    private float GetAlpha()
    {
        if (_fillImage == null)
        {
            return 0f;
        }

        return _fillImage.color.a;
    }

    private void SetAlpha(float alpha)
    {
        SetRendererAlpha(_fillImage, alpha);
        SetRendererAlpha(_backgroundImage, alpha);
    }

    private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        var color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private static void KillTween(ref Tweener tween)
    {
        if (tween == null)
        {
            return;
        }

        tween.Kill();
        tween = null;
    }

    #endregion
}
