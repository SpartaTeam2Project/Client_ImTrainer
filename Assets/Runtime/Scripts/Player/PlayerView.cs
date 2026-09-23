using UnityEngine;

/// <summary>
/// 플레이어가 바라보는 네 방향.
/// </summary>
public enum PlayerFacing
{
    Down = 0,
    Up = 1,
    Left = 2,
    Right = 3
}

/// <summary>
/// Idle / Walk 스프라이트를 방향에 맞춰 재생한다. 칸이 비면 자리표시 사각형을 쓴다.
/// </summary>
public class PlayerView : MonoBehaviour
{
    private const float DEFAULT_FRAMES_PER_SECOND = 8f;
    private static readonly Color PLACEHOLDER_COLOR = new Color(0.95f, 0.85f, 0.2f, 1f);

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Timing")]
    [SerializeField] private float _framesPerSecond = DEFAULT_FRAMES_PER_SECOND;

    [Header("Idle")]
    [SerializeField] private Sprite[] _idleDown;
    [SerializeField] private Sprite[] _idleUp;
    [SerializeField] private Sprite[] _idleLeft;
    [SerializeField] private Sprite[] _idleRight;

    [Header("Walk")]
    [SerializeField] private Sprite[] _walkDown;
    [SerializeField] private Sprite[] _walkUp;
    [SerializeField] private Sprite[] _walkLeft;
    [SerializeField] private Sprite[] _walkRight;

    private PlayerFacing _facing = PlayerFacing.Right;
    private bool _isMoving;
    private bool _hasVisual;
    private Sprite[] _currentFrames;
    private int _frameIndex;
    private float _frameTimer;

    #region Unity Methods

    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        AdvanceFrames();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 이동 여부와 시선으로 Idle / Walk 클립을 고른다. 멈출 때는 마지막 방향을 유지한다.
    /// </summary>
    public void SetVisual(bool isMoving, Vector2 lookDirection)
    {
        if (isMoving)
        {
            _facing = CalculateFacing(lookDirection);
        }

        var frames = ResolveFrames(isMoving, _facing);
        if (_hasVisual && frames == _currentFrames && isMoving == _isMoving)
        {
            return;
        }

        _hasVisual = true;
        _isMoving = isMoving;
        _currentFrames = frames;
        _frameIndex = 0;
        _frameTimer = 0f;
        ApplyCurrentSprite();
    }

    #endregion

    #region Private Methods

    private void AdvanceFrames()
    {
        if (_currentFrames == null || _currentFrames.Length <= 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        _frameTimer += Time.deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        var changed = false;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex = (_frameIndex + 1) % _currentFrames.Length;
            changed = true;
        }

        if (changed)
        {
            ApplyCurrentSprite();
        }
    }

    private void ApplyCurrentSprite()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        var sprite = GetFrameSprite();
        if (sprite == null)
        {
            _spriteRenderer.sprite = PrototypeSprite.WhiteSquare;
            _spriteRenderer.color = PLACEHOLDER_COLOR;
            return;
        }

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = Color.white;
    }

    private Sprite GetFrameSprite()
    {
        if (_currentFrames == null || _currentFrames.Length == 0)
        {
            return null;
        }

        if (_frameIndex < 0 || _frameIndex >= _currentFrames.Length)
        {
            return null;
        }

        return _currentFrames[_frameIndex];
    }

    private Sprite[] ResolveFrames(bool isMoving, PlayerFacing facing)
    {
        var primary = GetFrames(isMoving, facing);
        if (HasFrames(primary))
        {
            return primary;
        }

        if (isMoving)
        {
            var idle = GetFrames(false, facing);
            if (HasFrames(idle))
            {
                return idle;
            }
        }

        if (HasFrames(_idleDown))
        {
            return _idleDown;
        }

        return null;
    }

    private Sprite[] GetFrames(bool isMoving, PlayerFacing facing)
    {
        if (isMoving)
        {
            switch (facing)
            {
                case PlayerFacing.Down:
                    return _walkDown;
                case PlayerFacing.Up:
                    return _walkUp;
                case PlayerFacing.Left:
                    return _walkLeft;
                default:
                    return _walkRight;
            }
        }

        switch (facing)
        {
            case PlayerFacing.Down:
                return _idleDown;
            case PlayerFacing.Up:
                return _idleUp;
            case PlayerFacing.Left:
                return _idleLeft;
            default:
                return _idleRight;
        }
    }

    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 대각선은 절댓값이 큰 축을 쓴다. 같으면 위아래를 고른다.
    /// </summary>
    private static PlayerFacing CalculateFacing(Vector2 lookDirection)
    {
        if (Mathf.Abs(lookDirection.x) > Mathf.Abs(lookDirection.y))
        {
            return lookDirection.x < 0f ? PlayerFacing.Left : PlayerFacing.Right;
        }

        return lookDirection.y < 0f ? PlayerFacing.Down : PlayerFacing.Up;
    }

    #endregion
}
