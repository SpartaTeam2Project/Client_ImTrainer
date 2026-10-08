using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 광선 한 줄기가 쓰는 Image 묶음. 만든 Image는 숨겨 두었다가 다음 광선에서 다시 쓴다.
/// </summary>
public sealed class BeamImagePool
{
    private static readonly Vector2 CENTER = new Vector2(0.5f, 0.5f);

    private readonly RectTransform _parent;
    private readonly List<Image> _images = new List<Image>();
    private int _used;

    public BeamImagePool(RectTransform parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// 다음 Image를 꺼내 그림과 색을 넣는다. 위치, 회전, 크기, 기준점은 처음 상태로 되돌린다.
    /// 나중에 꺼낸 Image가 위에 그려진다.
    /// </summary>
    public Image Get(Sprite sprite, Color color)
    {
        Image image;
        if (_used < _images.Count)
        {
            image = _images[_used];
        }
        else
        {
            image = Create();
            _images.Add(image);
        }

        _used++;
        var rect = image.rectTransform;
        rect.SetAsLastSibling();
        rect.pivot = CENTER;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        image.sprite = sprite;
        image.color = color;
        image.gameObject.SetActive(true);
        return image;
    }

    public void HideAll()
    {
        for (var i = 0; i < _images.Count; i++)
        {
            if (_images[i] != null)
            {
                _images[i].gameObject.SetActive(false);
            }
        }

        _used = 0;
    }

    private Image Create()
    {
        var part = new GameObject("BeamPart", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        part.layer = _parent.gameObject.layer;
        var rect = (RectTransform)part.transform;
        rect.SetParent(_parent, false);
        rect.anchorMin = CENTER;
        rect.anchorMax = CENTER;
        var image = part.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
}
