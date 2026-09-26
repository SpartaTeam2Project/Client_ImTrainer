using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 종류별 배경과 이동 한계.
/// </summary>
public interface IStageFieldBehavior
{
    void Init(StageFieldData data, bool spawnProp, StageFieldContext context);

    void Update();

    void Clear();

    Vector2 ClampPosition(Vector2 position);

    Vector2 GetRandomPositionOnBorder();

    Vector2 GetIntersectionPoint(Vector2 start, Vector2 end, float offset);

    bool IsPointOutsideLeft(Vector2 point, out float distance);

    bool IsPointOutsideRight(Vector2 point, out float distance);

    bool IsPointOutsideTop(Vector2 point, out float distance);

    bool IsPointOutsideBottom(Vector2 point, out float distance);

    void RemovePropsInside(IStageFence fence);
}

/// <summary>
/// 배경 풀과 장식 배치를 공유한다.
/// </summary>
public abstract class StageFieldBehavior : IStageFieldBehavior
{
    private const float BORDER_VIEW_SCALE = 1.05f;

    private readonly List<StageObjectPool<StageProp>> _propPools = new List<StageObjectPool<StageProp>>();

    private bool _spawnProp;

    protected StageFieldData Data { get; private set; }

    protected StageFieldContext Context { get; private set; }

    protected float BorderViewScale => BORDER_VIEW_SCALE;

    public virtual void Init(StageFieldData data, bool spawnProp, StageFieldContext context)
    {
        Data = data;
        Context = context;
        _spawnProp = spawnProp && data != null;
        _propPools.Clear();
        if (!_spawnProp || data.PropChances == null)
        {
            return;
        }

        for (var i = 0; i < data.PropChances.Count; i++)
        {
            var propData = data.PropChances[i];
            if (propData == null || propData.Prefab == null)
            {
                _propPools.Add(null);
                continue;
            }

            _propPools.Add(new StageObjectPool<StageProp>(propData.Prefab, context.Root));
        }
    }

    public abstract void Update();

    public virtual void Clear()
    {
        for (var i = 0; i < _propPools.Count; i++)
        {
            _propPools[i]?.DestroyAll();
        }

        _propPools.Clear();
    }

    public abstract Vector2 ClampPosition(Vector2 position);

    public abstract Vector2 GetRandomPositionOnBorder();

    public abstract Vector2 GetIntersectionPoint(Vector2 start, Vector2 end, float offset);

    public abstract bool IsPointOutsideLeft(Vector2 point, out float distance);

    public abstract bool IsPointOutsideRight(Vector2 point, out float distance);

    public abstract bool IsPointOutsideTop(Vector2 point, out float distance);

    public abstract bool IsPointOutsideBottom(Vector2 point, out float distance);

    public abstract void RemovePropsInside(IStageFence fence);

    /// <summary>
    /// 확률에 맞춰 칸 안에 장식을 깐다.
    /// </summary>
    protected void SpawnProp(StageChunk chunk)
    {
        if (!_spawnProp || chunk == null || Data.PropChances == null)
        {
            return;
        }

        for (var i = 0; i < Data.PropChances.Count; i++)
        {
            var propData = Data.PropChances[i];
            if (propData == null || i >= _propPools.Count || _propPools[i] == null)
            {
                continue;
            }

            if (Random.value * 100f >= propData.Chance)
            {
                continue;
            }

            var amount = Mathf.RoundToInt(Mathf.Lerp(1f, propData.MaxAmount, Random.value));
            for (var j = 0; j < amount; j++)
            {
                var prop = _propPools[i].Get();
                chunk.AddProp(prop);
            }
        }
    }

    protected List<StageObjectPool<StageChunk>> CreateChunkPools()
    {
        var pools = new List<StageObjectPool<StageChunk>>();
        var prefabs = Data.GetBackgroundPrefabs();
        for (var i = 0; i < prefabs.Count; i++)
        {
            pools.Add(new StageObjectPool<StageChunk>(prefabs[i], Context.Root));
        }

        return pools;
    }

    protected static StageChunk GetChunk(List<StageObjectPool<StageChunk>> pools)
    {
        if (pools == null || pools.Count == 0)
        {
            return null;
        }

        var chunk = pools[Random.Range(0, pools.Count)].Get();
        if (chunk == null)
        {
            return null;
        }

        if (chunk.Size.x <= 0f || chunk.Size.y <= 0f)
        {
            chunk.gameObject.SetActive(false);
            return null;
        }

        return chunk;
    }

    protected static void Place(StageChunk chunk, Vector3 position)
    {
        chunk.transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), Quaternion.identity);
        chunk.transform.localScale = Vector3.one;
    }

    protected static void DestroyPools(List<StageObjectPool<StageChunk>> pools)
    {
        if (pools == null)
        {
            return;
        }

        for (var i = 0; i < pools.Count; i++)
        {
            pools[i].DestroyAll();
        }

        pools.Clear();
    }

    protected static Vector2 ExitPoint(Vector2 start, Vector2 end, Rect rect, bool useX, bool useY)
    {
        var path = end - start;
        if (path.sqrMagnitude <= Mathf.Epsilon)
        {
            return start;
        }

        var enter = float.NegativeInfinity;
        var exit = float.PositiveInfinity;
        if (useX && Mathf.Abs(path.x) > Mathf.Epsilon)
        {
            var first = (rect.xMin - start.x) / path.x;
            var second = (rect.xMax - start.x) / path.x;
            if (first > second)
            {
                (first, second) = (second, first);
            }

            enter = first;
            exit = second;
        }

        if (useY && Mathf.Abs(path.y) > Mathf.Epsilon)
        {
            var first = (rect.yMin - start.y) / path.y;
            var second = (rect.yMax - start.y) / path.y;
            if (first > second)
            {
                (first, second) = (second, first);
            }

            enter = Mathf.Max(enter, first);
            exit = Mathf.Min(exit, second);
        }

        if (exit < 0f || enter > exit)
        {
            return start;
        }

        return start + path * exit;
    }

    protected static bool Outside(float value, float bound, bool greater, out float distance)
    {
        var outside = greater ? value > bound : value < bound;
        if (!outside)
        {
            distance = 0f;
            return false;
        }

        distance = greater ? value - bound : bound - value;
        return true;
    }
}
