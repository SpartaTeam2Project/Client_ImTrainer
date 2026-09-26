using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배경 타일 하나. 크기는 스프라이트 사이즈다.
/// </summary>
public class StageChunk : MonoBehaviour
{
    private const float REMOVE_PADDING = 2f;

    [SerializeField] private SpriteRenderer _sprite;

    private readonly List<Transform> _borders = new List<Transform>();
    private readonly List<StageProp> _props = new List<StageProp>();

    private void Awake()
    {
        if (_sprite == null)
        {
            _sprite = GetComponent<SpriteRenderer>();
        }
    }

    public Vector2 Size
    {
        get
        {
            if (_sprite == null)
            {
                return Vector2.zero;
            }

            if (_sprite.drawMode != SpriteDrawMode.Simple)
            {
                return _sprite.size;
            }

            return _sprite.sprite == null ? Vector2.zero : (Vector2)_sprite.sprite.bounds.size;
        }
    }

    public float LeftBound => transform.position.x - Size.x / 2f;

    public float RightBound => transform.position.x + Size.x / 2f;

    public float TopBound => transform.position.y + Size.y / 2f;

    public float BottomBound => transform.position.y - Size.y / 2f;

    /// <summary>
    /// 카메라 사각과 겹치면 true. 가장자리는 여유만큼 더 남긴다.
    /// </summary>
    public bool IsVisible(float left, float right, float top, float bottom)
    {
        return !(RightBound < left - REMOVE_PADDING
            || LeftBound > right + REMOVE_PADDING
            || TopBound < bottom - REMOVE_PADDING
            || BottomBound > top + REMOVE_PADDING);
    }

    public bool HasEmptyLeft(float left)
    {
        return LeftBound > left;
    }

    public bool HasEmptyRight(float right)
    {
        return RightBound < right;
    }

    public bool HasEmptyTop(float top)
    {
        return TopBound < top;
    }

    public bool HasEmptyBottom(float bottom)
    {
        return BottomBound > bottom;
    }

    /// <summary>
    /// 이 칸에 붙은 변을 기억한다.
    /// </summary>
    public void AddBorder(Transform border)
    {
        if (border == null)
        {
            return;
        }

        _borders.Add(border);
    }

    /// <summary>
    /// 장식을 칸 안에 무작위로 둔다.
    /// </summary>
    public void AddProp(StageProp prop)
    {
        if (prop == null)
        {
            return;
        }

        _props.Add(prop);
        prop.transform.SetPositionAndRotation(
            new Vector3(Random.Range(LeftBound, RightBound), Random.Range(BottomBound, TopBound), 0f),
            Quaternion.identity);
        prop.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 펜스 안 장식을 끈다.
    /// </summary>
    public void RemovePropsInside(IStageFence fence)
    {
        if (fence == null)
        {
            return;
        }

        for (var i = _props.Count - 1; i >= 0; i--)
        {
            var prop = _props[i];
            if (prop == null || !fence.Contains(prop.transform.position))
            {
                continue;
            }

            prop.gameObject.SetActive(false);
            _props.RemoveAt(i);
        }
    }

    /// <summary>
    /// 변과 장식을 끄고 칸도 끈다. 풀이 비활성 객체를 다시 쓴다.
    /// </summary>
    public void ClearContents()
    {
        for (var i = 0; i < _borders.Count; i++)
        {
            if (_borders[i] != null)
            {
                _borders[i].gameObject.SetActive(false);
            }
        }

        for (var i = 0; i < _props.Count; i++)
        {
            if (_props[i] != null)
            {
                _props[i].gameObject.SetActive(false);
            }
        }

        _borders.Clear();
        _props.Clear();
        gameObject.SetActive(false);
    }
}
