using TMPro;
using UnityEngine;

/// <summary>
/// 화면 위 피해 숫자 하나. 앵커와 스케일만 바꾸고 글자는 자식 TMP가 그린다.
/// </summary>
public class TextIndicatorBehavior : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private TMP_Text _textComponent;

    /// <summary>
    /// 표시할 숫자를 넣는다.
    /// </summary>
    public void SetText(string text)
    {
        if (_textComponent == null)
        {
            return;
        }

        _textComponent.text = text;
    }

    /// <summary>
    /// 글자색, 크기, 가로폭을 넣는다. 프리팹 그라데이션은 끈다.
    /// </summary>
    public void SetAppearance(Color color, float fontSize, float width)
    {
        if (_textComponent != null)
        {
            _textComponent.enableVertexGradient = false;
            _textComponent.color = color;
            _textComponent.fontSize = fontSize;
            var textSize = _textComponent.rectTransform.sizeDelta;
            textSize.x = width;
            _textComponent.rectTransform.sizeDelta = textSize;
        }

        if (_rectTransform == null)
        {
            return;
        }

        var size = _rectTransform.sizeDelta;
        size.x = width;
        _rectTransform.sizeDelta = size;
    }

    /// <summary>
    /// 부모 캔버스에서 뷰포트에 해당하는 앵커로 옮긴다.
    /// </summary>
    public void SetAnchors(Vector2 viewportPosition)
    {
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.anchorMin = viewportPosition;
        _rectTransform.anchorMax = viewportPosition;
    }

    /// <summary>
    /// 앵커 기준 위치를 넣는다.
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.anchoredPosition = position;
    }

    /// <summary>
    /// 균등 스케일을 넣는다.
    /// </summary>
    public void SetScale(Vector3 scale)
    {
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.localScale = scale;
    }

    /// <summary>
    /// 떠오르는 동안의 스케일, 위로 올린 거리, 뷰포트 앵커를 한 번에 넣는다.
    /// </summary>
    public void SetAnimationParameters(float scale, float anchoredY, Vector2 viewportPosition)
    {
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.localScale = new Vector3(scale, scale, scale);
        _rectTransform.anchoredPosition = new Vector2(0f, anchoredY);
        _rectTransform.anchorMin = viewportPosition;
        _rectTransform.anchorMax = viewportPosition;
    }
}
