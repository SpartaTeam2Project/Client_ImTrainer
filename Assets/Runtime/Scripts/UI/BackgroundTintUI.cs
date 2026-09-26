using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면을 덮는 딤의 알파를 올린다.
/// </summary>
public class BackgroundTintUI : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField, Range(0f, 1f)] private float _alpha = 0.85f;
    [SerializeField, Range(0f, 1f)] private float _duration = 0.3f;

    private Tweener _alphaTween;

    private void OnDisable()
    {
        KillTween();
    }

    /// <summary>
    /// 딤을 목표 알파까지 올린다.
    /// </summary>
    public void Show(bool instantly = false)
    {
        KillTween();
        if (_image == null)
        {
            return;
        }

        _image.enabled = true;
        if (instantly)
        {
            SetAlpha(_alpha);
            return;
        }

        _alphaTween = _image.DOFade(_alpha, _duration).SetUpdate(true);
    }

    /// <summary>
    /// 딤을 투명하게 만든 뒤 이미지를 끈다.
    /// </summary>
    public void Hide(bool instantly = false)
    {
        KillTween();
        if (_image == null)
        {
            return;
        }

        if (instantly)
        {
            SetAlpha(0f);
            _image.enabled = false;
            return;
        }

        _alphaTween = _image.DOFade(0f, _duration).SetUpdate(true).OnComplete(DisableImage);
    }

    private void DisableImage()
    {
        if (_image == null)
        {
            return;
        }

        _image.enabled = false;
    }

    private void SetAlpha(float alpha)
    {
        var color = _image.color;
        color.a = alpha;
        _image.color = color;
    }

    private void KillTween()
    {
        if (_alphaTween == null)
        {
            return;
        }

        _alphaTween.Kill();
        _alphaTween = null;
    }
}
