using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 포켓몬 초상화를 원본 픽셀 비율대로 칸 안에 놓는다. 모든 그림이 같은 배율이라 덩치 차이가 그대로 보인다.
/// 칸은 프리팹에 놓인 Image의 처음 크기와 위치다. 애니메이션 전체가 칸보다 크면 칸에 맞게 줄이고, 칸 한가운데에 놓는다.
/// 프레임마다 잘린 크기가 달라도 스프라이트 피벗(잘리기 전 원본의 가운데)을 같은 자리에 두어서 재생 중에 흔들리지 않는다.
/// </summary>
public class PortraitFitter
{
    private readonly Image _image;
    private bool _hasSlot;
    private Vector2 _slotSize;
    private Vector2 _slotPosition;
    private float _scale;
    // 스프라이트 피벗이 놓일 자리. 칸 가운데에서 잰 UI 좌표다.
    private Vector2 _pivotOffset;

    public PortraitFitter(Image image)
    {
        _image = image;
    }

    /// <summary>
    /// 프레임 묶음 전체로 배율과 피벗 자리를 정한다. 원본 referencePixels 픽셀이 칸의 짧은 변 길이가 되는 배율이다.
    /// 쓸 프레임이 없으면 false라서 칸으로 되돌린다. 그림은 Show로 넣는다.
    /// </summary>
    public bool Fit(Sprite[] frames, float referencePixels)
    {
        if (_image == null || referencePixels <= 0f || !RememberSlot() || !TryGetBounds(frames, out var min, out var max))
        {
            Restore();
            return false;
        }

        var size = max - min;
        // 칸이 세로로 길어도 같은 배율이 되게 짧은 변을 기준으로 삼는다.
        var unit = Mathf.Min(_slotSize.x, _slotSize.y) / referencePixels;
        _scale = Mathf.Min(unit, _slotSize.x / size.x, _slotSize.y / size.y);
        _pivotOffset = -(min + max) * 0.5f * _scale;
        return true;
    }

    /// <summary>
    /// Fit으로 정한 배율과 자리에 프레임 하나를 놓는다. 그림 교체는 하지 않고 크기와 위치만 맞춘다.
    /// </summary>
    public void Show(Sprite frame)
    {
        if (_image == null || frame == null || !_hasSlot || _scale <= 0f)
        {
            return;
        }

        var rect = _image.rectTransform;
        var size = frame.rect.size * _scale;
        var slotCenter = _slotPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, _slotSize);
        var bottomLeft = slotCenter + _pivotOffset - frame.pivot * _scale;
        rect.sizeDelta = size;
        rect.anchoredPosition = bottomLeft + Vector2.Scale(rect.pivot, size);
    }

    /// <summary>
    /// 프리팹에 놓인 원래 크기와 위치로 되돌린다.
    /// </summary>
    public void Restore()
    {
        _scale = 0f;
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

    // 모든 프레임이 차지하는 영역. 스프라이트 피벗에서 잰 원본 픽셀 좌표다.
    private static bool TryGetBounds(Sprite[] frames, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);
        if (frames == null)
        {
            return false;
        }

        var found = false;
        foreach (var frame in frames)
        {
            if (frame == null || frame.rect.width <= 0f || frame.rect.height <= 0f)
            {
                continue;
            }

            min = Vector2.Min(min, -frame.pivot);
            max = Vector2.Max(max, frame.rect.size - frame.pivot);
            found = true;
        }

        return found;
    }
}
