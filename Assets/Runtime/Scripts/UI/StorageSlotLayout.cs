using UnityEngine;

/// <summary>
/// 스토리지 칸을 줄 단위로 배치하고, 줄과 칸 사이 이동을 계산한다.
/// </summary>
public static class StorageSlotLayout
{
    /// <summary>
    /// 포커스 칸이 맨 윗줄인지 본다.
    /// </summary>
    public static bool IsTopRow(IStorageSlotList list)
    {
        return list.Count > 0 && list.FocusIndex < ColumnCount(list);
    }

    /// <summary>
    /// 포커스 칸이 줄 오른쪽 끝(또는 마지막 칸)인지 본다. 칸이 없으면 true.
    /// </summary>
    public static bool IsRowEnd(IStorageSlotList list)
    {
        var count = list.Count;
        if (count == 0)
        {
            return true;
        }

        Canvas.ForceUpdateCanvases();
        var columns = ColumnCount(list);
        return list.FocusIndex % columns == columns - 1 || list.FocusIndex >= count - 1;
    }

    /// <summary>
    /// 방향키로 옮겨 갈 칸 번호. 좌우는 줄 안에서 멈추고, 위아래는 칸이 없으면 그대로 둔다.
    /// </summary>
    public static int MoveIndex(IStorageSlotList list, Vector2Int move)
    {
        var count = list.Count;
        var index = list.FocusIndex;
        if (count == 0)
        {
            return index;
        }

        Canvas.ForceUpdateCanvases();
        var columns = ColumnCount(list);
        if (move.x != 0)
        {
            var rowStart = index - index % columns;
            var rowEnd = Mathf.Min(rowStart + columns, count) - 1;
            return Mathf.Clamp(index + move.x, rowStart, rowEnd);
        }

        if (move.y != 0)
        {
            var next = index - move.y * columns;
            if (next >= 0 && next < count)
            {
                return next;
            }
        }

        return index;
    }

    /// <summary>
    /// 칸 크기가 서로 달라도 왼쪽부터 채우고, 넘치면 다음 줄로 넘긴다. GridLayoutGroup은 끄고 간격 값만 쓴다.
    /// </summary>
    public static void Layout(IStorageSlotList list)
    {
        var grid = list.Grid;
        if (grid != null)
        {
            grid.enabled = false;
        }

        var content = list.Content;
        var count = list.Count;
        if (content == null || count == 0)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var padding = grid != null ? grid.padding : new RectOffset();
        var spacing = grid != null ? grid.spacing : Vector2.zero;
        var contentWidth = content.rect.width;
        var innerRight = contentWidth - padding.right;
        var x = (float)padding.left;
        var y = (float)padding.top;
        var rowHeight = 0f;
        var rowStart = 0;

        for (var i = 0; i < count; i++)
        {
            var rect = list.RectAt(i);
            if (rect == null)
            {
                continue;
            }

            var width = rect.sizeDelta.x;
            var height = rect.sizeDelta.y;
            if (x > padding.left && x + width > innerRight)
            {
                AlignRowBottom(list, rowStart, i, y, rowHeight);
                x = padding.left;
                y += rowHeight + spacing.y;
                rowHeight = 0f;
                rowStart = i;
            }

            rect.anchoredPosition = new Vector2(x, -y);
            x += width + spacing.x;
            rowHeight = Mathf.Max(rowHeight, height);
        }

        AlignRowBottom(list, rowStart, count, y, rowHeight);

        var size = content.sizeDelta;
        size.y = y + rowHeight + padding.bottom;
        content.sizeDelta = size;
    }

    public static int ColumnCount(IStorageSlotList list)
    {
        if (list.Count == 0)
        {
            return 1;
        }

        var slot = list.RectAt(0);
        var content = list.Content;
        var grid = list.Grid;
        if (slot == null || content == null)
        {
            return 1;
        }

        var spacing = grid != null ? grid.spacing.x : 0f;
        var padding = grid != null ? grid.padding.left + grid.padding.right : 0;
        var stride = slot.sizeDelta.x + spacing;
        if (stride <= 0.01f)
        {
            return 1;
        }

        var inner = content.rect.width - padding + spacing;
        return Mathf.Max(1, Mathf.FloorToInt((inner + 0.001f) / stride));
    }

    /// <summary>
    /// 한 줄의 칸을 줄 아랫변에 붙인다. 칸 아랫변이 발끝이라 키가 달라도 발이 한 선에 선다.
    /// </summary>
    private static void AlignRowBottom(IStorageSlotList list, int start, int end, float rowTop, float rowHeight)
    {
        for (var i = start; i < end; i++)
        {
            var rect = list.RectAt(i);
            if (rect == null)
            {
                continue;
            }

            var position = rect.anchoredPosition;
            position.y = -(rowTop + rowHeight - rect.sizeDelta.y);
            rect.anchoredPosition = position;
        }
    }
}
