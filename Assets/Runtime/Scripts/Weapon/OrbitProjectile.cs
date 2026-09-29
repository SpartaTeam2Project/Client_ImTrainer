using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 주위를 일정 시간 돌며, 닿은 적에게 간격마다 피해를 준다.
/// </summary>
public class OrbitProjectile : MonoBehaviour
{
    private const float PROJECTILE_SIZE = 0.4f;
    private const int PROJECTILE_SORTING_ORDER = 12;
    private static readonly Color PROJECTILE_COLOR = new Color(0.55f, 0.9f, 1f, 1f);

    private readonly List<Enemy> _overlap = new List<Enemy>();
    private readonly List<RecentHit> _recentHits = new List<RecentHit>();

    private SpriteRenderer _renderer;
    private int _playerId;
    private float _angleDegrees;
    private float _radius;
    private float _degreesPerSecond;
    private float _remaining;
    private float _hitRadius;
    private float _hitInterval;
    private float _damage;
    private float _elapsed;
    private MonsterType _attackType = MonsterType.Normal;

    public bool IsActive => gameObject.activeSelf;

    private struct RecentHit
    {
        public Enemy Enemy;
        public float ReadyAt;
    }

    #region Unity Methods

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null && _renderer.sprite == null)
        {
            _renderer.sprite = PrototypeSprite.WhiteSquare;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 자리표시 궤도 총알을 만들어 꺼 둔다.
    /// </summary>
    public static OrbitProjectile Create(Transform parent)
    {
        var actorObject = new GameObject("OrbitProjectile");
        actorObject.transform.SetParent(parent, false);
        actorObject.transform.localScale = new Vector3(PROJECTILE_SIZE, PROJECTILE_SIZE, 1f);

        var renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.sprite = PrototypeSprite.WhiteSquare;
        renderer.color = PROJECTILE_COLOR;
        renderer.sortingOrder = PROJECTILE_SORTING_ORDER;

        var projectile = actorObject.AddComponent<OrbitProjectile>();
        actorObject.SetActive(false);
        return projectile;
    }

    /// <summary>
    /// 총알 그림을 넣는다. 비어 있으면 기존 그림을 유지한다.
    /// </summary>
    public void ApplySprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        if (_renderer == null)
        {
            return;
        }

        _renderer.sprite = sprite;
        _renderer.color = Color.white;
    }

    /// <summary>
    /// 플레이어 기준 시작 각도로 궤도를 켠다.
    /// </summary>
    public void Launch(int playerId, float angleDegrees, float radius, float degreesPerSecond, float duration, float hitRadius, float hitInterval, float damage, MonsterType attackType)
    {
        _playerId = playerId;
        _angleDegrees = angleDegrees;
        _radius = Mathf.Max(0f, radius);
        _degreesPerSecond = Mathf.Max(0f, degreesPerSecond);
        _remaining = Mathf.Max(0f, duration);
        _hitRadius = Mathf.Max(0f, hitRadius);
        _hitInterval = Mathf.Max(0f, hitInterval);
        _damage = Mathf.Max(0f, damage);
        _attackType = attackType;
        _elapsed = 0f;
        _recentHits.Clear();
        gameObject.SetActive(true);
        PlaceOnOrbit();
    }

    /// <summary>
    /// 이미 도는 총알의 수명과 수치를 다시 채운다. 현재 각도는 유지한다.
    /// </summary>
    public void Refresh(float duration, float degreesPerSecond, float radius, float hitRadius, float damage)
    {
        _remaining = Mathf.Max(0f, duration);
        _degreesPerSecond = Mathf.Max(0f, degreesPerSecond);
        _radius = Mathf.Max(0f, radius);
        _hitRadius = Mathf.Max(0f, hitRadius);
        _damage = Mathf.Max(0f, damage);
    }

    /// <summary>
    /// 플레이어 주위를 돌고, 수명이 끝나면 끈다.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!IsActive)
        {
            return;
        }

        if (!TryGetPlayer(out var playerTransform))
        {
            gameObject.SetActive(false);
            return;
        }

        _elapsed += deltaTime;
        _remaining -= deltaTime;
        _angleDegrees += _degreesPerSecond * deltaTime;
        PlaceOnOrbit(playerTransform);
        ApplyHits();

        if (_remaining <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Private Methods

    private void PlaceOnOrbit()
    {
        if (!TryGetPlayer(out var playerTransform))
        {
            return;
        }

        PlaceOnOrbit(playerTransform);
    }

    private void PlaceOnOrbit(Transform playerTransform)
    {
        var radians = _angleDegrees * Mathf.Deg2Rad;
        var offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * _radius;
        transform.position = (Vector2)playerTransform.position + offset;
        transform.rotation = Quaternion.Euler(0f, 0f, _angleDegrees);
    }

    private void ApplyHits()
    {
        if (_damage <= 0f || _hitRadius <= 0f || Managers.Instance == null || !Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            return;
        }

        enemyManager.CollectInRadius(transform.position, _hitRadius, _overlap);
        for (var i = 0; i < _overlap.Count; i++)
        {
            var enemy = _overlap[i];
            if (enemy == null || !enemy.IsAlive || !IsHitReady(enemy))
            {
                continue;
            }

            enemy.ApplyDamage(_damage, _attackType);
            _recentHits.Add(new RecentHit
            {
                Enemy = enemy,
                ReadyAt = _elapsed + _hitInterval
            });
        }
    }

    /// <summary>
    /// 같은 적은 간격이 지나기 전에는 다시 맞지 않는다.
    /// </summary>
    private bool IsHitReady(Enemy enemy)
    {
        for (var i = _recentHits.Count - 1; i >= 0; i--)
        {
            var mark = _recentHits[i];
            if (mark.Enemy == null || !mark.Enemy.IsAlive || mark.ReadyAt <= _elapsed)
            {
                _recentHits.RemoveAt(i);
                continue;
            }

            if (mark.Enemy == enemy)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetPlayer(out Transform playerTransform)
    {
        playerTransform = null;
        if (Managers.Instance == null || !Managers.Instance.TryGetManager(out PlayerManager playerManager))
        {
            return false;
        }

        if (playerManager.LocalPlayerId != _playerId || !playerManager.IsAlive || playerManager.PlayerTransform == null)
        {
            return false;
        }

        playerTransform = playerManager.PlayerTransform;
        return true;
    }

    #endregion
}
