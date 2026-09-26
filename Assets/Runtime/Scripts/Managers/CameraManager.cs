using UnityEngine;

/// <summary>
/// 게임 씬 카메라를 로컬 플레이어에 붙이고 시야 사각을 제공한다.
/// </summary>
public class CameraManager : BaseManager
{
    private Camera _camera;
    private Transform _cameraTransform;

    public bool HasView => _camera != null && _camera.orthographic;

    public float HalfHeight => HasView ? _camera.orthographicSize : 0f;

    public float HalfWidth => HalfHeight * (_camera != null ? _camera.aspect : 1f);

    public Vector2 Position => _cameraTransform == null ? Vector2.zero : _cameraTransform.position;

    public float LeftBound => Position.x - HalfWidth;

    public float RightBound => Position.x + HalfWidth;

    public float TopBound => Position.y + HalfHeight;

    public float BottomBound => Position.y - HalfHeight;

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
        var targetPosition = target.position;
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

    #endregion
}
