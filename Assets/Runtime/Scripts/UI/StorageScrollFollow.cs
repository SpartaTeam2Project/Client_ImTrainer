using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 키보드로 포커스한 칸이 보이도록 스크롤을 옮기고, 그동안 마우스 호버를 막는다.
/// </summary>
public sealed class StorageScrollFollow
{
    private const float SCROLL_FOLLOW_DURATION = 0.1f;
    // 포커스 테두리가 칸 밖으로 나오는 만큼 뷰포트 가장자리에서 띄운다.
    private const float SCROLL_FOLLOW_MARGIN = 10f;

    private Tween _scrollTween;
    // 키보드로 스크롤하면 가만히 있는 커서 아래로 칸이 지나가며 호버가 포커스를 빼앗는다. 커서가 움직일 때까지 호버를 막는다.
    private bool _hoverLocked;
    private Vector2 _hoverLockPointer;

    /// <summary>
    /// 칸이 뷰포트 밖이면 딱 보일 만큼만 스크롤을 부드럽게 옮긴다.
    /// 마우스로 고른 칸은 이미 보이고, 스크롤하면 호버가 바뀌어서 부르지 않는다.
    /// </summary>
    public void ScrollTo(ScrollRect scroll, RectTransform slot, GameObject link)
    {
        if (scroll == null || slot == null || scroll.content == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        var content = scroll.content;
        var corners = new Vector3[4];
        slot.GetWorldCorners(corners);
        var slotBottom = viewport.InverseTransformPoint(corners[0]).y;
        var slotTop = viewport.InverseTransformPoint(corners[1]).y;
        var view = viewport.rect;

        var delta = 0f;
        if (slotTop > view.yMax - SCROLL_FOLLOW_MARGIN)
        {
            delta = slotTop - (view.yMax - SCROLL_FOLLOW_MARGIN);
        }
        else if (slotBottom < view.yMin + SCROLL_FOLLOW_MARGIN)
        {
            delta = slotBottom - (view.yMin + SCROLL_FOLLOW_MARGIN);
        }

        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        // content를 올리면(y +) 아래 칸이 보인다. 칸이 위로 벗어났으면 delta가 양수라서 content를 내린다.
        var maxY = Mathf.Max(0f, content.rect.height - view.height);
        var targetY = Mathf.Clamp(content.anchoredPosition.y - delta, 0f, maxY);

        Stop();
        scroll.StopMovement();
        LockHoverUntilPointerMoves();
        _scrollTween = content.DOAnchorPosY(targetY, SCROLL_FOLLOW_DURATION)
            .SetUpdate(true)
            .SetLink(link);
    }

    public void Stop()
    {
        if (_scrollTween != null)
        {
            _scrollTween.Kill();
            _scrollTween = null;
        }
    }

    /// <summary>
    /// 키보드 스크롤 뒤 커서가 그대로면 true. 커서가 움직이거나 클릭하면 잠금을 푼다.
    /// </summary>
    public bool IsHoverLocked()
    {
        if (!_hoverLocked)
        {
            return false;
        }

        var mouse = Mouse.current;
        if (mouse == null
            || mouse.leftButton.wasReleasedThisFrame
            || (mouse.position.ReadValue() - _hoverLockPointer).sqrMagnitude > 1f)
        {
            _hoverLocked = false;
            return false;
        }

        return true;
    }

    private void LockHoverUntilPointerMoves()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        _hoverLocked = true;
        _hoverLockPointer = mouse.position.ReadValue();
    }
}
