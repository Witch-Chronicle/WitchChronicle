using System;
using UnityEngine;

/// <summary>
/// 전투 계산에 사용되는 스탯 묶음으로 변환
/// CharacterStats의 FinalStats를 기반으로 계산 -> 전투할때 사용
/// </summary>
[Serializable]
public class CombatStatBlock
{
    [Header("Resource")]
    [SerializeField] private int _maxHp;
    [SerializeField] private int _maxMp;

    [Header("Combat Power")]
    [SerializeField] private float _attackPower;
    [SerializeField] private float _magicPower;

    [Header("Defense")]
    [SerializeField] private float _defense;
    [SerializeField] private float _magicDefense;

    [Header("Utility")]
    [SerializeField] private float _speed;
    [SerializeField] private float _luck;

    public int MaxHp => _maxHp;
    public int MaxMp => _maxMp;

    public float AttackPower => _attackPower;
    public float MagicPower => _magicPower;

    public float Defense => _defense;
    public float MagicDefense => _magicDefense;

    public float Speed => _speed;
    public float Luck => _luck;

    public void SetValues(
        int maxHp,
        int maxMp,
        float attackPower,
        float magicPower,
        float defense,
        float magicDefense,
        float speed,
        float luck)
    {
        _maxHp = Mathf.Max(1, maxHp);
        _maxMp = Mathf.Max(1, maxMp);

        _attackPower = Mathf.Max(0f, attackPower);
        _magicPower = Mathf.Max(0f, magicPower);

        _defense = Mathf.Max(0f, defense);
        _magicDefense = Mathf.Max(0f, magicDefense);

        _speed = Mathf.Max(0f, speed);
        _luck = Mathf.Max(0f, luck);
    }
}
