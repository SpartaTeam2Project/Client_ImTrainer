using UnityEngine;

/// <summary>
/// 스테이지 배경 타일과 이동 한계를 맡는다.
/// </summary>
public class StageFieldManager : BaseManager
{
    private Transform _root;
    private IStageFieldBehavior _behavior;
    private IStageFence _fence;
    private bool _armed;

    #region Unity Methods

    private void Update()
    {
        if (_behavior == null)
        {
            return;
        }

        if (!_armed)
        {
            _armed = true;
            return;
        }

        _behavior.Update();
    }

    /// <summary>
    /// 매니저가 정리되면 깔아 둔 배경을 치운다.
    /// </summary>
    public override void Cleanup()
    {
        Clear();
        _root = null;
        base.Cleanup();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 게임 씬이 청크를 붙일 부모를 알린다.
    /// </summary>
    public void SetRoot(Transform root)
    {
        if (root == null)
        {
            return;
        }

        _root = root;
    }

    /// <summary>
    /// 같은 부모가 사라지면 배경을 치우고 참조를 비운다.
    /// </summary>
    public void ClearRoot(Transform root)
    {
        if (_root != root)
        {
            return;
        }

        Clear();
        _root = null;
    }

    /// <summary>
    /// 판이 시작되면 필드 종류에 맞춰 배경을 깐다.
    /// </summary>
    public void Begin(StageData stageData)
    {
        Clear();
        if (_root == null || stageData == null || stageData.FieldData == null || !stageData.FieldData.HasBackground)
        {
            return;
        }

        if (Managers.Instance == null
            || !Managers.Instance.TryGetManager<CameraManager>(out var cameraManager)
            || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        _behavior = CreateBehavior(stageData.StageType);
        _behavior.Init(stageData.FieldData, stageData.SpawnProp, new StageFieldContext(cameraManager, playerManager, _root));
        _armed = false;
    }

    /// <summary>
    /// 깔아 둔 배경과 펜스 참조를 치운다.
    /// </summary>
    public void Clear()
    {
        _behavior?.Clear();
        _behavior = null;
        _fence = null;
        _armed = false;
    }

    /// <summary>
    /// 좌표를 필드와 펜스 안으로 되돌린다.
    /// </summary>
    public Vector2 ValidatePosition(Vector2 position)
    {
        if (_behavior != null)
        {
            position = _behavior.ClampPosition(position);
        }

        if (_fence != null)
        {
            position = _fence.ClampPosition(position);
        }

        return position;
    }

    /// <summary>
    /// 필드 경계 위의 임의 좌표.
    /// </summary>
    public Vector2 GetRandomPositionOnBorder()
    {
        return _behavior == null ? Vector2.zero : _behavior.GetRandomPositionOnBorder();
    }

    /// <summary>
    /// 선분이 필드 경계를 나가는 점. 펜스가 있으면 펜스 클램프를 쓴다.
    /// </summary>
    public Vector2 GetIntersectionPoint(Vector2 start, Vector2 end, float offset)
    {
        if (_fence != null)
        {
            return _fence.ClampPosition(end);
        }

        return _behavior == null ? end : _behavior.GetIntersectionPoint(start, end, offset);
    }

    /// <summary>
    /// 점이 왼쪽 경계 밖이면 true.
    /// </summary>
    public bool IsPointOutsideLeft(Vector2 point, out float distance)
    {
        if (_behavior == null)
        {
            distance = 0f;
            return false;
        }

        return _behavior.IsPointOutsideLeft(point, out distance);
    }

    /// <summary>
    /// 점이 오른쪽 경계 밖이면 true.
    /// </summary>
    public bool IsPointOutsideRight(Vector2 point, out float distance)
    {
        if (_behavior == null)
        {
            distance = 0f;
            return false;
        }

        return _behavior.IsPointOutsideRight(point, out distance);
    }

    /// <summary>
    /// 점이 위쪽 경계 밖이면 true.
    /// </summary>
    public bool IsPointOutsideTop(Vector2 point, out float distance)
    {
        if (_behavior == null)
        {
            distance = 0f;
            return false;
        }

        return _behavior.IsPointOutsideTop(point, out distance);
    }

    /// <summary>
    /// 점이 아래쪽 경계 밖이면 true.
    /// </summary>
    public bool IsPointOutsideBottom(Vector2 point, out float distance)
    {
        if (_behavior == null)
        {
            distance = 0f;
            return false;
        }

        return _behavior.IsPointOutsideBottom(point, out distance);
    }

    /// <summary>
    /// 보스 펜스를 이동 한계로 건다.
    /// </summary>
    public void SetFence(IStageFence fence)
    {
        _fence = fence;
    }

    /// <summary>
    /// 보스 펜스 참조를 지운다.
    /// </summary>
    public void ClearFence()
    {
        _fence = null;
    }

    /// <summary>
    /// 펜스 안 장식을 치운다.
    /// </summary>
    public void RemovePropsInsideFence()
    {
        if (_fence == null)
        {
            return;
        }

        _behavior?.RemovePropsInside(_fence);
    }

    #endregion

    #region Private Methods

    private static IStageFieldBehavior CreateBehavior(StageType stageType)
    {
        switch (stageType)
        {
            case StageType.VerticalEndless:
                return new VerticalStageField();
            case StageType.HorizontalEndless:
                return new HorizontalStageField();
            case StageType.Rect:
                return new RectStageField();
            default:
                return new EndlessStageField();
        }
    }

    #endregion
}
