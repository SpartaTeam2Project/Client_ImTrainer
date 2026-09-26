using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타일 하나와 변, 구석으로 고정된 사각형이다.
/// </summary>
public class RectStageField : StageFieldBehavior
{
    private readonly List<GameObject> _borders = new List<GameObject>();

    private List<StageObjectPool<StageChunk>> _pools;
    private StageChunk _chunk;

    public override void Init(StageFieldData data, bool spawnProp, StageFieldContext context)
    {
        base.Init(data, spawnProp, context);
        _pools = CreateChunkPools();
        var chunk = GetChunk(_pools);
        if (chunk == null)
        {
            return;
        }

        _chunk = chunk;
        Place(_chunk, Vector3.zero);
        AddBorder(data.TopPrefab, new Vector3(0f, _chunk.Size.y, 0f));
        AddBorder(data.BottomPrefab, new Vector3(0f, -_chunk.Size.y, 0f));
        AddBorder(data.LeftPrefab, new Vector3(-_chunk.Size.x, 0f, 0f));
        AddBorder(data.RightPrefab, new Vector3(_chunk.Size.x, 0f, 0f));
        AddBorder(data.TopLeftPrefab, new Vector3(-_chunk.Size.x, _chunk.Size.y, 0f));
        AddBorder(data.TopRightPrefab, new Vector3(_chunk.Size.x, _chunk.Size.y, 0f));
        AddBorder(data.BottomLeftPrefab, new Vector3(-_chunk.Size.x, -_chunk.Size.y, 0f));
        AddBorder(data.BottomRightPrefab, new Vector3(_chunk.Size.x, -_chunk.Size.y, 0f));
        SpawnProp(_chunk);
    }

    public override void Update()
    {
    }

    public override void Clear()
    {
        if (_chunk != null)
        {
            _chunk.ClearContents();
            _chunk = null;
        }

        DestroyPools(_pools);
        _pools = null;

        for (var i = 0; i < _borders.Count; i++)
        {
            if (_borders[i] != null)
            {
                Object.Destroy(_borders[i]);
            }
        }

        _borders.Clear();
        base.Clear();
    }

    public override Vector2 ClampPosition(Vector2 position)
    {
        if (_chunk == null)
        {
            return position;
        }

        var minX = _chunk.transform.position.x - _chunk.Size.x / 2f - Data.LeftMargin;
        var maxX = _chunk.transform.position.x + _chunk.Size.x / 2f + Data.RightMargin;
        var minY = _chunk.transform.position.y - _chunk.Size.y / 2f - Data.BottomMargin;
        var maxY = _chunk.transform.position.y + _chunk.Size.y / 2f + Data.TopMargin;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    public override Vector2 GetRandomPositionOnBorder()
    {
        if (_chunk == null)
        {
            return Vector2.zero;
        }

        var left = -_chunk.Size.x / 2f - Data.LeftMargin;
        var right = _chunk.Size.x / 2f + Data.RightMargin;
        var bottom = -_chunk.Size.y / 2f - Data.BottomMargin;
        var top = _chunk.Size.y / 2f + Data.TopMargin;
        var sample = Random.value;
        if (sample <= 0.25f)
        {
            return new Vector2(left, Random.Range(bottom, top));
        }

        if (sample <= 0.5f)
        {
            return new Vector2(right, Random.Range(bottom, top));
        }

        if (sample <= 0.75f)
        {
            return new Vector2(Random.Range(left, right), bottom);
        }

        return new Vector2(Random.Range(left, right), top);
    }

    public override Vector2 GetIntersectionPoint(Vector2 start, Vector2 end, float offset)
    {
        if (_chunk == null)
        {
            return end;
        }

        var rect = new Rect(
            _chunk.LeftBound + offset,
            _chunk.BottomBound + offset,
            (_chunk.Size.x - offset) * 2f,
            (_chunk.Size.y - offset) * 2f);
        return ExitPoint(start, end, rect, true, true);
    }

    public override bool IsPointOutsideLeft(Vector2 point, out float distance)
    {
        if (_chunk == null)
        {
            distance = 0f;
            return false;
        }

        return Outside(point.x, _chunk.LeftBound - Data.LeftMargin, false, out distance);
    }

    public override bool IsPointOutsideRight(Vector2 point, out float distance)
    {
        if (_chunk == null)
        {
            distance = 0f;
            return false;
        }

        return Outside(point.x, _chunk.RightBound + Data.RightMargin, true, out distance);
    }

    public override bool IsPointOutsideTop(Vector2 point, out float distance)
    {
        if (_chunk == null)
        {
            distance = 0f;
            return false;
        }

        return Outside(point.y, _chunk.TopBound + Data.TopMargin, true, out distance);
    }

    public override bool IsPointOutsideBottom(Vector2 point, out float distance)
    {
        if (_chunk == null)
        {
            distance = 0f;
            return false;
        }

        return Outside(point.y, _chunk.BottomBound - Data.BottomMargin, false, out distance);
    }

    public override void RemovePropsInside(IStageFence fence)
    {
        if (_chunk == null)
        {
            return;
        }

        _chunk.RemovePropsInside(fence);
    }

    private void AddBorder(GameObject prefab, Vector3 position)
    {
        if (prefab == null || Context.Root == null)
        {
            return;
        }

        var border = Object.Instantiate(prefab, position, Quaternion.identity, Context.Root);
        _borders.Add(border);
    }
}
