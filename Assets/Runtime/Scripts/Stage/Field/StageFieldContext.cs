using UnityEngine;

/// <summary>
/// 필드가 카메라와 플레이어를 읽을 때 쓰는 참조.
/// </summary>
public sealed class StageFieldContext
{
    public StageFieldContext(CameraManager camera, PlayerManager player, Transform root)
    {
        Camera = camera;
        Player = player;
        Root = root;
    }

    public CameraManager Camera { get; }

    public PlayerManager Player { get; }

    public Transform Root { get; }

    public Vector2 PlayerPosition
    {
        get
        {
            if (Player == null || Player.PlayerTransform == null)
            {
                return Vector2.zero;
            }

            return Player.PlayerTransform.position;
        }
    }

    public bool TryGetView(out float left, out float right, out float top, out float bottom)
    {
        left = 0f;
        right = 0f;
        top = 0f;
        bottom = 0f;
        if (Camera == null || !Camera.HasView)
        {
            return false;
        }

        left = Camera.LeftBound;
        right = Camera.RightBound;
        top = Camera.TopBound;
        bottom = Camera.BottomBound;
        return true;
    }
}
