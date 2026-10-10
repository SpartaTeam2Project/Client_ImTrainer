using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 포켓몬 초상화를 원본 픽셀 비율대로 칸 안에 놓는다. 모든 그림이 같은 배율이라 덩치 차이가 그대로 보인다.
/// 칸은 프리팹에 놓인 Image의 처음 크기와 위치다. 칸보다 큰 그림만 칸에 맞게 줄이고, 칸 한가운데에 놓는다.
/// </summary>
public class PortraitFitter
{
    private readonly Image _image;
    private bool _hasSlot;
    private Vector2 _slotSize;
    private Vector2 _slotPosition;

    public PortraitFitter(Image image)
    {
        _image = image;
    }

    /// <summary>
    /// 원본 referencePixels 픽셀이 칸의 짧은 변 길이가 되는 배율로 놓는다. 그림이 없으면 칸으로 되돌린다.
    /// </summary>
    public void Fit(Sprite sprite, float referencePixels)
    {
        if (_image == null || sprite == null || referencePixels <= 0f || !RememberSlot())
        {
            Restore();
            return;
        }

        var size = sprite.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            Restore();
            return;
        }

        // 칸이 세로로 길어도 같은 배율이 되게 짧은 변을 기준으로 삼는다.
        var unit = Mathf.Min(_slotSize.x, _slotSize.y) / referencePixels;
        var scale = Mathf.Min(unit, _slotSize.x / size.x, _slotSize.y / size.y);
        Place(size * scale);
    }

    /// <summary>
    /// 프리팹에 놓인 원래 크기와 위치로 되돌린다.
    /// </summary>
    public void Restore()
    {
        if (_image == null || !_hasSlot)
        {
            return;
        }

        var rect = _image.rectTransform;
        rect.sizeDelta = _slotSize;
        rect.anchoredPosition = _slotPosition;
    }

    private bool RememberSlot()
    {
        if (!_hasSlot)
        {
            var rect = _image.rectTransform;
            _slotSize = rect.sizeDelta;
            _slotPosition = rect.anchoredPosition;
            _hasSlot = _slotSize.x > 0f && _slotSize.y > 0f;
        }

        return _hasSlot;
    }

    // 칸 가운데를 유지한다. 피벗이 어디에 있어도 맞도록 피벗 기준으로 옮긴다.
    private void Place(Vector2 size)
    {
        var rect = _image.rectTransform;
        var position = _slotPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, _slotSize - size);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
