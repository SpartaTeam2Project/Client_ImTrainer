using UnityEngine;

/// <summary>
/// 플레이어가 나갈 수 있는 사각형 한계.
/// </summary>
public class StageField : MonoBehaviour
{
    [SerializeField] private float _halfWidth = 60f;
    [SerializeField] private float _halfHeight = 60f;

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
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        playerManager.ClearStageField(this);
    }

    /// <summary>
    /// 좌표를 필드 안으로 되돌린다.
    /// </summary>
    public Vector2 ValidatePosition(Vector2 position)
    {
        position.x = Mathf.Clamp(position.x, -_halfWidth, _halfWidth);
        position.y = Mathf.Clamp(position.y, -_halfHeight, _halfHeight);
        return position;
    }

    private void Register()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        playerManager.SetStageField(this);
    }
}
