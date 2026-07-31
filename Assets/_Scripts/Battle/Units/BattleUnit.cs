using System;
using UnityEngine;
using System.Collections.Generic;

// 전투 중 체력,마나 등 계산용 (런타임)
// 캐릭터 또는 적 SO값을 복사해 생성하며 전투 중 변경되는 상태를 관리
public class BattleUnit
{
    private readonly string _unitId;
    private readonly string _unitName;
    private readonly BattleTeamType _teamType;
    private readonly Sprite _icon;

    private readonly int _level;

    private readonly int _maxHp;
    private int _currentHp;

    private readonly int _maxMp;
    private int _currentMp;
    private readonly bool _usesMp;

    private readonly float _attackPower;
    private readonly float _magicPower;
    private readonly float _defensePower;
    private readonly float _magicDefensePower;
    private readonly float _speed;

    private readonly List<ElementType> _weakElements;
    private readonly List<ElementType> _resistElements;
    private readonly List<ElementType> _nullElements;
    private readonly List<ElementType> _absorbElements;

    private readonly List<SkillData> _skillList;
    private readonly EnemyAIProfileData _aiProfileData;

    public EnemyAIProfileData AIProfileData => _aiProfileData;

    public string UnitId => _unitId;
    public string UnitName => _unitName;
    public BattleTeamType TeamType => _teamType;
    public Sprite Icon => _icon;

    public int Level => _level;

    public int MaxHp => _maxHp;
    public int CurrentHp => _currentHp;

    public int MaxMp => _maxMp;
    public int CurrentMp => _currentMp;
    public bool UsesMp => _usesMp;

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

    public bool IsAlive => _currentHp > 0;

    public event Action OnHpChanged;
    public event Action OnMpChanged;

    /// <summary>
    /// 실제로 HP가 감소한 양(양수)을 실어서 발동. 데미지 팝업 등에서 사용.
    /// (요청 데미지가 아니라 "실제로 깎인 HP"라 오버킬 상황에서도 정확한 수치를 보장함)
    /// </summary>
    public event Action<int> OnDamaged;

    /// <summary>
    /// 실제로 HP가 회복된 양(양수)을 실어서 발동. 힐 팝업 등에서 사용.
    /// (요청 회복량이 아니라 "실제로 채워진 HP"라 만피 상태의 오버힐은 0으로 처리되어 발동 안 함)
    /// </summary>
    public event Action<int> OnHealed;

    /// <summary>
    /// BattleUnit 내부 데이터를 초기화
    /// </summary>
    private BattleUnit(
        string unitId,
        string unitName,
        BattleTeamType teamType,
        int maxHp,
        int currentHp,
        int maxMp,
        int currentMp,
        bool usesMp,
        float attackPower,
        float magicPower,
        float defensePower,
        float magicDefensePower,
        float speed,
        IReadOnlyList<ElementType> weakElements,
        IReadOnlyList<ElementType> resistElements,
        IReadOnlyList<ElementType> nullElements,
        IReadOnlyList<ElementType> absorbElements,
        IReadOnlyList<SkillData> skillList,
        EnemyAIProfileData aiProfileData = null,
        Sprite icon = null,
        int level = 1)
    {
        _unitId = unitId;
        _unitName = unitName;
        _teamType = teamType;
        _icon = icon;
        _level = Mathf.Max(1, level);

        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = Mathf.Clamp(currentHp, 0, _maxHp);

        _maxMp = Mathf.Max(0, maxMp);
        _currentMp = Mathf.Clamp(currentMp, 0, _maxMp);
        _usesMp = usesMp;

        _attackPower = attackPower;
        _magicPower = magicPower;
        _defensePower = defensePower;
        _magicDefensePower = magicDefensePower;
        _speed = speed;

        _weakElements = CreateElementList(weakElements);
        _resistElements = CreateElementList(resistElements);
        _nullElements = CreateElementList(nullElements);
        _absorbElements = CreateElementList(absorbElements);

        _skillList = CreateSkillList(skillList);

        _aiProfileData = aiProfileData;
    }

    /// <summary>
    /// 플레이어 전투 유닛을 생성합니다.
    /// CharacterStats에서 계산된 최대 스탯과 CharacterVitals의 현재 HP/MP를 기반으로 초기화합니다.
    /// </summary>
    /// <param name="unitId">유닛 ID입니다.</param>
    /// <param name="unitName">유닛 이름입니다.</param>
    /// <param name="maxHp">최대 HP입니다.</param>
    /// <param name="currentHp">현재 HP입니다.</param>
    /// <param name="maxMp">최대 MP입니다.</param>
    /// <param name="currentMp">현재 MP입니다.</param>
    /// <param name="attackPower">물리 공격력입니다.</param>
    /// <param name="magicPower">마법 공격력입니다.</param>
    /// <param name="defensePower">물리 방어력입니다.</param>
    /// <param name="magicDefensePower">마법 방어력입니다.</param>
    /// <param name="speed">속도입니다.</param>
    /// <param name="skillList">사용 가능한 스킬 목록입니다.</param>
    /// <param name="icon">캐릭터 아이콘입니다.</param>
    /// <param name="level">캐릭터 레벨입니다.</param>
    /// <returns>생성된 플레이어 BattleUnit입니다.</returns>
    public static BattleUnit CreatePlayer(
        string unitId,
        string unitName,
        int maxHp,
        int currentHp,
        int maxMp,
        int currentMp,
        float attackPower,
        float magicPower,
        float defensePower,
        float magicDefensePower,
        float speed,
        IReadOnlyList<SkillData> skillList,
        Sprite icon = null,
        int level = 1)
    {
        return new BattleUnit(
            unitId,
            unitName,
            BattleTeamType.Player,
            maxHp,
            currentHp,
            maxMp,
            currentMp,
            true,
            attackPower,
            magicPower,
            defensePower,
            magicDefensePower,
            speed,
            null,
            null,
            null,
            null,
            skillList,
            null,
            icon,
            level);
    }

    /// <summary>
    /// 적 데이터 SO를 기반으로 적 BattleUnit을 생성합니다.
    /// 적은 MP 제한 없이 스킬을 사용하므로 UsesMp를 false로 설정합니다.
    /// </summary>
    /// <param name="enemyData">적 전투 데이터입니다.</param>
    /// <returns>생성된 적 BattleUnit입니다.</returns>
    public static BattleUnit CreateEnemy(EnemyBattleData enemyData)
    {
        if (enemyData == null)
        {
            Debug.LogError("EnemyBattleData가 null입니다. Enemy BattleUnit을 생성할 수 없습니다.");
            return null;
        }

        return new BattleUnit(
            enemyData.EnemyId,
            enemyData.EnemyName,
            BattleTeamType.Enemy,
            enemyData.MaxHp,
            enemyData.MaxHp,
            0,
            0,
            false,
            enemyData.AttackPower,
            enemyData.MagicPower,
            enemyData.DefensePower,
            enemyData.MagicDefensePower,
            enemyData.Speed,
            enemyData.WeakElements,
            enemyData.ResistElements,
            enemyData.NullElements,
            enemyData.AbsorbElements,
            enemyData.SkillList,
            enemyData.AIProfileData,
            enemyData.Icon);
    }

    /// <summary>
    /// 대상에게 데미지 적용. 실제로 깎인 HP만큼 OnDamaged를 발동.
    /// </summary>
    public void TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(0, damage);

        int previousHp = _currentHp;
        _currentHp = Mathf.Max(0, _currentHp - finalDamage);
        int actualDamage = previousHp - _currentHp;

        OnHpChanged?.Invoke();

        if (actualDamage > 0)
        {
            OnDamaged?.Invoke(actualDamage);
        }
    }

    /// <summary>
    /// 빗나감(혼란 miss 등) 통지. HP는 변하지 않고 OnDamaged(0)만 발동한다.
    /// 연출 측(데미지 팝업)에서 0을 "Miss"로 표시하는 용도.
    /// </summary>
    public void NotifyMiss()
    {
        OnDamaged?.Invoke(0);
    }

    /// <summary>
    /// 대상의 HP 회복. 실제로 채워진 HP만큼 OnHealed를 발동 (만피 상태의 오버힐은 발동 안 함).
    /// </summary>
    public void Heal(int amount)
    {
        int finalAmount = Mathf.Max(0, amount);

        int previousHp = _currentHp;
        _currentHp = Mathf.Min(_maxHp, _currentHp + finalAmount);
        int actualHeal = _currentHp - previousHp;

        OnHpChanged?.Invoke();

        if (actualHeal > 0)
        {
            OnHealed?.Invoke(actualHeal);
        }
    }

    /// <summary>
    /// MP 소모
    /// MP를 사용하지 않는 유닛은 항상 성공
    /// </summary>
    public bool UseMp(int amount)
    {
        if (_usesMp == false)
        {
            return true;
        }

        int finalAmount = Mathf.Max(0, amount);

        if (_currentMp < finalAmount)
        {
            return false;
        }

        _currentMp -= finalAmount;

        OnMpChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// MP 회복. MP를 사용하지 않는 유닛은 효과 없음. 최대 MP를 넘지 않는다.
    /// </summary>
    public void RestoreMp(int amount)
    {
        if (_usesMp == false)
        {
            return;
        }

        int finalAmount = Mathf.Max(0, amount);

        _currentMp = Mathf.Min(_maxMp, _currentMp + finalAmount);

        OnMpChanged?.Invoke();
    }

    /// <summary>
    /// 해당 스킬을 사용할 수 있는지 확인
    /// MP를 사용하는 유닛만 MP 소모량을 검사
    /// </summary>
    public bool CanUseSkill(SkillData skillData)
    {
        if (skillData == null)
        {
            return false;
        }

        if (_usesMp == false)
        {
            return true;
        }

        return _currentMp >= skillData.MpCost;
    }

    /// <summary>
    /// 지정한 속성이 이 유닛의 약점인지 확인
    /// </summary>
    public bool IsWeakTo(ElementType elementType)
    {
        return ContainsElement(_weakElements, elementType);
    }

    /// <summary>
    /// 지정한 속성에 이 유닛이 저항을 가지는지 확인
    /// </summary>
    public bool IsResistTo(ElementType elementType)
    {
        return ContainsElement(_resistElements, elementType);
    }

    /// <summary>
    /// 지정한 속성을 이 유닛이 무효화하는지 확인
    /// </summary>
    public bool IsNullTo(ElementType elementType)
    {
        return ContainsElement(_nullElements, elementType);
    }

    /// <summary>
    /// 지정한 속성을 이 유닛이 흡수하는지 확인
    /// </summary>
    public bool IsAbsorbTo(ElementType elementType)
    {
        return ContainsElement(_absorbElements, elementType);
    }

    /// <summary>
    /// 전달된 속성 목록을 복사해 BattleUnit 내부 리스트로 변환
    /// </summary>
    private static List<ElementType> CreateElementList(IReadOnlyList<ElementType> elements)
    {
        List<ElementType> result = new List<ElementType>();

        if (elements == null)
        {
            return result;
        }

        for (int i = 0; i < elements.Count; i++)
        {
            result.Add(elements[i]);
        }

        return result;
    }

    /// <summary>
    /// 전달된 스킬 목록을 복사해 BattleUnit 내부 리스트로 변환
    /// </summary>
    private static List<SkillData> CreateSkillList(IReadOnlyList<SkillData> skillList)
    {
        List<SkillData> result = new List<SkillData>();

        if (skillList == null)
        {
            return result;
        }

        for (int i = 0; i < skillList.Count; i++)
        {
            if (skillList[i] == null)
            {
                continue;
            }

            result.Add(skillList[i]);
        }

        return result;
    }

    /// <summary>
    /// 속성 목록에 특정 속성이 포함되어 있는지 확인
    /// </summary>
    private static bool ContainsElement(IReadOnlyList<ElementType> elements, ElementType elementType)
    {
        if (elements == null)
        {
            return false;
        }

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] == elementType)
            {
                return true;
            }
        }

        return false;
    }
}