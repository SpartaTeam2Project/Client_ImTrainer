using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 위아래로만 타일을 이어 붙이고 좌우는 변으로 막는다.
/// </summary>
public class VerticalStageField : StageFieldBehavior
{
    private readonly List<StageChunk> _chunks = new List<StageChunk>();

    private List<StageObjectPool<StageChunk>> _pools;
    private StageObjectPool<Transform> _leftPool;
    private StageObjectPool<Transform> _rightPool;

    public override void Init(StageFieldData data, bool spawnProp, StageFieldContext context)
    {
        base.Init(data, spawnProp, context);
        _chunks.Clear();
        _pools = CreateChunkPools();
        _leftPool = CreateBorderPool(data.LeftPrefab);
        _rightPool = CreateBorderPool(data.RightPrefab);
        AddChunk(Vector3.zero, null, false);
    }

    public override void Update()
    {
        if (!Context.TryGetView(out _, out _, out var top, out var bottom))
        {
            return;
        }

        RemoveInvisible();
        EnsureOrigin();
        TryAdd(top, true);
        TryAdd(bottom, false);
    }

    public override void Clear()
    {
        ReleaseAll();
        DestroyPools(_pools);
        _pools = null;
        _leftPool?.DestroyAll();
        _rightPool?.DestroyAll();
        _leftPool = null;
        _rightPool = null;
        base.Clear();
    }

    public override Vector2 ClampPosition(Vector2 position)
    {
        if (_chunks.Count == 0)
        {
            return position;
        }

        var chunk = _chunks[0];
        var left = chunk.transform.position.x - chunk.Size.x / 2f - Data.LeftMargin;
        var right = chunk.transform.position.x + chunk.Size.x / 2f + Data.RightMargin;
        position.x = Mathf.Clamp(position.x, left, right);
        return position;
    }

    public override Vector2 GetRandomPositionOnBorder()
    {
        if (_chunks.Count == 0 || Context.Camera == null)
        {
            return Context.PlayerPosition;
        }

        var chunk = _chunks[0];
        var x = Random.Range(-chunk.Size.x / 2f - Data.LeftMargin, chunk.Size.x / 2f + Data.RightMargin) + chunk.transform.position.x;
        var sign = Random.Range(0, 2) * 2 - 1;
        var y = Context.PlayerPosition.y + Context.Camera.HalfHeight * BorderViewScale * sign;
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
        return ExitPoint(start, end, rect, true, false);
    }

    public override bool IsPointOutsideLeft(Vector2 point, out float distance)
    {
        if (_chunks.Count == 0)
        {
            distance = 0f;
            return false;
        }

        return Outside(point.x, _chunks[0].LeftBound - Data.LeftMargin, false, out distance);
    }

    public override bool IsPointOutsideRight(Vector2 point, out float distance)
    {
        if (_chunks.Count == 0)
        {
            distance = 0f;
            return false;
        }

        return Outside(point.x, _chunks[0].RightBound + Data.RightMargin, true, out distance);
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

    private void TryAdd(float edge, bool top)
    {
        if (_chunks.Count == 0)
        {
            return;
        }

        var source = top ? _chunks[0] : _chunks[_chunks.Count - 1];
        var empty = top ? source.HasEmptyTop(edge) : source.HasEmptyBottom(edge);
        if (!empty)
        {
            return;
        }

        var direction = top ? Vector3.up : Vector3.down;
        AddChunk(source.transform.position + direction * source.Size.y, top ? 0 : (int?)null, true);
    }

    private void AddChunk(Vector3 position, int? insertAt, bool spawnProp)
    {
        var chunk = GetChunk(_pools);
        if (chunk == null)
        {
            return;
        }

        Place(chunk, position);
        AddBorder(chunk, _leftPool, Vector3.left * chunk.Size.x);
        AddBorder(chunk, _rightPool, Vector3.right * chunk.Size.x);
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
