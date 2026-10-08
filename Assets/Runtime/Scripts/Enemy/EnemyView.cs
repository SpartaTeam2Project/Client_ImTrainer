using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 적 걷기 프레임을 재생한다. 발사 중이고 사격 그림이 있으면 그 프레임을 쓴다.
/// 8방향 그림은 스폰 때 받은 종의 MonsterAnimationSet만 쓴다.
/// </summary>
public class EnemyView : MonoBehaviour
{
    private const float DEFAULT_FRAMES_PER_SECOND = 8f;
    private const float STRIKE_FRAMES_PER_SECOND = 12f;
    private const float POSE_FRAMES_PER_SECOND = 16f;
    private const float HURT_HOLD_SECONDS = 0.45f;
    private const float HURT_SHORT_LIMIT_SECONDS = 1f;
    private const float HURT_MIN_SECONDS = 1.4f;
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
    private MonsterAnimationSet _animations;
    private bool _isMoving;
    private bool _isShooting;
    private bool _holdLastFrame;
    private bool _isAttacking;
    private bool _isStriking;
    private bool _lungeUsesAttack;
    private bool _isPosing;
    private bool _poseFacesLeft;
    private bool _useIdleMove;
    private bool _isCharging;
    private bool _hasVisual;
    private bool _facesLeft;
    private EightWay _eightWay = EightWay.Right;
    private int _frameIndex;
    private float _frameTimer;
    private int _hurtPlayId;
    private bool _hurtPlaying;
    private CircleCollider2D _hitCollider;
    private Material _defaultMaterial;

    #region Unity Methods

    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer != null)
        {
            _defaultMaterial = _spriteRenderer.sharedMaterial;
        }

        _hitCollider = GetComponent<CircleCollider2D>();
        if (_hitCollider != null)
        {
            _hitCollider.isTrigger = true;
        }

        var body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 이번 스폰에 쓸 그림 에셋을 넣는다. 8방향은 이 에셋만 재생한다.
    /// </summary>
    public void ApplyVisual(MonsterVisualData visual)
    {
        CancelHurt();
        _visual = visual;
        _animations = MonsterAnimationLoader.Get(visual);
        ApplyBodyMaterial(visual);
        _frameIndex = 0;
        _frameTimer = 0f;
        _hasVisual = false;
        FitHitCollider(FindLargestSprite(_animations));
    }

    /// <summary>
    /// hurt 칸에 스프라이트가 있으면 true.
    /// </summary>
    public bool HasHurtSprite => _animations != null && _animations.Hurt != null && _animations.Hurt.HasFrames();

    /// <summary>
    /// 지금 방향의 hurt 프레임을 한 번 재생하고 마지막 장을 잠시 유지한다.
    /// </summary>
    public async UniTask PlayHurtAsync()
    {
        var frames = GetDirectionFrames(_animations != null ? _animations.Hurt : null, _eightWay);
        if (!HasFrames(frames))
        {
            return;
        }

        var playId = ++_hurtPlayId;
        _hurtPlaying = true;
        var frameDuration = 1f / Mathf.Max(1f, _framesPerSecond);
        var frameSeconds = frames.Length * frameDuration;
        var totalSeconds = frameSeconds + HURT_HOLD_SECONDS;
        if (totalSeconds <= HURT_SHORT_LIMIT_SECONDS)
        {
            totalSeconds = HURT_MIN_SECONDS;
        }

        var holdSeconds = Mathf.Max(0f, totalSeconds - frameSeconds);
        for (var i = 0; i < frames.Length; i++)
        {
            if (playId != _hurtPlayId)
            {
                return;
            }

            ShowSprite(frames[i]);
            await UniTask.Delay(System.TimeSpan.FromSeconds(frameDuration));
        }

        if (playId != _hurtPlayId)
        {
            return;
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(holdSeconds));
        if (playId == _hurtPlayId)
        {
            _hurtPlaying = false;
        }
    }

    /// <summary>
    /// 이동 중이면 걷기 프레임을 돌린다. 차지는 차지 그림, 공격은 공격 그림을 고른다.
    /// </summary>
    public void SetVisual(bool isMoving, Vector2 lookDirection, bool shooting = false, bool attacking = false, bool charging = false, bool striking = false, bool posing = false)
    {
        if (_hurtPlaying)
        {
            return;
        }

        var directionChanged = ApplyDirection(lookDirection);
        var motionChanged = !_hasVisual
            || isMoving != _isMoving
            || shooting != _isShooting
            || attacking != _isAttacking
            || charging != _isCharging
            || striking != _isStriking
            || posing != _isPosing;
        var strikeContinues = _isStriking && striking;
        var poseContinues = _isPosing && posing;
        _hasVisual = true;
        _isMoving = isMoving;
        _isShooting = shooting;
        _isAttacking = attacking;
        _isCharging = charging;
        _isStriking = striking;
        _isPosing = posing;

        if (motionChanged && !strikeContinues && !poseContinues)
        {
            _frameIndex = 0;
            _frameTimer = 0f;
            _holdLastFrame = false;
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

    /// <summary>
    /// 풀에서 다른 종으로 다시 쓰일 수 있으니 매번 종의 몸 머티리얼이나 처음 머티리얼로 맞춘다.
    /// </summary>
    private void ApplyBodyMaterial(MonsterVisualData visual)
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        var material = visual != null ? visual.BodyMaterial : null;
        _spriteRenderer.sharedMaterial = material != null ? material : _defaultMaterial;
    }

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
        var playing = _isMoving || _isAttacking || _isCharging || _isShooting || _isStriking || _isPosing;
        if (!playing || !HasFrames(frames) || frames.Length <= 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        if (_isPosing && _frameIndex >= frames.Length - 1)
        {
            _frameTimer += Time.deltaTime;
            _frameIndex = frames.Length - 1;
            return;
        }

        if ((_holdLastFrame || _isStriking) && _frameIndex >= frames.Length - 1)
        {
            _frameIndex = frames.Length - 1;
            return;
        }

        _frameTimer += Time.deltaTime;
        var framesPerSecond = _framesPerSecond;
        if (_isStriking)
        {
            framesPerSecond = STRIKE_FRAMES_PER_SECOND;
        }
        else if (_isPosing)
        {
            framesPerSecond = POSE_FRAMES_PER_SECOND;
        }
        var frameDuration = 1f / framesPerSecond;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            if ((_holdLastFrame || _isStriking || _isPosing) && _frameIndex >= frames.Length - 1)
            {
                _frameIndex = frames.Length - 1;
                if (!_isPosing)
                {
                    _frameTimer = 0f;
                }

                break;
            }

            _frameIndex = (_frameIndex + 1) % frames.Length;
        }
    }

    private void CancelHurt()
    {
        _hurtPlayId++;
        _hurtPlaying = false;
    }

    private void ShowSprite(Sprite sprite)
    {
        if (_spriteRenderer == null || sprite == null)
        {
            return;
        }

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = Color.white;
        _spriteRenderer.flipX = false;
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

    /// <summary>
    /// 맞는 판정을 걷기 그림 중 가장 큰 장에 한 번만 맞춘다. 걷기가 비면 공격, 피격, 사격 그림을 본다.
    /// 프레임마다 바꾸면 겹침이 끊긴다.
    /// </summary>
    private void FitHitCollider(Sprite sprite)
    {
        if (_hitCollider == null || sprite == null)
        {
            return;
        }

        var bounds = BodyBounds(sprite);
        _hitCollider.offset = bounds.center;
        _hitCollider.radius = Mathf.Max(bounds.extents.x, bounds.extents.y);
    }

    // 칸 크기 격자로 자른 시트는 sprite.bounds가 빈 여백까지 포함한 칸 전체다.
    // Tight 메시가 잘라 낸 그림 영역(textureRect)으로 몸 크기를 잰다. 아틀라스에 Tight로 묶이면 textureRect를 못 써서 bounds를 쓴다.
    private static Bounds BodyBounds(Sprite sprite)
    {
        if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
        {
            return sprite.bounds;
        }

        var size = sprite.textureRect.size;
        var center = sprite.textureRectOffset + size * 0.5f - sprite.pivot;
        return new Bounds(center / sprite.pixelsPerUnit, size / sprite.pixelsPerUnit);
    }

    private static Sprite FindLargestSprite(MonsterAnimationSet visual)
    {
        if (visual == null)
        {
            return null;
        }

        Sprite largest = null;
        var largestSize = 0f;
        // 걷기 그림이 몸 크기 기준이다. 격자로 자른 시트는 공격, 피격 장에서 몸이 칸 안을 움직여서 판정이 한쪽으로 쏠린다.
        Consider(visual.Walk, ref largest, ref largestSize);
        if (largest != null)
        {
            return largest;
        }

        Consider(visual.Attack, ref largest, ref largestSize);
        Consider(visual.Hurt, ref largest, ref largestSize);
        Consider(visual.Shoot, ref largest, ref largestSize);
        return largest;
    }

    private static void Consider(EightDirectionFrames clip, ref Sprite largest, ref float largestSize)
    {
        if (clip == null)
        {
            return;
        }

        Consider(clip.Down, ref largest, ref largestSize);
        Consider(clip.DownRight, ref largest, ref largestSize);
        Consider(clip.Right, ref largest, ref largestSize);
        Consider(clip.UpRight, ref largest, ref largestSize);
        Consider(clip.Up, ref largest, ref largestSize);
        Consider(clip.UpLeft, ref largest, ref largestSize);
        Consider(clip.Left, ref largest, ref largestSize);
        Consider(clip.DownLeft, ref largest, ref largestSize);
    }

    private static void Consider(Sprite[] frames, ref Sprite largest, ref float largestSize)
    {
        if (frames == null)
        {
            return;
        }

        for (var i = 0; i < frames.Length; i++)
        {
            var sprite = frames[i];
            if (sprite == null)
            {
                continue;
            }

            var extents = BodyBounds(sprite).extents;
            var size = Mathf.Max(extents.x, extents.y);
            if (size <= largestSize)
            {
                continue;
            }

            largestSize = size;
            largest = sprite;
        }
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

        var playing = _isMoving || _isAttacking || _isCharging || _isShooting || _isStriking || _isPosing;
        if (!playing || _frameIndex < 0 || _frameIndex >= frames.Length)
        {
            return frames[0];
        }

        return frames[_frameIndex];
    }

    private Sprite[] CurrentFrames()
    {
        if (_isPosing)
        {
            var pose = PoseFrames();
            if (HasFrames(pose))
            {
                return pose;
            }
        }

        if (_isCharging)
        {
            var charge = GetDirectionFrames(_animations != null ? _animations.Charge : null, _eightWay);
            if (HasFrames(charge))
            {
                return charge;
            }
        }

        if (_isStriking)
        {
            var strike = StrikeFrames();
            if (HasFrames(strike))
            {
                return strike;
            }
        }

        if (_isAttacking)
        {
            var attack = GetDirectionFrames(_animations != null ? _animations.Attack : null, _eightWay);
            if (HasFrames(attack))
            {
                return attack;
            }
        }

        if (_isShooting)
        {
            var shoot = GetDirectionFrames(_animations != null ? _animations.Shoot : null, _eightWay);
            if (HasFrames(shoot))
            {
                return shoot;
            }
        }

        return CurrentWalk();
    }

    /// <summary>
    /// 평소 이동에 걷기 대신 아이들 장을 쓴다. 스킬 이동은 끄고 걷기를 재생한다.
    /// </summary>
    public void SetIdleMove(bool enabled)
    {
        _useIdleMove = enabled;
    }

    /// <summary>
    /// 플레이어가 있는 좌우의 포즈 0~3을 한 번 재생한다.
    /// </summary>
    public void PlayPose(Vector2 lookDirection)
    {
        if (Mathf.Abs(lookDirection.x) > FLIP_X_EPSILON)
        {
            _poseFacesLeft = lookDirection.x < 0f;
        }

        SetVisual(false, lookDirection, posing: true);
    }

    /// <summary>
    /// 포즈 마지막 장을 한 장 시간만큼 보여 준 뒤인가.
    /// </summary>
    public bool IsPoseFinished
    {
        get
        {
            var frames = PoseFrames();
            if (!HasFrames(frames) || frames.Length <= 1)
            {
                return true;
            }

            var duration = 1f / POSE_FRAMES_PER_SECOND;
            return _frameIndex >= frames.Length - 1 && _frameTimer >= duration;
        }
    }

    /// <summary>
    /// 지금 방향의 Strike를 한 번 재생한다. 마지막 장에 닿으면 그 장을 유지한다.
    /// </summary>
    public void PlayStrike(Vector2 lookDirection, bool isMoving)
    {
        _lungeUsesAttack = false;
        SetVisual(isMoving, lookDirection, false, false, false, true);
    }

    /// <summary>
    /// 몸통박치기와 같이 움직이되, Strike 대신 지금 방향의 Attack을 한 번 재생한다.
    /// </summary>
    public void PlayAttackLunge(Vector2 lookDirection, bool isMoving)
    {
        _lungeUsesAttack = true;
        SetVisual(isMoving, lookDirection, false, false, false, true);
    }

    /// <summary>
    /// Strike를 첫 장부터 마지막 장까지 재생했는가.
    /// </summary>
    public bool IsStrikeFinished
    {
        get
        {
            var frames = StrikeFrames();
            return !HasFrames(frames) || _frameIndex >= frames.Length - 1;
        }
    }

    /// <summary>
    /// Strike 마지막 장에서 멈춘다.
    /// </summary>
    public void HoldStrike(Vector2 lookDirection)
    {
        _lungeUsesAttack = false;
        HoldLungeFrame(lookDirection);
    }

    /// <summary>
    /// Attack 마지막 장에서 멈춘다.
    /// </summary>
    public void HoldAttackLunge(Vector2 lookDirection)
    {
        _lungeUsesAttack = true;
        HoldLungeFrame(lookDirection);
    }

    private void HoldLungeFrame(Vector2 lookDirection)
    {
        SetVisual(false, lookDirection, false, false, false, true);
        var frames = StrikeFrames();
        if (!HasFrames(frames))
        {
            return;
        }

        _frameIndex = frames.Length - 1;
        _frameTimer = 0f;
        _holdLastFrame = true;
        ApplyCurrentSprite();
    }

    private Sprite[] StrikeFrames()
    {
        if (_lungeUsesAttack)
        {
            return GetDirectionFrames(_animations != null ? _animations.Attack : null, _eightWay);
        }

        var strike = GetDirectionFrames(_animations != null ? _animations.Strike : null, _eightWay);
        if (HasFrames(strike))
        {
            return strike;
        }

        return GetDirectionFrames(_animations != null ? _animations.Attack : null, _eightWay);
    }

    /// <summary>
    /// 지금 방향 공격 그림의 끝 프레임만 한 번 재생하고, 그 시간을 반환한다.
    /// </summary>
    public float BeginAttackTail(Vector2 lookDirection, int tailCount)
    {
        SetVisual(false, lookDirection, false, true);
        var frames = GetDirectionFrames(_animations != null ? _animations.Attack : null, _eightWay);
        if (!HasFrames(frames) || _framesPerSecond <= 0f)
        {
            return 0f;
        }

        var count = Mathf.Clamp(tailCount, 1, frames.Length);
        _frameIndex = frames.Length - count;
        _frameTimer = 0f;
        _holdLastFrame = true;
        return count / _framesPerSecond;
    }

    private Sprite[] PoseFrames()
    {
        if (_animations == null)
        {
            return null;
        }

        return _poseFacesLeft ? _animations.PoseLeft : _animations.PoseRight;
    }

    private Sprite[] CurrentWalk()
    {
        if (UsesEightDirection)
        {
            if (_useIdleMove && _animations != null && _animations.Idle != null && _animations.Idle.HasFrames())
            {
                return GetDirectionFrames(_animations.Idle, _eightWay);
            }

            return GetDirectionFrames(_animations != null ? _animations.Walk : null, _eightWay);
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
