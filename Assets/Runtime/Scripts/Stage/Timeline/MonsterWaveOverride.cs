using System;
using UnityEngine;

/// <summary>
/// 클립이 트랙 수치 대신 쓰는 값. 켜진 항목만 덮어쓴다.
/// </summary>
[Serializable]
public class MonsterWaveOverride
{
    [SerializeField] private bool _useHealthOverride;
    [SerializeField] private float _healthOverride = 10f;
    [SerializeField] private bool _useDamageOverride;
    [SerializeField] private float _damageOverride = 2f;
    [SerializeField] private bool _useSpeedOverride;
    [SerializeField] private float _speedOverride = 1.5f;
    [SerializeField] private bool _useScaleOverride;
    [SerializeField, Min(0.01f)] private float _scaleOverride = MonsterVisualData.DEFAULT_SCALE;
    [SerializeField] private bool _disableOffscreenTeleport;

    /// <summary>
    /// 켜진 덮어쓰기만 프로필에 반영한다.
    /// </summary>
    public void Apply(MonsterWaveProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        if (_useHealthOverride)
        {
            profile.MaxHealth = _healthOverride;
        }

        if (_useDamageOverride)
        {
            profile.ContactDamage = _damageOverride;
        }

        if (_useSpeedOverride)
        {
            profile.MoveSpeed = _speedOverride;
        }

        if (_useScaleOverride)
        {
            profile.OverrideScale = true;
            profile.Scale = _scaleOverride;
        }

        profile.DisableOffscreenTeleport = _disableOffscreenTeleport;
    }
}
