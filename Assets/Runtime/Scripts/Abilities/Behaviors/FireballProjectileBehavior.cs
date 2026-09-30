using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 직진하다 적에게 닿거나 수명이 끝나면 주변 적에게 피해를 준다.
/// </summary>
public class FireballProjectileBehavior : MonoBehaviour
{
    private const int EXPLOSION_HIDE_MS = 1000;
    private const float MIN_TRAVEL_TIME = 0.01f;

    [SerializeField] private Collider2D fireballCollider;
    [SerializeField] private ParticleSystem explosionParticle;
    [SerializeField] private GameObject visuals;
    [SerializeField] private AudioClip _launchClip;
    [SerializeField] private AudioClip _explosionClip;

    private readonly List<Enemy> _hitEnemies = new List<Enemy>();

    private FireAttackAbilityBehavior _owner;
    private CancellationTokenSource _hide;
    private Vector3 _velocity;
    private float _lifeRemaining;
    private float _damage;
    private float _explosionRadius;
    private MonsterType _attackType;
    private bool _flying;

    #region Unity Methods

    private void Update()
    {
        if (!_flying)
        {
            return;
        }

        transform.position += _velocity * Time.deltaTime;
        _lifeRemaining -= Time.deltaTime;
        if (_lifeRemaining <= 0f)
        {
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_flying || other.GetComponentInParent<Enemy>() == null)
        {
            return;
        }

        Explode();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 방향이 잡힌 뒤 비행을 시작한다.
    /// </summary>
    public void Launch(
        FireAttackAbilityBehavior owner,
        MonsterType attackType,
        float damage,
        float speed,
        float lifetime,
        float size,
        float explosionRadius,
        float sizeMultiplier,
        float durationMultiplier,
        float projectileSpeedMultiplier)
    {
        CancelHide();
        _owner = owner;
        _attackType = attackType;
        _damage = damage;
        _explosionRadius = explosionRadius;
        transform.localScale = Vector3.one * size * sizeMultiplier;

        var travelTime = Mathf.Max(MIN_TRAVEL_TIME, lifetime / projectileSpeedMultiplier);
        var distance = speed * lifetime * durationMultiplier;
        _velocity = transform.up * (distance / travelTime);
        _lifeRemaining = travelTime;
        _flying = true;

        if (visuals != null)
        {
            visuals.SetActive(true);
        }

        if (fireballCollider != null)
        {
            fireballCollider.enabled = true;
        }

        PlayClip(_launchClip);
    }

    /// <summary>
    /// 비행과 폭발 대기를 멈추고 풀로 되돌린다.
    /// </summary>
    public void Clear()
    {
        CancelHide();
        _flying = false;
        ResetVisuals();
        gameObject.SetActive(false);
    }

    #endregion

    #region Private Methods

    private void Explode()
    {
        if (!_flying)
        {
            return;
        }

        _flying = false;
        if (fireballCollider != null)
        {
            fireballCollider.enabled = false;
        }

        if (visuals != null)
        {
            visuals.SetActive(false);
        }

        if (Managers.Instance != null && Managers.Instance.TryGetManager<EnemyManager>(out var enemyManager))
        {
            enemyManager.CollectInRadius(transform.position, _explosionRadius, _hitEnemies);
            for (var i = 0; i < _hitEnemies.Count; i++)
            {
                _hitEnemies[i].ApplyDamage(_damage, _attackType);
            }
        }

        if (explosionParticle != null)
        {
            explosionParticle.Play();
        }

        PlayClip(_explosionClip);
        _hide = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        HideAfterExplosionAsync(_hide.Token).Forget();
    }

    private async UniTaskVoid HideAfterExplosionAsync(CancellationToken token)
    {
        var canceled = await UniTask.Delay(EXPLOSION_HIDE_MS, cancellationToken: token).SuppressCancellationThrow();
        if (canceled)
        {
            return;
        }

        ResetVisuals();
        gameObject.SetActive(false);
        if (_owner != null)
        {
            _owner.NotifyFireballFinished(this);
        }
    }

    private void ResetVisuals()
    {
        if (visuals != null)
        {
            visuals.SetActive(true);
        }

        if (fireballCollider != null)
        {
            fireballCollider.enabled = true;
        }
    }

    private void CancelHide()
    {
        if (_hide == null)
        {
            return;
        }

        _hide.Cancel();
        _hide.Dispose();
        _hide = null;
    }

    private static void PlayClip(AudioClip clip)
    {
        if (clip == null || Managers.Instance == null || !Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            return;
        }

        audioManager.PlaySound(clip);
    }

    #endregion
}
