using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어를 따라다니는 장판으로, 안에 있는 적에게 주기적으로 피해를 준다.
/// </summary>
public class GrassAttackAbilityBehavior : AbilityBehavior<GrassAttackAbilityData, GrassAttackAbilityLevel>
{
    private const float SOUND_INTERVAL = 5f;
    private const float MIN_MULTIPLIER = 0.01f;

    [SerializeField] private CircleCollider2D abilityCollider;
    [SerializeField] private Transform visuals;
    [SerializeField] private AudioClip _loopClip;

    private readonly Dictionary<Enemy, float> _enemies = new Dictionary<Enemy, float>();
    private readonly List<Enemy> _scratch = new List<Enemy>();
    private float _lastSoundTime = -100f;

    #region Unity Methods

    private void LateUpdate()
    {
        if (AbilityLevel == null || !TryGetPlayer(out var player))
        {
            return;
        }

        transform.position = player.transform.position;
        var radius = AbilityLevel.FieldRadius * player.sizeMultiplier;
        if (visuals != null)
        {
            visuals.localScale = Vector3.one * radius * 2f;
        }

        if (abilityCollider != null)
        {
            abilityCollider.radius = radius;
        }
    }

    private void Update()
    {
        if (AbilityLevel == null || !TryGetPlayer(out var player))
        {
            return;
        }

        TickDamage(player);
        TickSound();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (AbilityLevel == null || !TryGetPlayer(out var player))
        {
            return;
        }

        var enemy = collision.GetComponentInParent<Enemy>();
        if (enemy == null || _enemies.ContainsKey(enemy))
        {
            return;
        }

        _enemies.Add(enemy, Time.time);
        ApplyFieldDamage(enemy, player);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var enemy = collision.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            _enemies.Remove(enemy);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 접촉 목록을 비우고 치운다.
    /// </summary>
    public override void Clear()
    {
        _enemies.Clear();
        base.Clear();
    }

    #endregion

    #region Private Methods

    private void TickDamage(Player player)
    {
        _scratch.Clear();
        foreach (var enemy in _enemies.Keys)
        {
            _scratch.Add(enemy);
        }

        var interval = AbilityLevel.DamageCooldown * Mathf.Max(MIN_MULTIPLIER, player.cooldownMultiplier);
        for (var i = 0; i < _scratch.Count; i++)
        {
            var enemy = _scratch[i];
            if (enemy == null || !enemy.IsAlive)
            {
                _enemies.Remove(enemy);
                continue;
            }

            if (Time.time - _enemies[enemy] < interval)
            {
                continue;
            }

            _enemies[enemy] = Time.time;
            ApplyFieldDamage(enemy, player);
        }
    }

    private void ApplyFieldDamage(Enemy enemy, Player player)
    {
        enemy.ApplyDamage(AbilityLevel.Damage * player.damageMultiplier, ResolveAttackType());
    }

    private void TickSound()
    {
        if (_loopClip == null || Time.time - _lastSoundTime <= SOUND_INTERVAL)
        {
            return;
        }

        _lastSoundTime = Time.time;
        if (Managers.Instance != null && Managers.Instance.TryGetManager<AudioManager>(out var audioManager))
        {
            audioManager.PlaySound(_loopClip);
        }
    }

    private MonsterType ResolveAttackType()
    {
        if (Managers.Instance != null && Managers.Instance.TryGetManager<AccountManager>(out var account))
        {
            var monster = account.ResolveRunMonster();
            if (monster != null)
            {
                return monster.PrimaryType;
            }
        }

        return MonsterType.Normal;
    }

    #endregion
}
