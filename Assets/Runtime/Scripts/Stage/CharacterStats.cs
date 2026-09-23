using System;
using UnityEngine;

/// <summary>
/// 한 판에 스폰할 플레이어의 기본 체력과 이동 속도.
/// </summary>
[Serializable]
public class CharacterStats
{
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _moveSpeed = 4f;

    public float MaxHealth => _maxHealth;

    public float MoveSpeed => _moveSpeed;
}
