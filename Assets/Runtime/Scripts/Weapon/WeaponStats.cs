using System;
using UnityEngine;

/// <summary>
/// 장착 포켓몬이 플레이어에게서 떨어질 거리.
/// </summary>
[Serializable]
public class WeaponStats
{
    [SerializeField] private float _followDistance = 1.15f;

    public float FollowDistance => Mathf.Max(0f, _followDistance);
}
