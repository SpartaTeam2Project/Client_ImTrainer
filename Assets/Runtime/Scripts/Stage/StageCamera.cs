using UnityEngine;

/// <summary>
/// 게임 씬 카메라를 CameraManager에 등록한다.
/// </summary>
public class StageCamera : MonoBehaviour
{
    private Camera _camera;

    #region Unity Methods

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        Register();
    }

    private void Start()
    {
        Register();
    }

    private void OnDisable()
    {
        if (_camera == null || Managers.Instance == null || !Managers.Instance.TryGetManager<CameraManager>(out var cameraManager))
        {
            return;
        }

        cameraManager.ClearCamera(_camera);
    }

    #endregion

    #region Private Methods

    private void Register()
    {
        if (_camera == null || Managers.Instance == null || !Managers.Instance.TryGetManager<CameraManager>(out var cameraManager))
        {
            return;
        }

        cameraManager.SetCamera(_camera);
    }

    #endregion
}
