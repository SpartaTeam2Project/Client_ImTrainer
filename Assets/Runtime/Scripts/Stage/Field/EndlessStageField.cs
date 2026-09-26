using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사방으로 타일을 이어 붙이고 이동을 막지 않는다.
/// </summary>
public class EndlessStageField : StageFieldBehavior
{
    private readonly List<List<StageChunk>> _rows = new List<List<StageChunk>>();

    private List<StageObjectPool<StageChunk>> _pools;

    public override void Init(StageFieldData data, bool spawnProp, StageFieldContext context)
    {
        base.Init(data, spawnProp, context);
        _rows.Clear();
        _pools = CreateChunkPools();
        AddOrigin(Vector3.zero, false);
    }

    public override void Update()
    {
        if (!Context.TryGetView(out var left, out var right, out var top, out var bottom))
        {
            return;
        }

        RemoveInvisible(left, right, top, bottom);
        EnsureOrigin();
        TryAddTop(top);
        TryAddBottom(bottom);
        TryAddLeft(left);
        TryAddRight(right);
    }

    public override void Clear()
    {
        ReleaseAll();
        DestroyPools(_pools);
        _pools = null;
        base.Clear();
    }

    public override Vector2 ClampPosition(Vector2 position)
    {
        return position;
    }

    public override Vector2 GetRandomPositionOnBorder()
    {
        return Vector2.zero;
    }

    public override Vector2 GetIntersectionPoint(Vector2 start, Vector2 end, float offset)
    {
        return end;
    }

    public override bool IsPointOutsideLeft(Vector2 point, out float distance)
    {
        distance = 0f;
        return false;
    }

    public override bool IsPointOutsideRight(Vector2 point, out float distance)
    {
        distance = 0f;
        return false;
    }

    public override bool IsPointOutsideTop(Vector2 point, out float distance)
    {
        distance = 0f;
        return false;
    }

    public override bool IsPointOutsideBottom(Vector2 point, out float distance)
    {
        distance = 0f;
        return false;
    }

    public override void RemovePropsInside(IStageFence fence)
    {
        for (var y = 0; y < _rows.Count; y++)
        {
            var row = _rows[y];
            for (var x = 0; x < row.Count; x++)
            {
                row[x].RemovePropsInside(fence);
            }
        }
    }

    private void RemoveInvisible(float left, float right, float top, float bottom)
    {
        RemoveEdgeRow(top, bottom, left, right, true);
        RemoveEdgeColumn(left, right, top, bottom, true);
        RemoveEdgeColumn(left, right, top, bottom, false);
        RemoveEdgeRow(top, bottom, left, right, false);
    }

    private void RemoveEdgeRow(float top, float bottom, float left, float right, bool topRow)
    {
        if (!HasChunk())
        {
            return;
        }

        var row = topRow ? _rows[0] : _rows[_rows.Count - 1];
        if (row[0].IsVisible(left, right, top, bottom) || row[row.Count - 1].IsVisible(left, right, top, bottom))
        {
            return;
        }

        ReleaseRow(row);
        _rows.RemoveAt(topRow ? 0 : _rows.Count - 1);
    }

    private void RemoveEdgeColumn(float left, float right, float top, float bottom, bool leftColumn)
    {
        if (!HasChunk())
        {
            return;
        }

        var first = leftColumn ? _rows[0][0] : _rows[0][_rows[0].Count - 1];
        var lastRow = _rows[_rows.Count - 1];
        var last = leftColumn ? lastRow[0] : lastRow[lastRow.Count - 1];
        if (first.IsVisible(left, right, top, bottom) || last.IsVisible(left, right, top, bottom))
        {
            return;
        }

        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            var index = leftColumn ? 0 : row.Count - 1;
            row[index].ClearContents();
            row.RemoveAt(index);
        }

        for (var i = _rows.Count - 1; i >= 0; i--)
        {
            if (_rows[i].Count == 0)
            {
                _rows.RemoveAt(i);
            }
        }
    }

    private void EnsureOrigin()
    {
        if (HasChunk())
        {
            return;
        }

        _rows.Clear();
        AddOrigin(Context.PlayerPosition, false);
    }

    private bool HasChunk()
    {
        return _rows.Count > 0 && _rows[0].Count > 0;
    }

    private void AddOrigin(Vector2 position, bool spawnProp)
    {
        var chunk = GetChunk(_pools);
        if (chunk == null)
        {
            return;
        }

        Place(chunk, position);
        if (spawnProp)
        {
            SpawnProp(chunk);
        }

        _rows.Add(new List<StageChunk> { chunk });
    }

    private void TryAddTop(float top)
    {
        if (!HasChunk() || !_rows[0][0].HasEmptyTop(top))
        {
            return;
        }

        var source = _rows[0];
        if (!TryCreateLine(source, Vector3.up, true, out var row))
        {
            return;
        }

        _rows.Insert(0, row);
    }

    private void TryAddBottom(float bottom)
    {
        if (!HasChunk())
        {
            return;
        }

        var source = _rows[_rows.Count - 1];
        if (source.Count == 0 || !source[source.Count - 1].HasEmptyBottom(bottom))
        {
            return;
        }

        if (!TryCreateLine(source, Vector3.down, true, out var row))
        {
            return;
        }

        _rows.Add(row);
    }

    private void TryAddLeft(float left)
    {
        if (!HasChunk() || !_rows[0][0].HasEmptyLeft(left))
        {
            return;
        }

        InsertColumn(Vector3.left, true);
    }

    private void TryAddRight(float right)
    {
        if (!HasChunk())
        {
            return;
        }

        var row = _rows[_rows.Count - 1];
        if (row.Count == 0 || !row[row.Count - 1].HasEmptyRight(right))
        {
            return;
        }

        InsertColumn(Vector3.right, false);
    }

    private void InsertColumn(Vector3 direction, bool insertAtStart)
    {
        var created = new List<StageChunk>(_rows.Count);
        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            var source = insertAtStart ? row[0] : row[row.Count - 1];
            var chunk = CreateBeside(source, direction * (direction.x < 0f ? source.Size.x : source.Size.x));
            if (chunk == null)
            {
                ReleaseRow(created);
                return;
            }

            created.Add(chunk);
        }

        for (var i = 0; i < created.Count; i++)
        {
            if (insertAtStart)
            {
                _rows[i].Insert(0, created[i]);
            }
            else
            {
                _rows[i].Add(created[i]);
            }
        }
    }

    private bool TryCreateLine(List<StageChunk> source, Vector3 direction, bool vertical, out List<StageChunk> created)
    {
        created = new List<StageChunk>(source.Count);
        for (var i = 0; i < source.Count; i++)
        {
            var distance = vertical ? source[i].Size.y : source[i].Size.x;
            var chunk = CreateBeside(source[i], direction * distance);
            if (chunk == null)
            {
                ReleaseRow(created);
                created = null;
                return false;
            }

            created.Add(chunk);
        }

        return true;
    }

    private StageChunk CreateBeside(StageChunk source, Vector3 offset)
    {
        var chunk = GetChunk(_pools);
        if (chunk == null)
        {
            return null;
        }

        Place(chunk, source.transform.position + offset);
        SpawnProp(chunk);
        return chunk;
    }

    private static void ReleaseRow(List<StageChunk> row)
    {
        for (var i = 0; i < row.Count; i++)
        {
            if (row[i] != null)
            {
                row[i].ClearContents();
            }
        }

        row.Clear();
    }

    private void ReleaseAll()
    {
        for (var y = 0; y < _rows.Count; y++)
        {
            ReleaseRow(_rows[y]);
        }

        _rows.Clear();
    }
}
