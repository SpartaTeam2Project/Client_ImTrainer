using UnityEngine;

/// <summary>
/// 스테이지에서 PlayerManager가 가진 플레이어 트랜스폼을 따라간다.
/// </summary>
public class StageCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return;
        }

        var target = playerManager.PlayerTransform;
        if (target == null)
        {
            return;
        }

        var position = transform.position;
        var targetPosition = target.position;
        transform.position = new Vector3(targetPosition.x, targetPosition.y, position.z);
    }
}
