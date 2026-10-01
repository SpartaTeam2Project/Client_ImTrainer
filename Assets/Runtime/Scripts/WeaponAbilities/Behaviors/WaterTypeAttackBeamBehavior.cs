using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 앞에서 적들을 관통하는 빔. 유지 시간 동안 주기 피해를 준다.
/// </summary>
public class WaterTypeAttackBeamBehavior : MonoBehaviour
{
    private const float CLOSEST_RANGE = 80f;
    private const float MIN_DIRECTION = 0.0001f;
    private const float MIN_LIFETIME = 0.01f;
    private const float PARTICLE_SIZE_PER_WIDTH = 2f;

    [SerializeField] private BoxCollider2D beamCollider;
    [SerializeField] private Transform visuals;
    [Tooltip("비우면 visuals를 빔 크기로 늘린다. 넣으면 이 파티클을 빔 길이만큼 쏘아 물줄기로 보여 준다.")]
    [SerializeField] private ParticleSystem streamParticle;

    private readonly Dictionary<Enemy, float> _nextHit = new Dictionary<Enemy, float>();

    private int _playerId;
    private float _damage;
    private float _interval;
    private float _timeLeft;
    private MonsterType _attackType;
    private bool _playing;

    #region Unity Methods

    private void Awake()
    {
        if (beamCollider == null)
        {
            beamCollider = GetComponent<BoxCollider2D>();
        }

        SetPlaying(false);
    }

    private void Update()
    {
        if (!_playing)
        {
            return;
        }

        if (WeaponAbilityManager.IsCombatPaused())
        {
            return;
        }

        if (!TryGetPlayer(out var player))
        {
            Stop();
            return;
        }

        transform.position = player.transform.position;
        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f)
        {
            Stop();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_playing || WeaponAbilityManager.IsCombatPaused())
        {
            return;
        }

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null)
        {
            return;
        }

        if (_nextHit.TryGetValue(enemy, out var nextTime) && Time.time < nextTime)
        {
            return;
        }

        enemy.ApplyDamage(_damage, _attackType);
        _nextHit[enemy] = Time.time + _interval;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 가까운 적 방향으로 빔을 켜고, 유지 시간이 끝나면 끈다.
    /// </summary>
    public void Play(
        int playerId,
        float damage,
        float length,
        float width,
        float duration,
        float interval,
        MonsterType attackType)
    {
        _playerId = playerId;
        _damage = damage;
        _interval = Mathf.Max(0.02f, interval);
        _timeLeft = duration;
        _attackType = attackType;
        _nextHit.Clear();
        ApplyShape(length, width);
        Aim();
        SetPlaying(true);
    }

    /// <summary>
    /// 빔을 끈다.
    /// </summary>
    public void Stop()
    {
        _playing = false;
        _nextHit.Clear();
        SetPlaying(false);
    }

    #endregion

    #region Private Methods

    private void ApplyShape(float length, float width)
    {
        if (beamCollider != null)
        {
            beamCollider.size = new Vector2(width, length);
            beamCollider.offset = new Vector2(0f, length * 0.5f);
        }

        if (streamParticle != null)
        {
            ApplyStream(length, width);
            return;
        }

        if (visuals != null)
        {
            visuals.localScale = new Vector3(width, length, 1f);
            visuals.localPosition = new Vector3(0f, length * 0.5f, 0f);
        }
    }

    /// <summary>
    /// 물방울이 수명 동안 빔 끝까지 날아가도록 속도를 맞춘다. 파티클 쪽 +X가 빔 방향이다.
    /// </summary>
    private void ApplyStream(float length, float width)
    {
        var main = streamParticle.main;
        main.startSize = width * PARTICLE_SIZE_PER_WIDTH;

        var velocity = streamParticle.velocityOverLifetime;
        velocity.x = length / Mathf.Max(MIN_LIFETIME, main.startLifetime.constant);
        velocity.y = 0f;
        velocity.z = 0f;
    }

    private void Aim()
    {
        var direction = Vector2.up;
        if (TryGetPlayer(out var player))
        {
            transform.position = player.transform.position;
            direction = ResolveDirection(player.transform.position);
        }

        transform.rotation = Quaternion.FromToRotation(Vector2.up, direction);
        if (Managers.Instance != null && Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            playerManager.FaceShot(_playerId, direction);
        }
    }

    private Vector2 ResolveDirection(Vector3 origin)
    {
        if (Managers.Instance != null
            && Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager)
            && enemyManager.TryGetClosest(origin, CLOSEST_RANGE, out var enemy))
        {
            var offset = (Vector2)enemy.transform.position - (Vector2)origin;
            if (offset.sqrMagnitude > MIN_DIRECTION)
            {
                return offset.normalized;
            }
        }

        var random = Random.insideUnitCircle;
        if (random.sqrMagnitude <= MIN_DIRECTION)
        {
            return Vector2.up;
        }

        return random.normalized;
    }

    private bool TryGetPlayer(out Player player)
    {
        player = null;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager<PlayerManager>(out var playerManager))
        {
            return false;
        }

        return playerManager.TryGetPlayer(_playerId, out player);
    }

    private void SetPlaying(bool playing)
    {
        _playing = playing;
        if (beamCollider != null)
        {
            beamCollider.enabled = playing;
        }

        if (visuals != null)
        {
            visuals.gameObject.SetActive(playing);
        }
    }

    #endregion
}
