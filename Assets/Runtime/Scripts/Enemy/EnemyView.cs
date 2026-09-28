using UnityEngine;

/// <summary>
/// 적 걷기 프레임을 재생한다. 발사 중이고 사격 그림이 있으면 그 프레임을 쓴다.
/// 8방향 그림은 스폰 때 받은 ScriptableObject만 쓴다.
/// </summary>
public class EnemyView : MonoBehaviour
{
    private const float DEFAULT_FRAMES_PER_SECOND = 8f;
    private const float FLIP_X_EPSILON = 0.0001f;
    private const float SECTOR_DEGREES = 45f;
    private const float SECTOR_HALF_DEGREES = 22.5f;
    private static readonly Color PLACEHOLDER_COLOR = new Color(0.85f, 0.2f, 0.25f, 1f);

    private enum FacingMode
    {
        FlipHorizontal = 0,
        EightDirection = 1
    }

    private enum EightWay
    {
        Right = 0,
        UpRight = 1,
        Up = 2,
        UpLeft = 3,
        Left = 4,
        DownLeft = 5,
        Down = 6,
        DownRight = 7
    }

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private bool _spriteFacesRight = false;

    [Header("Timing")]
    [SerializeField] private float _framesPerSecond = DEFAULT_FRAMES_PER_SECOND;

    [Header("Facing")]
    [SerializeField] private FacingMode _facingMode = FacingMode.FlipHorizontal;

    [Header("Walk")]
    [SerializeField] private Sprite[] _walkSprites;

    private MonsterVisualData _visual;
    private bool _isMoving;
    private bool _isShooting;
    private bool _hasVisual;
    private bool _facesLeft;
    private EightWay _eightWay = EightWay.Right;
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

    #endregion

    #region Public Methods

    /// <summary>
    /// 이번 스폰에 쓸 그림 에셋을 넣는다. 8방향은 이 에셋만 재생한다.
    /// </summary>
    public void ApplyVisual(MonsterVisualData visual)
    {
        _visual = visual;
        _frameIndex = 0;
        _frameTimer = 0f;
        _hasVisual = false;
    }

    /// <summary>
    /// 이동 중이면 걷기 프레임을 돌린다. 8방향 모드는 추적 방향 그림을 고른다.
    /// </summary>
    public void SetVisual(bool isMoving, Vector2 lookDirection, bool shooting = false)
    {
        var directionChanged = ApplyDirection(lookDirection);
        var motionChanged = !_hasVisual || isMoving != _isMoving || shooting != _isShooting;
        _hasVisual = true;
        _isMoving = isMoving;
        _isShooting = shooting;

        if (motionChanged)
        {
            _frameIndex = 0;
            _frameTimer = 0f;
        }
        else if (directionChanged)
        {
            WrapFrameIndex();
        }

        AdvanceFrames();
        ApplyCurrentSprite();
        ApplyFlip();
    }

    #endregion

    #region Private Methods

    private bool ApplyDirection(Vector2 lookDirection)
    {
        if (!UsesEightDirection)
        {
            UpdateFacing(lookDirection);
            return false;
        }

        if (lookDirection.sqrMagnitude <= FLIP_X_EPSILON)
        {
            return false;
        }

        var next = ResolveEightWay(lookDirection);
        if (next == _eightWay)
        {
            return false;
        }

        _eightWay = next;
        return true;
    }

    /// <summary>
    /// 방향이 바뀌어도 걷기 순서는 유지하고, 새 방향 장 수 안으로만 맞춘다.
    /// </summary>
    private void WrapFrameIndex()
    {
        var frames = CurrentFrames();
        if (!HasFrames(frames))
        {
            _frameIndex = 0;
            return;
        }

        _frameIndex %= frames.Length;
    }

    private void UpdateFacing(Vector2 lookDirection)
    {
        if (Mathf.Abs(lookDirection.x) <= FLIP_X_EPSILON)
        {
            return;
        }

        _facesLeft = lookDirection.x < 0f;
    }

    private void AdvanceFrames()
    {
        var frames = CurrentFrames();
        if (!_isMoving || !HasFrames(frames) || frames.Length <= 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        _frameTimer += Time.deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex = (_frameIndex + 1) % frames.Length;
        }
    }

    private void ApplyCurrentSprite()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        var sprite = GetCurrentSprite();
        if (sprite == null)
        {
            _spriteRenderer.sprite = PrototypeSprite.WhiteSquare;
            _spriteRenderer.color = PLACEHOLDER_COLOR;
            return;
        }

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = Color.white;
    }

    private void ApplyFlip()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        if (UsesEightDirection)
        {
            _spriteRenderer.flipX = false;
            return;
        }

        var flipX = _spriteFacesRight ? _facesLeft : !_facesLeft;
        _spriteRenderer.flipX = flipX;
    }

    private Sprite GetCurrentSprite()
    {
        var frames = CurrentFrames();
        if (!HasFrames(frames))
        {
            return null;
        }

        if (!_isMoving || _frameIndex < 0 || _frameIndex >= frames.Length)
        {
            return frames[0];
        }

        return frames[_frameIndex];
    }

    private Sprite[] CurrentFrames()
    {
        if (_isShooting)
        {
            var shoot = GetDirectionFrames(_visual != null ? _visual.Shoot : null, _eightWay);
            if (HasFrames(shoot))
            {
                return shoot;
            }
        }

        return CurrentWalk();
    }

    private Sprite[] CurrentWalk()
    {
        if (UsesEightDirection)
        {
            return GetDirectionFrames(_visual != null ? _visual.Walk : null, _eightWay);
        }

        return _walkSprites;
    }

    private bool UsesEightDirection => _visual != null || _facingMode == FacingMode.EightDirection;

    private static Sprite[] GetDirectionFrames(EightDirectionFrames clip, EightWay way)
    {
        if (clip == null)
        {
            return null;
        }

        switch (way)
        {
            case EightWay.UpRight:
                return FirstFrames(clip.UpRight, clip.Right);
            case EightWay.Up:
                return FirstFrames(clip.Up, clip.Right);
            case EightWay.UpLeft:
                return FirstFrames(clip.UpLeft, clip.Right);
            case EightWay.Left:
                return FirstFrames(clip.Left, clip.Right);
            case EightWay.DownLeft:
                return FirstFrames(clip.DownLeft, clip.Right);
            case EightWay.Down:
                return FirstFrames(clip.Down, clip.Right);
            case EightWay.DownRight:
                return FirstFrames(clip.DownRight, clip.Right);
            default:
                return clip.Right;
        }
    }

    private static Sprite[] FirstFrames(Sprite[] primary, Sprite[] fallback)
    {
        if (HasFrames(primary))
        {
            return primary;
        }

        return fallback;
    }

    private static EightWay ResolveEightWay(Vector2 lookDirection)
    {
        var angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        if (angle < 0f)
        {
            angle += 360f;
        }

        var index = Mathf.FloorToInt((angle + SECTOR_HALF_DEGREES) / SECTOR_DEGREES) % 8;
        return (EightWay)index;
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

    #endregion
}
