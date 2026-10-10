using UnityEngine;

/// <summary>
/// 플레이어의 이동과 시선을 담당한다.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    private const float MOVE_SQR_EPSILON = 0.0001f;

    private PlayerStat _stat;
    private PlayerHealth _health;
    private PlayerView _view;
    private Vector2 _lookDirection = Vector2.right;
    private bool _isMoving;

    public Vector2 LookDirection => _lookDirection;

    public bool IsMoving => _isMoving;

    #region Public Methods

    /// <summary>
    /// 시선을 오른쪽으로 되돌리고 서 있는 모습으로 맞춘다.
    /// </summary>
    public void Initialize(PlayerStat stat, PlayerHealth health, PlayerView view)
    {
        _stat = stat;
        _health = health;
        _view = view;
        _lookDirection = Vector2.right;
        if (_view != null)
        {
            _view.SetVisual(false, _lookDirection);
        }
    }

    /// <summary>
    /// 서 있는 채로 시선을 바꾼다.
    /// </summary>
    public void Face(Vector2 direction)
    {
        if (!_health.IsAlive || direction.sqrMagnitude <= MOVE_SQR_EPSILON)
        {
            return;
        }

        _lookDirection = direction.normalized;
        if (_view != null)
        {
            _view.SetVisual(false, _lookDirection);
        }
    }

    /// <summary>
    /// 전달받은 이동량으로 움직인다. 입력 출처는 모른다.
    /// </summary>
    public void Move(Vector2 movement)
    {
        if (!_health.IsAlive)
        {
            return;
        }

        var isMoving = movement.sqrMagnitude > MOVE_SQR_EPSILON;
        _isMoving = isMoving;
        if (isMoving)
        {
            _lookDirection = movement.normalized;
        }

        var delta = movement * _stat.moveSpeed * _stat.moveSpeedMultiplier * _stat.moveSlowMultiplier * Time.deltaTime;
        var next = (Vector2)transform.position + delta;
        if (Managers.Instance != null && Managers.Instance.TryGetManager<StageFieldManager>(out var fieldManager))
        {
            next = fieldManager.ValidatePosition(next);
        }

        transform.position = next;
        if (_view != null)
        {
            _view.SetVisual(isMoving, _lookDirection);
        }
    }

    #endregion
}
