using UnityEngine;

/// <summary>
/// 게임 씬 카메라를 로컬 플레이어에 붙이고 시야 사각을 제공한다.
/// </summary>
public class CameraManager : BaseManager
{
    private Camera _camera;
    private Transform _cameraTransform;
    private bool _hasLookPoint;
    private Vector2 _lookPoint;

    public bool HasView => _camera != null && _camera.orthographic;

    public float HalfHeight => HasView ? _camera.orthographicSize : 0f;

    public float HalfWidth => HalfHeight * (_camera != null ? _camera.aspect : 1f);

    public Vector2 Position => _cameraTransform == null ? Vector2.zero : _cameraTransform.position;

    public float LeftBound => Position.x - HalfWidth;

    public float RightBound => Position.x + HalfWidth;

    public float TopBound => Position.y + HalfHeight;

    public float BottomBound => Position.y - HalfHeight;

    public float ViewDiagonal => HasView ? Mathf.Sqrt(HalfWidth * HalfWidth + HalfHeight * HalfHeight) : 0f;

    #region Unity Methods

    private void LateUpdate()
    {
        if (_cameraTransform == null || Managers.Instance == null)
        {
            return;
        }

        if (!Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        var target = playerManager.PlayerTransform;
        if (target == null)
        {
            return;
        }

        var position = _cameraTransform.position;
        var targetPosition = _hasLookPoint ? (Vector3)_lookPoint : target.position;
        _cameraTransform.position = new Vector3(targetPosition.x, targetPosition.y, position.z);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 씬 카메라가 자신을 알린다.
    /// </summary>
    public void SetCamera(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        _camera = camera;
        _cameraTransform = camera.transform;
    }

    /// <summary>
    /// 같은 카메라가 꺼지면 참조만 지운다.
    /// </summary>
    public void ClearCamera(Camera camera)
    {
        if (_camera != camera)
        {
            return;
        }

        _camera = null;
        _cameraTransform = null;
    }

    /// <summary>
    /// 연출 동안 플레이어 대신 이 지점을 본다.
    /// </summary>
    public void SetLookPoint(Vector2 point)
    {
        _hasLookPoint = true;
        _lookPoint = point;
    }

    /// <summary>
    /// 연출이 끝나면 다시 플레이어를 따른다.
    /// </summary>
    public void ClearLookPoint()
    {
        _hasLookPoint = false;
    }

    /// <summary>
    /// 시야 사각형 밖의 임의 점. padding은 경계에서 더 떨어진 거리.
    /// </summary>
    public Vector2 GetRandomPointOutside(float padding)
    {
        if (!HasView)
        {
            return Position;
        }

        var aspect = _camera.aspect;
        if (Random.value > aspect / (aspect + 1f))
        {
            var x = Random.value > 0.5f ? LeftBound - padding : RightBound + padding;
            return new Vector2(x, Random.Range(BottomBound - padding, TopBound + padding));
        }

        var y = Random.value > 0.5f ? TopBound + padding : BottomBound - padding;
        return new Vector2(Random.Range(LeftBound - padding, RightBound + padding), y);
    }

    #endregion
}
