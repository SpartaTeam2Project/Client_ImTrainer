using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좌우로만 타일을 이어 붙이고 위아래는 변으로 막는다.
/// </summary>
public class HorizontalStageField : StageFieldBehavior
{
    private readonly List<StageChunk> _chunks = new List<StageChunk>();

    private List<StageObjectPool<StageChunk>> _pools;
    private StageObjectPool<Transform> _topPool;
    private StageObjectPool<Transform> _bottomPool;

    public override void Init(StageFieldData data, bool spawnProp, StageFieldContext context)
    {
        base.Init(data, spawnProp, context);
        _chunks.Clear();
        _pools = CreateChunkPools();
        _topPool = CreateBorderPool(data.TopPrefab);
        _bottomPool = CreateBorderPool(data.BottomPrefab);
        AddChunk(Vector3.zero, null, false);
    }

    public override void Update()
    {
        if (!Context.TryGetView(out var left, out var right, out _, out _))
        {
            return;
        }

        RemoveInvisible();
        EnsureOrigin();
        TryAdd(right, true);
        TryAdd(left, false);
    }

    public override void Clear()
    {
        ReleaseAll();
        DestroyPools(_pools);
        _pools = null;
        _topPool?.DestroyAll();
        _bottomPool?.DestroyAll();
        _topPool = null;
        _bottomPool = null;
        base.Clear();
    }

    public override Vector2 ClampPosition(Vector2 position)
    {
        if (_chunks.Count == 0)
        {
            return position;
        }

        var chunk = _chunks[0];
        var bottom = chunk.transform.position.y - chunk.Size.y / 2f - Data.BottomMargin;
        var top = chunk.transform.position.y + chunk.Size.y / 2f + Data.TopMargin;
        position.y = Mathf.Clamp(position.y, bottom, top);
        return position;
    }

    public override Vector2 GetRandomPositionOnBorder()
    {
        if (_chunks.Count == 0 || Context.Camera == null)
        {
            return Context.PlayerPosition;
        }

        var chunk = _chunks[0];
        var y = Random.Range(-chunk.Size.y / 2f - Data.BottomMargin, chunk.Size.y / 2f + Data.TopMargin) + chunk.transform.position.y;
        var sign = Random.Range(0, 2) * 2 - 1;
        var x = Context.PlayerPosition.x + Context.Camera.HalfWidth * BorderViewScale * sign;
        return new Vector2(x, y);
    }

    public override Vector2 GetIntersectionPoint(Vector2 start, Vector2 end, float offset)
    {
        if (_chunks.Count == 0)
        {
            return end;
        }

        var chunk = _chunks[0];
        var rect = new Rect(
            chunk.LeftBound + offset,
            chunk.BottomBound + offset,
            (chunk.Size.x - offset) * 2f,
            (chunk.Size.y - offset) * 2f);
        return ExitPoint(start, end, rect, false, true);
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
        if (_chunks.Count == 0)
        {
            distance = 0f;
            return false;
        }

        return Outside(point.y, _chunks[0].TopBound + Data.TopMargin, true, out distance);
    }

    public override bool IsPointOutsideBottom(Vector2 point, out float distance)
    {
        if (_chunks.Count == 0)
        {
            distance = 0f;
            return false;
        }

        return Outside(point.y, _chunks[0].BottomBound - Data.BottomMargin, false, out distance);
    }

    public override void RemovePropsInside(IStageFence fence)
    {
        for (var i = 0; i < _chunks.Count; i++)
        {
            _chunks[i].RemovePropsInside(fence);
        }
    }

    private void RemoveInvisible()
    {
        if (!Context.TryGetView(out var left, out var right, out var top, out var bottom))
        {
            return;
        }

        if (_chunks.Count > 0 && !_chunks[0].IsVisible(left, right, top, bottom))
        {
            _chunks[0].ClearContents();
            _chunks.RemoveAt(0);
        }

        if (_chunks.Count > 0 && !_chunks[_chunks.Count - 1].IsVisible(left, right, top, bottom))
        {
            var last = _chunks.Count - 1;
            _chunks[last].ClearContents();
            _chunks.RemoveAt(last);
        }
    }

    private void EnsureOrigin()
    {
        if (_chunks.Count > 0)
        {
            return;
        }

        AddChunk(Context.PlayerPosition, null, false);
    }

    private void TryAdd(float edge, bool rightSide)
    {
        if (_chunks.Count == 0)
        {
            return;
        }

        var source = rightSide ? _chunks[0] : _chunks[_chunks.Count - 1];
        var empty = rightSide ? source.HasEmptyRight(edge) : source.HasEmptyLeft(edge);
        if (!empty)
        {
            return;
        }

        var direction = rightSide ? Vector3.right : Vector3.left;
        AddChunk(source.transform.position + direction * source.Size.x, rightSide ? 0 : (int?)null, true);
    }

    private void AddChunk(Vector3 position, int? insertAt, bool spawnProp)
    {
        var chunk = GetChunk(_pools);
        if (chunk == null)
        {
            return;
        }

        Place(chunk, position);
        AddBorder(chunk, _topPool, Vector3.up * chunk.Size.y);
        AddBorder(chunk, _bottomPool, Vector3.down * chunk.Size.y);
        if (spawnProp)
        {
            SpawnProp(chunk);
        }

        if (insertAt.HasValue)
        {
            _chunks.Insert(insertAt.Value, chunk);
            return;
        }

        _chunks.Add(chunk);
    }

    private static void AddBorder(StageChunk chunk, StageObjectPool<Transform> pool, Vector3 offset)
    {
        if (pool == null)
        {
            return;
        }

        var border = pool.Get();
        if (border == null)
        {
            return;
        }

        border.SetPositionAndRotation(chunk.transform.position + offset, Quaternion.identity);
        border.localScale = Vector3.one;
        chunk.AddBorder(border);
    }

    private StageObjectPool<Transform> CreateBorderPool(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        return new StageObjectPool<Transform>(prefab, Context.Root);
    }

    private void ReleaseAll()
    {
        for (var i = 0; i < _chunks.Count; i++)
        {
            _chunks[i].ClearContents();
        }

        _chunks.Clear();
    }
}
