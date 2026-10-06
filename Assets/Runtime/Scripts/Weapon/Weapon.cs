using UnityEngine;

/// <summary>
/// 시계 칸에 장착된 포켓몬. 칸을 따라가고 그림만 재생한다.
/// </summary>
public class Weapon : MonoBehaviour
{
    private const float LOOK_AXIS_EPSILON = 0.01f;
    private const int BACK_SORTING_ORDER = 9;
    private const int FRONT_SORTING_ORDER = 11;
    private const float DEFAULT_FRAMES_PER_SECOND = 8f;
    private const float ATTACK_POSE_SECONDS = 0.6f;
    private const float SECTOR_DEGREES = 45f;
    private const float SECTOR_HALF_DEGREES = 22.5f;
    private static readonly Color PLACEHOLDER_COLOR = new Color(0.35f, 0.75f, 0.95f, 1f);

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

    [Header("Idle")]
    [SerializeField] private FacingMode _facingMode = FacingMode.FlipHorizontal;
    [SerializeField] private Sprite[] _idleFrames;
    [SerializeField] private float _framesPerSecond = DEFAULT_FRAMES_PER_SECOND;

    [Header("Stats")]
    [SerializeField] private WeaponStats _stats = new WeaponStats();

    private MonsterVisualData _visual;
    private MonsterAnimationSet _animations;
    private Player _owner;
    private SpriteRenderer _renderer;
    private int _playerId;
    private float _attackFacingTimer;
    private bool _shotPose;
    private bool _facingRight;
    private EightWay _eightWay = EightWay.Right;
    private int _frameIndex;
    private float _frameTimer;
    private bool _fainting;
    private bool _posing;
    private float _resultDelay;
    private bool _inFrontOfOwner;
    private bool _lastSideRight = true;

    public int PlayerId => _playerId;

    public float FollowDistance => _stats != null ? _stats.FollowDistance : 0f;

    #region Unity Methods

    private void Awake()
    {
        CacheRenderer();
        if (HasFrames(CurrentFrames()))
        {
            ShowCurrentFrame();
            return;
        }

        EnsurePlaceholder();
    }

    private void LateUpdate()
    {
        if (_fainting || _posing)
        {
            // 승패 직후 시간이 멈추므로 결과 그림은 스케일을 무시한다.
            if (!ConsumeResultDelay(Time.unscaledDeltaTime))
            {
                return;
            }

            AdvanceResult(Time.unscaledDeltaTime);
            return;
        }

        if (_owner == null)
        {
            return;
        }

        Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_owner == null)
        {
            return;
        }

        var owner = _owner;
        _owner = null;
        owner.Weapons.NotifyWeaponDestroyed(this);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 장착한 플레이어를 연결한다.
    /// </summary>
    public void Bind(Player owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 파괴 알림 없이 연결을 끊는다.
    /// </summary>
    public void Unbind()
    {
        _owner = null;
    }

    /// <summary>
    /// 이번 칸에 쓸 종 그림을 넣는다.
    /// </summary>
    public void ApplyVisual(MonsterVisualData visual)
    {
        _visual = visual;
        _animations = MonsterAnimationLoader.Get(visual);
        _shotPose = false;
        _fainting = false;
        _posing = false;
        _resultDelay = 0f;
        _frameIndex = 0;
        _frameTimer = 0f;
        if (visual == null)
        {
            return;
        }

        var scale = visual.Scale;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>
    /// 시계 칸에 두고 그림 재생을 시작한다. 플레이어 아래쪽 칸은 플레이어 앞에 그린다.
    /// </summary>
    public void Initialize(int playerId, WeaponSlot slot)
    {
        _playerId = playerId;
        if (_stats == null)
        {
            _stats = new WeaponStats();
        }

        _attackFacingTimer = 0f;
        _shotPose = false;
        _fainting = false;
        _posing = false;
        _resultDelay = 0f;
        _lastSideRight = true;
        _facingRight = false;
        _eightWay = EightWay.Right;
        _frameIndex = 0;
        _frameTimer = 0f;
        _inFrontOfOwner = WeaponSlots.IsBelowOwner(slot);
        CacheRenderer();
        if (_renderer != null)
        {
            _renderer.sortingOrder = _inFrontOfOwner ? FRONT_SORTING_ORDER : BACK_SORTING_ORDER;
        }

        if (HasFrames(CurrentFrames()))
        {
            ShowCurrentFrame();
        }
        else if (_renderer != null)
        {
            _renderer.flipX = false;
        }
    }

    /// <summary>
    /// 연출이 멈춘 동안을 빼고 걷기 그림을 재생한다.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (Managers.Instance == null || !Managers.Instance.IsSimulationRunning || WeaponAbilityManager.IsCombatPaused())
        {
            return;
        }

        if (_attackFacingTimer > 0f)
        {
            _attackFacingTimer -= deltaTime;
        }

        if (_attackFacingTimer <= 0f)
        {
            _shotPose = false;
            if (_owner != null)
            {
                ApplyFacing(_owner.Movement.LookDirection);
            }
        }

        AdvanceIdle(deltaTime);
    }

    /// <summary>
    /// 발사 방향의 걷기 8방향을 한 바퀴 재생한다.
    /// </summary>
    public void FaceShot(Vector2 direction)
    {
        if (_fainting || _posing)
        {
            return;
        }

        _shotPose = true;
        ApplyFacing(direction);
        _frameIndex = 0;
        _frameTimer = 0f;
        _attackFacingTimer = ShotCycleSeconds();
        ShowCurrentFrame();
    }

    /// <summary>
    /// 마지막으로 본 좌우 방향의 기절 그림을 한 번 재생하고 마지막 장에서 멈춘다. 사망 조명 위에 그린다.
    /// </summary>
    public void PlayFaint(float delay = 0f)
    {
        BeginResult(true, delay);
    }

    /// <summary>
    /// 마지막으로 본 좌우 방향의 성공 포즈를 한 번 재생하고 마지막 장에서 멈춘다. 그림이 없으면 현재 장을 유지한다.
    /// </summary>
    public void PlayPose(float delay = 0f)
    {
        if (_fainting)
        {
            return;
        }

        BeginResult(false, delay);
    }

    #endregion

    #region Private Methods

    private void CacheRenderer()
    {
        if (_renderer != null)
        {
            return;
        }

        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = BACK_SORTING_ORDER;
        }
    }

    /// <summary>
    /// 좌우 모드는 그림을 뒤집고, 8방향 모드는 방향에 맞는 그림을 고른다.
    /// </summary>
    private void ApplyFacing(Vector2 lookDirection)
    {
        if (UsesEightDirection)
        {
            ApplyEightDirection(lookDirection);
            return;
        }

        if (lookDirection.x > LOOK_AXIS_EPSILON)
        {
            _facingRight = true;
        }
        else if (lookDirection.x < -LOOK_AXIS_EPSILON)
        {
            _facingRight = false;
        }

        if (_renderer != null)
        {
            _renderer.flipX = _facingRight;
        }
    }

    private void ApplyEightDirection(Vector2 lookDirection)
    {
        _facingRight = false;
        if (_renderer != null)
        {
            _renderer.flipX = false;
        }

        if (lookDirection.sqrMagnitude <= LOOK_AXIS_EPSILON)
        {
            return;
        }

        // 기절 그림은 좌우만 있어서, 위아래를 볼 때도 마지막 좌우를 기억한다.
        if (lookDirection.x > LOOK_AXIS_EPSILON)
        {
            _lastSideRight = true;
        }
        else if (lookDirection.x < -LOOK_AXIS_EPSILON)
        {
            _lastSideRight = false;
        }

        var next = ResolveEightWay(lookDirection);
        if (next == _eightWay)
        {
            return;
        }

        _eightWay = next;
        _frameIndex = 0;
        _frameTimer = 0f;
        ShowCurrentFrame();
    }

    private void AdvanceIdle(float deltaTime)
    {
        var frames = CurrentFrames();
        if (!HasFrames(frames) || frames.Length <= 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        _frameTimer += deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        var changed = false;
        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _frameIndex = (_frameIndex + 1) % frames.Length;
            changed = true;
        }

        if (changed)
        {
            ShowCurrentFrame();
        }
    }

    private void BeginResult(bool faint, float delay)
    {
        _fainting = faint;
        _posing = !faint;
        _shotPose = false;
        _attackFacingTimer = 0f;
        _frameIndex = 0;
        _frameTimer = 0f;
        _resultDelay = Mathf.Max(0f, delay);
        if (_resultDelay > 0f)
        {
            return;
        }

        ShowResultStart();
    }

    /// <summary>
    /// 대기 중이면 false. 대기가 끝나는 순간 결과 그림의 첫 장을 보여 준다.
    /// </summary>
    private bool ConsumeResultDelay(float deltaTime)
    {
        if (_resultDelay <= 0f)
        {
            return true;
        }

        _resultDelay -= deltaTime;
        if (_resultDelay > 0f)
        {
            return false;
        }

        _resultDelay = 0f;
        _frameIndex = 0;
        _frameTimer = 0f;
        ShowResultStart();
        return true;
    }

    private void ShowResultStart()
    {
        CacheRenderer();
        if (_fainting && _renderer != null)
        {
            _renderer.sortingOrder = _inFrontOfOwner
                ? PlayerView.WEAPON_FRONT_DEATH_SORTING_ORDER
                : PlayerView.WEAPON_BACK_DEATH_SORTING_ORDER;
        }

        ShowCurrentFrame();
    }

    private void AdvanceResult(float deltaTime)
    {
        var frames = CurrentFrames();
        if (!HasFrames(frames) || _frameIndex >= frames.Length - 1 || _framesPerSecond <= 0f)
        {
            return;
        }

        _frameTimer += deltaTime;
        var frameDuration = 1f / _framesPerSecond;
        var changed = false;
        while (_frameTimer >= frameDuration && _frameIndex < frames.Length - 1)
        {
            _frameTimer -= frameDuration;
            _frameIndex++;
            changed = true;
        }

        if (changed)
        {
            ShowCurrentFrame();
        }
    }

    private void ShowCurrentFrame()
    {
        var frames = CurrentFrames();
        if (_renderer == null || !HasFrames(frames))
        {
            return;
        }

        if (_frameIndex < 0 || _frameIndex >= frames.Length)
        {
            _frameIndex = 0;
        }

        var sprite = frames[_frameIndex];
        if (sprite == null)
        {
            return;
        }

        _renderer.sprite = sprite;
        _renderer.color = Color.white;
        _renderer.flipX = !UsesEightDirection && _facingRight;
    }

    private Sprite[] CurrentFrames()
    {
        if (_fainting)
        {
            return GetFaintFrames();
        }

        if (_posing)
        {
            return GetPoseFrames();
        }

        if (_shotPose)
        {
            var shotFrames = GetEightWayFrames(_eightWay);
            if (HasFrames(shotFrames))
            {
                return shotFrames;
            }
        }

        if (UsesEightDirection)
        {
            return GetEightWayFrames(_eightWay);
        }

        return _idleFrames;
    }

    private float ShotCycleSeconds()
    {
        var frames = GetEightWayFrames(_eightWay);
        if (!HasFrames(frames) || _framesPerSecond <= 0f)
        {
            return ATTACK_POSE_SECONDS;
        }

        return frames.Length / _framesPerSecond;
    }

    private bool UsesEightDirection => _visual != null || _facingMode == FacingMode.EightDirection;

    private Sprite[] GetFaintFrames()
    {
        if (_animations == null)
        {
            return null;
        }

        return _lastSideRight
            ? FirstFrames(_animations.FaintRight, _animations.FaintLeft)
            : FirstFrames(_animations.FaintLeft, _animations.FaintRight);
    }

    private Sprite[] GetPoseFrames()
    {
        if (_animations == null)
        {
            return null;
        }

        return _lastSideRight
            ? FirstFrames(_animations.PoseRight, _animations.PoseLeft)
            : FirstFrames(_animations.PoseLeft, _animations.PoseRight);
    }

    private Sprite[] GetEightWayFrames(EightWay way)
    {
        if (_animations == null || _animations.Walk == null)
        {
            return null;
        }

        switch (way)
        {
            case EightWay.UpRight:
                return FirstFrames(_animations.Walk.UpRight, _animations.Walk.Right);
            case EightWay.Up:
                return FirstFrames(_animations.Walk.Up, _animations.Walk.Right);
            case EightWay.UpLeft:
                return FirstFrames(_animations.Walk.UpLeft, _animations.Walk.Right);
            case EightWay.Left:
                return FirstFrames(_animations.Walk.Left, _animations.Walk.Right);
            case EightWay.DownLeft:
                return FirstFrames(_animations.Walk.DownLeft, _animations.Walk.Right);
            case EightWay.Down:
                return FirstFrames(_animations.Walk.Down, _animations.Walk.Right);
            case EightWay.DownRight:
                return FirstFrames(_animations.Walk.DownRight, _animations.Walk.Right);
            default:
                return _animations.Walk.Right;
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

    private void EnsurePlaceholder()
    {
        if (_renderer == null || _renderer.sprite != null)
        {
            return;
        }

        _renderer.sprite = PrototypeSprite.WhiteSquare;
        _renderer.color = PLACEHOLDER_COLOR;
    }

    #endregion
}
