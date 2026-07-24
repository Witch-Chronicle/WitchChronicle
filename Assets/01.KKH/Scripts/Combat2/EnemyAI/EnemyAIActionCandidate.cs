using UnityEngine;

/// <summary>
/// 적 AI 행동 후보 데이터
/// 바로 행동하기 이전에 후보를 만든 후 비교해서 최종 행동을 고르는 구조를 위함
/// 점수를 통해 판별
/// </summary>
public class EnemyAIActionCandidate
{
    public BattleActionRequest Request { get; private set; }
    public SkillData SkillData { get; private set; }
    public BattleUnit Target { get; private set; }

    public float Score { get; private set; }
    public int ExpectedDamage { get; private set; }
    public int ExpectedHeal { get; private set; }

    public bool IsBasicAttack { get; private set; }
    public bool IsDamageAction { get; private set; }
    public bool IsHealAction { get; private set; }
    public bool CanKillTarget { get; private set; }
    public bool CanExploitWeakness { get; private set; }

    public int WeaknessHitCount { get; private set; }
    public int ResistHitCount { get; private set; }
    public int NullHitCount { get; private set; }
    public int AbsorbHitCount { get; private set; }

    public bool IsStatusEffectAction { get; private set; }
    public StatusEffectType StatusEffectType { get; private set; }
    public float StatusEffectChance { get; private set; }

    public bool IsInvalid { get; private set; }
    public string InvalidReason { get; private set; }

    public string ScoreReason { get; private set; }

    public bool HasBadElementMatchup => NullHitCount > 0 || AbsorbHitCount > 0;

    /// <summary>
    /// 행동 후보 생성
    /// </summary>
    /// <param name="request">행동 요청</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="target">대상 유팃</param>
    /// <param name="isBasicAttack">기본 공격 여부</param>
    public EnemyAIActionCandidate(
        BattleActionRequest request,
        SkillData skillData,
        BattleUnit target,
        bool isBasicAttack)
    {
        Request = request;
        SkillData = skillData;
        Target = target;
        IsBasicAttack = isBasicAttack;

        Score = 0f;
        ExpectedDamage = 0;
        ExpectedHeal = 0;
        IsDamageAction = false;
        IsHealAction = false;
        CanKillTarget = false;
        CanExploitWeakness = false;
        IsStatusEffectAction = false;
        StatusEffectType = default;
        StatusEffectChance = 0f;
        IsInvalid = false;
        InvalidReason = string.Empty;
        ScoreReason = string.Empty;
    }

    /// <summary>
    /// 예상 피해 설정
    /// 해당 타겟을 처치할 수 있는지 여부 확인
    /// </summary>
    /// <param name="expectedDamage">예상 피해량</param>
    public void SetExpectedDamage(int expectedDamage)
    {
        ExpectedDamage = Mathf.Max(0, expectedDamage);
        IsDamageAction = ExpectedDamage > 0;

        if (Target != null && Target.IsAlive)
        {
            CanKillTarget = ExpectedDamage >= Target.CurrentHp;
        }
    }

    /// <summary>
    /// 예상 회복 설정
    /// </summary>
    /// <param name="expectedHeal">예상 회복량</param>
    public void SetExpectedHeal(int expectedHeal)
    {
        ExpectedHeal = Mathf.Max(0, expectedHeal);
        IsHealAction = ExpectedHeal > 0;
    }

    /// <summary>
    /// 약점 공략 여부 설정
    /// </summary>
    /// <param name="canExploitWeakness">약점 공략 여부</param>
    public void SetCanExploitWeakness(bool canExploitWeakness)
    {
        CanExploitWeakness = canExploitWeakness;
    }

    /// <summary>
    /// 점수 더하기
    /// </summary>
    /// <param name="amount">추가 점수</param>
    public void AddScore(float amount)
    {
        Score += amount;
    }

    /// <summary>
    /// 점수 설정
    /// </summary>
    /// <param name="score">설정 점수</param>
    public void SetScore(float score)
    {
        Score = score;
    }

    /// <summary>
    /// 속성 상성 개수 설정
    /// </summary>
    /// <param name="weaknessHitCount">약점 타격 수</param>
    /// <param name="resistHitCount">내성 타격 수</param>
    /// <param name="nullHitCount">무효 타격 수</param>
    /// <param name="absorbHitCount">흡수 타격 수</param>
    public void SetElementMatchupCounts(
        int weaknessHitCount,
        int resistHitCount,
        int nullHitCount,
        int absorbHitCount)
    {
        WeaknessHitCount = Mathf.Max(0, weaknessHitCount);
        ResistHitCount = Mathf.Max(0, resistHitCount);
        NullHitCount = Mathf.Max(0, nullHitCount);
        AbsorbHitCount = Mathf.Max(0, absorbHitCount);

        CanExploitWeakness = WeaknessHitCount > 0;
    }

    /// <summary>
    /// 상태이상 정보 설정
    /// </summary>
    /// <param name="statusEffectType">상태이상 타입</param>
    /// <param name="statusEffectChance">상태이상 확률</param>
    public void SetStatusEffectInfo(
        StatusEffectType statusEffectType,
        float statusEffectChance)
    {
        StatusEffectType = statusEffectType;
        StatusEffectChance = Mathf.Clamp01(statusEffectChance);
        IsStatusEffectAction = StatusEffectChance > 0f;
    }

    /// <summary>
    /// 행동 후보 선택 불가 설정
    /// </summary>
    /// <param name="reason">선택 불가 사유</param>
    public void SetInvalid(string reason)
    {
        IsInvalid = true;
        InvalidReason = reason;
    }

    /// <summary>
    /// 점수 초기화
    /// </summary>
    public void ResetScore()
    {
        Score = 0f;
        ScoreReason = string.Empty;
    }

    /// <summary>
    /// 사유와 함께 점수 더하기
    /// </summary>
    /// <param name="amount">추가 점수</param>
    /// <param name="reason">점수 사유</param>
    public void AddScore(float amount, string reason)
    {
        Score += amount;

        if (string.IsNullOrEmpty(reason))
        {
            return;
        }

        if (string.IsNullOrEmpty(ScoreReason))
        {
            ScoreReason = $"{reason}: {amount:F1}";
            return;
        }

        ScoreReason += $", {reason}: {amount:F1}";
    }
}
