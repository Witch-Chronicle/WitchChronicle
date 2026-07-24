using System;
using UnityEngine;

/// <summary>
/// 캐릭터의 현재 HP와 MP를 관리
/// 최대 HP/MP는 CharacterStats에서 계산된 최종 스탯을 참조
/// 현재 HP/MP는 전투와 필드 상태를 관통해 유지
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class CharacterVitals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterStats _characterStats;

    [Header("Current Resources")]
    [SerializeField] private int _currentHp;
    [SerializeField] private int _currentMp;

    private int _lastMaxHp;
    private int _lastMaxMp;

    public event Action OnVitalsChanged;

    public int CurrentHp
    {
        get => _currentHp;
        private set
        {
            _currentHp = Mathf.Clamp(value, 0, MaxHp);
            OnVitalsChanged?.Invoke();
        }
    }

    public int CurrentMp
    {
        get => _currentMp;
        private set
        {
            _currentMp = Mathf.Clamp(value, 0, MaxMp);
            OnVitalsChanged?.Invoke();
        }
    }

    public int MaxHp => _characterStats != null ? _characterStats.GetStat(StatType.MaxHP) : 0;
    public int MaxMp => _characterStats != null ? _characterStats.GetStat(StatType.MaxMP) : 0;

    public bool IsDead => _currentHp <= 0;

    private void Awake()
    {
        if (_characterStats == null)
        {
            _characterStats = GetComponent<CharacterStats>();
        }
    }

    /// <summary>
    /// 스탯 변경 이벤트를 구독
    /// </summary>
    private void OnEnable()
    {
        if (_characterStats != null)
        {
            _characterStats.OnStatsChanged += HandleStatsChanged;
        }
    }

    /// <summary>
    /// 스탯 변경 이벤트 구독을 해제
    /// </summary>
    private void OnDisable()
    {
        if (_characterStats != null)
        {
            _characterStats.OnStatsChanged -= HandleStatsChanged;
        }
    }

    /// <summary>
    /// 시작 시 현재 HP/MP를 최대치로 초기화
    /// 이후 세이브 로드가 생기면 저장된 현재 HP/MP로 덮어쓰기
    /// </summary>
    private void Start()
    {
        InitializeFullVitals();
    }

    /// <summary>
    /// 현재 HP와 MP를 최대치로 초기화
    /// 테스트 시작, 신규 게임 시작, 완전 회복 상황에서 사용
    /// </summary>
    public void InitializeFullVitals()
    {
        _lastMaxHp = MaxHp;
        _lastMaxMp = MaxMp;

        _currentHp = _lastMaxHp;
        _currentMp = _lastMaxMp;

        OnVitalsChanged?.Invoke();
    }

    /// <summary>
    /// 현재 HP와 MP를 지정한 값으로 설정
    /// 세이브 로드나 전투 종료 결과 반영에 사용
    /// </summary>
    /// <param name="currentHp">설정할 현재 HP</param>
    /// <param name="currentMp">설정할 현재 MP</param>
    public void SetCurrentVitals(int currentHp, int currentMp)
    {
        _currentHp = Mathf.Clamp(currentHp, 0, MaxHp);
        _currentMp = Mathf.Clamp(currentMp, 0, MaxMp);

        _lastMaxHp = MaxHp;
        _lastMaxMp = MaxMp;

        OnVitalsChanged?.Invoke();
    }

    /// <summary>
    /// 현재 HP와 MP를 모두 최대치 회복용
    /// </summary>
    public void RestoreFully()
    {
        _currentHp = MaxHp;
        _currentMp = MaxMp;

        OnVitalsChanged?.Invoke();
    }

    /// <summary>
    /// 현재 HP에 피해 적용
    /// </summary>
    /// <param name="damage">적용할 피해량</param>
    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        CurrentHp = _currentHp - damage;
    }

    /// <summary>
    /// 현재 HP를 회복
    /// </summary>
    /// <param name="amount">회복할 HP</param>
    public void HealHp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHp = _currentHp + amount;
    }

    /// <summary>
    /// MP를 소모할 수 있는지 확인
    /// </summary>
    /// <param name="amount">확인할 MP 소모량</param>
    /// <returns>현재 MP가 충분하면 true를 반환</returns>
    public bool CanUseMp(int amount)
    {
        if (amount < 0)
        {
            return false;
        }

        return _currentMp >= amount;
    }

    /// <summary>
    /// 현재 MP를 소모
    /// MP가 부족하면 false를 반환
    /// </summary>
    /// <param name="amount">소모할 MP 양</param>
    /// <returns>MP 소모에 성공하면 true를 반환</returns>
    public bool TryUseMp(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (CanUseMp(amount) == false)
        {
            return false;
        }

        CurrentMp = _currentMp - amount;
        return true;
    }

    /// <summary>
    /// 현재 MP를 회복
    /// </summary>
    /// <param name="amount">회복할 MP 양</param>
    public void RecoverMp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentMp = _currentMp + amount;
    }

    /// <summary>
    /// CharacterStats의 최대 HP/MP가 변경되었을 때 현재 HP/MP를 보정
    /// 최대치가 증가하면 증가분만큼 현재값도 증가
    /// 최대치가 감소하면 현재값을 새 최대치 이하로 제한
    /// </summary>
    private void HandleStatsChanged()
    {
        int maxHp = MaxHp;
        int maxMp = MaxMp;

        if (maxHp > _lastMaxHp)
        {
            _currentHp += maxHp - _lastMaxHp;
        }

        if (maxMp > _lastMaxMp)
        {
            _currentMp += maxMp - _lastMaxMp;
        }

        _currentHp = Mathf.Clamp(_currentHp, 0, maxHp);
        _currentMp = Mathf.Clamp(_currentMp, 0, maxMp);

        _lastMaxHp = maxHp;
        _lastMaxMp = maxMp;

        OnVitalsChanged?.Invoke();
    }
}