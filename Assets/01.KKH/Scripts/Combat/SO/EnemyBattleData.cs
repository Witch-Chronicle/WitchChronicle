using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Witch Chronicle/EnemyData")]
public class EnemyBattleData : ScriptableObject
{
    [Header("Enemy Info")]
    [SerializeField] private string _enemyId;
    [SerializeField] private string _enemyName;
    [SerializeField] private Sprite _icon;

    [Header("Enemy Stats")]
    [SerializeField] private int _maxHp;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _magicPower;
    [SerializeField] private float _defensePower;
    [SerializeField] private float _magicDefensePower;
    [SerializeField] private float _speed;

    [Header("Enemy Type")]
    [SerializeField] private ElementType[] _weakElements;
    [SerializeField] private ElementType[] _resistElements;
    [SerializeField] private ElementType[] _nullElements;
    [SerializeField] private ElementType[] _absorbElements;

    [Header("Enemy Skill List")]
    [SerializeField] private SkillData[] _skillList;

    [Header("AI")]
    [SerializeField] private EnemyAIProfileData _aiProfileData;

    [Header("Reward")]
    [SerializeField] private int _expReward;
    [SerializeField] private int _goldReward;

    // 아이템 리스트로 추가 예정
    [SerializeField] private DropTable _dropTable;

    // 추가
    [Header("Prefab")]
    [SerializeField] private GameObject _prefab;

    public DropTable DropTable => _dropTable;

    public string EnemyId => _enemyId;
    public string EnemyName => _enemyName;
    public Sprite Icon => _icon;

    public int MaxHp => _maxHp;
    public float AttackPower => _attackPower;
    public float MagicPower => _magicPower;
    public float DefensePower => _defensePower;
    public float MagicDefensePower => _magicDefensePower;
    public float Speed => _speed;

    public IReadOnlyList<ElementType> WeakElements => _weakElements;
    public IReadOnlyList<ElementType> ResistElements => _resistElements;
    public IReadOnlyList<ElementType> NullElements => _nullElements;
    public IReadOnlyList<ElementType> AbsorbElements => _absorbElements;

    public IReadOnlyList<SkillData> SkillList => _skillList;

    public EnemyAIProfileData AIProfileData => _aiProfileData;

    public int ExpReward => _expReward;
    public int GoldReward => _goldReward;

    // 추가
    public GameObject Prefab => _prefab;
}
