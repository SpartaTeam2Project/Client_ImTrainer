using DG.Tweening;
using UnityEngine;

/// <summary>
/// 캐릭터 머리 위 체력바. 숫자는 플레이어가 갖고, 이 컴포넌트는 마스크와 표시만 갱신한다.
/// </summary>
public class HealthbarBehavior : MonoBehaviour
{
    private const float FADE_DURATION = 0.3f;

    [SerializeField] private SpriteRenderer _fillImage;
    [SerializeField] private SpriteRenderer _backgroundImage;
    [SerializeField] private Transform _maskTransform;
    [SerializeField] private float _maskMaxPosition = 0.34f;
    [SerializeField] private float _maskMaxScale = 0.132f;

    private float _maxHealth = 1f;
    private float _currentHealth = 1f;
    private bool _autoShowOnChanged;
    private bool _autoHideWhenMax;
    private bool _isShown;
    private Tweener _fade;

    #region Unity Methods

    private void OnDisable()
    {
        KillFade();
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
    /// 현재 체력과 최대 체력으로 게이지를 다시 그린다.
    /// </summary>
    public void Apply(float current, float max)
    {
        _maxHealth = Mathf.Max(1f, max);
        _currentHealth = Mathf.Clamp(current, 0f, _maxHealth);
        Redraw();

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

    private void Redraw()
    {
        if (_maskTransform == null)
        {
            return;
        }

        var ratio = _currentHealth / _maxHealth;
        var position = _maskTransform.localPosition;
        position.x = -_maskMaxPosition * (1f - ratio);
        _maskTransform.localPosition = position;

        var scale = _maskTransform.localScale;
        scale.x = _maskMaxScale * ratio;
        _maskTransform.localScale = scale;
    }

    private void Show()
    {
        Redraw();
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
        KillFade();
        SetAlpha(0f);
    }

    private void PlayFade(float targetAlpha, bool ignoreTimeScale)
    {
        KillFade();
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

    private void KillFade()
    {
        if (_fade == null)
        {
            return;
        }

        _fade.Kill();
        _fade = null;
    }

    #endregion
}
