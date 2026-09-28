using System;
using UnityEngine;

/// <summary>
/// 증강에서 하나만 고르는 무기 수치.
/// </summary>
public enum WeaponStatKind
{
    AttackRange = 0,
    Cooldown = 1,
    Width = 2,
    Damage = 3
}

/// <summary>
/// 증강 한 가지. 고르면 그 수치만 적힌 만큼 오른다.
/// </summary>
[Serializable]
public class WeaponStatUpgrade
{
    [SerializeField] private WeaponStatKind _stat = WeaponStatKind.Damage;
    [SerializeField] private float _amount = 1f;

    public WeaponStatKind Stat => _stat;

    public float Amount => _amount;
}
