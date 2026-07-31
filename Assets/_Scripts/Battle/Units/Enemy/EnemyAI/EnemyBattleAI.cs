using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 전투 판단 AI (임시)
/// </summary>
public class EnemyBattleAI
{
    private const float HealHpRatioThreshold = 0.4f;
    private const bool IsDebugLogEnabled = true;

    private readonly List<BattleUnit> _aliveAllies = new List<BattleUnit>();
    private readonly List<BattleUnit> _aliveOpponents = new List<BattleUnit>();
    private readonly List<EnemyAIActionCandidate> _actionCandidates = new List<EnemyAIActionCandidate>();

    /// <summary>
    /// 적 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동하는 적 유닛</param>
    /// <param name="battleUnits">전투에 참가된 전체 유닛</param>
    /// <returns>선택된 행동 요청</returns>
    public BattleActionRequest CreateActionRequest(
        BattleUnit actor,
        IReadOnlyList<BattleUnit> battleUnits)
    {
        if (actor == null || actor.IsAlive == false)
        {
            return null;
        }

        CacheAliveUnits(actor, battleUnits);
        BuildActionCandidates(actor);
        ValidateActionCandidates();

        if (_actionCandidates.Count <= 0)
        {
            return null;
        }

        EnemyAIProfileData aiProfile = actor.AIProfileData;

        if (aiProfile == null)
        {
            Debug.LogWarning("[EnemyBattleAI] AI Profile 없음. 기본 공격 선택");
            return CreateBasicAttackRequest(actor);
        }

        ScoreActionCandidates(actor, aiProfile);

        EnemyAIActionCandidate bestCandidate = SelectBestCandidate();

        LogActionCandidates(actor, bestCandidate);

        if (bestCandidate == null)
        {
            return CreateBasicAttackRequest(actor);
        }

        return bestCandidate.Request;
    }

    /// <summary>
    /// 생존하고 있는 유닛 목록 캐싱
    /// </summary>
    /// <param name="actor">기준 유닛</param>
    /// <param name="battleUnits">전투의 전체 유닛</param>
    private void CacheAliveUnits(
        BattleUnit actor,
        IReadOnlyList<BattleUnit> battleUnits)
    {
        _aliveAllies.Clear();
        _aliveOpponents.Clear();

        if (actor == null || battleUnits == null)
            return;

        for (int i = 0; i < battleUnits.Count; i++)
        {
            BattleUnit unit = battleUnits[i];

            if (unit == null || unit.IsAlive == false)
                continue;

            if (unit.TeamType == actor.TeamType)
            {
                _aliveAllies.Add(unit);
                continue;
            }

            _aliveOpponents.Add(unit);
        }
    }

    /// <summary>
    /// 회복 행동 요청 생성 시도
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <returns>회복 행동 요청</returns>
    private BattleActionRequest TryCreateHealRequest(
        BattleUnit actor,
        float healHpRatioThreshold)
    {
        if (actor.SkillList == null || actor.SkillList.Count == 0)
            return null;

        BattleUnit lowestHpAlly = FindLowestHpRatioUnit(_aliveAllies);

        if (lowestHpAlly == null)
            return null;

        if (GetHpRatio(lowestHpAlly) > healHpRatioThreshold)
            return null;

        for (int i = 0; i < actor.SkillList.Count; i++)
        {
            SkillData skillData = actor.SkillList[i];

            if (CanUseHealSkill(actor, skillData) == false)
                continue;

            if (skillData.TargetType == TargetType.Self && lowestHpAlly == actor)
                return BattleActionRequest.CreateSkill(actor, skillData, null);

            if (skillData.TargetType == TargetType.SingleAlly)
                return BattleActionRequest.CreateSkill(actor, skillData, lowestHpAlly);

            if (skillData.TargetType == TargetType.AllAllies)
                return BattleActionRequest.CreateSkill(actor, skillData, null);
        }

        return null;
    }

    /// <summary>
    /// 공격 스킬 행동 요청 생성 시도
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <returns>공격 스킬 행동 요청</returns>
    private BattleActionRequest TryCreateDamageSkillRequest(BattleUnit actor)
    {
        if (actor.SkillList == null || actor.SkillList.Count == 0)
        {
            return null;
        }

        BattleUnit target = FindLowestHpRatioUnit(_aliveOpponents);

        if (target == null)
        {
            return null;
        }

        SkillData allEnemySkill = FindDamageSkill(actor, TargetType.AllEnemies);

        if (allEnemySkill != null && _aliveOpponents.Count >= 2)
        {
            return BattleActionRequest.CreateSkill(actor, allEnemySkill, null);
        }

        SkillData singleEnemySkill = FindDamageSkill(actor, TargetType.SingleEnemy);

        if (singleEnemySkill != null)
        {
            return BattleActionRequest.CreateSkill(actor, singleEnemySkill, target);
        }

        if (allEnemySkill != null)
        {
            return BattleActionRequest.CreateSkill(actor, allEnemySkill, null);
        }

        return null;
    }

    /// <summary>
    /// 기본 공격 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <returns>기본 공격 행동 요청</returns>
    private BattleActionRequest CreateBasicAttackRequest(BattleUnit actor)
    {
        BattleUnit target = FindLowestHpRatioUnit(_aliveOpponents);

        if (target == null)
        {
            return null;
        }

        return BattleActionRequest.CreateAttack(actor, target);
    }

    /// <summary>
    /// 회복 스킬 사용 가능 여부
    /// </summary>
    /// <param name="actor">사용 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <returns>사용 가능 여부</returns>
    private bool CanUseHealSkill(BattleUnit actor, SkillData skillData)
    {
        if (skillData == null)
        {
            return false;
        }

        if (skillData.SkillType != SkillEffectType.Heal)
        {
            return false;
        }

        if (actor.CanUseSkill(skillData) == false)
        {
            return false;
        }

        return skillData.TargetType == TargetType.Self ||
               skillData.TargetType == TargetType.SingleAlly ||
               skillData.TargetType == TargetType.AllAllies;
    }

    /// <summary>
    /// 데미지 스킬 검색
    /// </summary>
    /// <param name="actor">사용 유닛</param>
    /// <param name="targetType">대상 타입</param>
    /// <returns>검색된 스킬</returns>
    private SkillData FindDamageSkill(BattleUnit actor, TargetType targetType)
    {
        for (int i = 0; i < actor.SkillList.Count; i++)
        {
            SkillData skillData = actor.SkillList[i];

            if (skillData == null)
            {
                continue;
            }

            if (skillData.SkillType != SkillEffectType.Damage)
            {
                continue;
            }

            if (skillData.TargetType != targetType)
            {
                continue;
            }

            if (actor.CanUseSkill(skillData) == false)
            {
                continue;
            }

            return skillData;
        }

        return null;
    }

    /// <summary>
    /// HP 비율 최저 유닛 검색
    /// </summary>
    /// <param name="units">검색 대상 목록</param>
    /// <returns>HP 비율 최저 유닛</returns>
    private BattleUnit FindLowestHpRatioUnit(IReadOnlyList<BattleUnit> units)
    {
        BattleUnit result = null;
        float lowestRatio = float.MaxValue;

        if (units == null)
        {
            return null;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];

            if (unit == null || unit.IsAlive == false)
            {
                continue;
            }

            float hpRatio = GetHpRatio(unit);

            if (hpRatio >= lowestRatio)
            {
                continue;
            }

            lowestRatio = hpRatio;
            result = unit;
        }

        return result;
    }

    /// <summary>
    /// HP 비율 반환
    /// </summary>
    /// <param name="unit">대상 유닛</param>
    /// <returns>HP 비율</returns>
    private float GetHpRatio(BattleUnit unit)
    {
        if (unit == null || unit.MaxHp <= 0)
        {
            return 0f;
        }

        return (float)unit.CurrentHp / unit.MaxHp;
    }

    /// <summary>
    /// 기본 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <returns>행동 요청</returns>
    private BattleActionRequest CreateDefaultActionRequest(BattleUnit actor)
    {
        BattleActionRequest healRequest = TryCreateHealRequest(actor, HealHpRatioThreshold);

        if (healRequest != null)
        {
            return healRequest;
        }

        BattleActionRequest damageSkillRequest = TryCreateDamageSkillRequest(actor);

        if (damageSkillRequest != null)
        {
            return damageSkillRequest;
        }

        return CreateBasicAttackRequest(actor);
    }

    /// <summary>
    /// 호전형 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>행동 요청</returns>
    private BattleActionRequest CreateAggressiveActionRequest(
        BattleUnit actor,
        EnemyAIProfileData aiProfile)
    {
        BattleActionRequest damageSkillRequest = TryCreateDamageSkillRequest(actor);

        if (damageSkillRequest != null)
        {
            return damageSkillRequest;
        }

        return CreateBasicAttackRequest(actor);
    }

    /// <summary>
    /// 방어형 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>행동 요청</returns>
    private BattleActionRequest CreateDefensiveActionRequest(
        BattleUnit actor,
        EnemyAIProfileData aiProfile)
    {
        BattleActionRequest healRequest = TryCreateHealRequest(actor, aiProfile.SelfDefenseHpRatio);

        if (healRequest != null)
        {
            return healRequest;
        }

        BattleActionRequest damageSkillRequest = TryCreateDamageSkillRequest(actor);

        if (damageSkillRequest != null)
        {
            return damageSkillRequest;
        }

        return CreateBasicAttackRequest(actor);
    }

    /// <summary>
    /// 지원형 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>행동 요청</returns>
    private BattleActionRequest CreateSupportActionRequest(
        BattleUnit actor,
        EnemyAIProfileData aiProfile)
    {
        BattleActionRequest healRequest = TryCreateHealRequest(actor, aiProfile.SelfDefenseHpRatio);

        if (healRequest != null)
        {
            return healRequest;
        }

        BattleActionRequest damageSkillRequest = TryCreateDamageSkillRequest(actor);

        if (damageSkillRequest != null)
        {
            return damageSkillRequest;
        }

        return CreateBasicAttackRequest(actor);
    }

    /// <summary>
    /// 교활형 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>행동 요청</returns>
    private BattleActionRequest CreateCunningActionRequest(
        BattleUnit actor,
        EnemyAIProfileData aiProfile)
    {
        BattleActionRequest damageSkillRequest = TryCreateDamageSkillRequest(actor);

        if (damageSkillRequest != null)
        {
            return damageSkillRequest;
        }

        BattleActionRequest healRequest = TryCreateHealRequest(actor, aiProfile.SelfDefenseHpRatio);

        if (healRequest != null)
        {
            return healRequest;
        }

        return CreateBasicAttackRequest(actor);
    }

    /// <summary>
    /// 광전사형 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>행동 요청</returns>
    private BattleActionRequest CreateBerserkerActionRequest(
        BattleUnit actor,
        EnemyAIProfileData aiProfile)
    {
        BattleActionRequest damageSkillRequest = TryCreateDamageSkillRequest(actor);

        if (damageSkillRequest != null)
        {
            return damageSkillRequest;
        }

        return CreateBasicAttackRequest(actor);
    }

    /// <summary>
    /// 랜덤형 행동 요청 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>행동 요청</returns>
    private BattleActionRequest CreateRandomActionRequest(
        BattleUnit actor,
        EnemyAIProfileData aiProfile)
    {
        int randomValue = Random.Range(0, 3);

        if (randomValue == 0)
        {
            BattleActionRequest healRequest = TryCreateHealRequest(actor, aiProfile.SelfDefenseHpRatio);

            if (healRequest != null)
            {
                return healRequest;
            }
        }

        if (randomValue == 1)
        {
            BattleActionRequest damageSkillRequest = TryCreateDamageSkillRequest(actor);

            if (damageSkillRequest != null)
            {
                return damageSkillRequest;
            }
        }

        return CreateBasicAttackRequest(actor);
    }

    /// <summary>
    /// 행동 후보 목록 생성
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    private void BuildActionCandidates(BattleUnit actor)
    {
        _actionCandidates.Clear();

        if (actor == null || actor.IsAlive == false)
        {
            return;
        }

        AddBasicAttackCandidates(actor);
        AddSkillCandidates(actor);
    }

    /// <summary>
    /// 기본 공격 후보 추가
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    private void AddBasicAttackCandidates(BattleUnit actor)
    {
        for (int i = 0; i < _aliveOpponents.Count; i++)
        {
            BattleUnit target = _aliveOpponents[i];

            if (target == null || target.IsAlive == false)
            {
                continue;
            }

            BattleActionRequest request = BattleActionRequest.CreateAttack(actor, target);
            EnemyAIActionCandidate candidate = new EnemyAIActionCandidate(
                request,
                null,
                target,
                true);

            candidate.SetExpectedDamage(EstimateBasicAttackDamage(actor, target));

            _actionCandidates.Add(candidate);
        }
    }

    /// <summary>
    /// 스킬 후보 추가
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    private void AddSkillCandidates(BattleUnit actor)
    {
        if (actor.SkillList == null || actor.SkillList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < actor.SkillList.Count; i++)
        {
            SkillData skillData = actor.SkillList[i];

            if (skillData == null)
            {
                continue;
            }

            if (actor.CanUseSkill(skillData) == false)
            {
                continue;
            }

            AddSkillCandidatesByTargetType(actor, skillData);
        }
    }

    /// <summary>
    /// 타겟 타입별 스킬 후보 추가
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    private void AddSkillCandidatesByTargetType(BattleUnit actor, SkillData skillData)
    {
        switch (skillData.TargetType)
        {
            case TargetType.SingleEnemy:
                AddSingleEnemySkillCandidates(actor, skillData);
                break;

            case TargetType.AllEnemies:
                AddAllEnemiesSkillCandidate(actor, skillData);
                break;

            case TargetType.SingleAlly:
                AddSingleAllySkillCandidates(actor, skillData);
                break;

            case TargetType.AllAllies:
                AddAllAlliesSkillCandidate(actor, skillData);
                break;

            case TargetType.Self:
                AddSelfSkillCandidate(actor, skillData);
                break;
        }
    }

    /// <summary>
    /// 단일 적 대상 스킬 후보 추가
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    private void AddSingleEnemySkillCandidates(BattleUnit actor, SkillData skillData)
    {
        for (int i = 0; i < _aliveOpponents.Count; i++)
        {
            BattleUnit target = _aliveOpponents[i];

            if (target == null || target.IsAlive == false)
            {
                continue;
            }

            BattleActionRequest request = BattleActionRequest.CreateSkill(actor, skillData, target);
            EnemyAIActionCandidate candidate = new EnemyAIActionCandidate(
                request,
                skillData,
                target,
                false);

            if (skillData.SkillType == SkillEffectType.Damage)
            {
                candidate.SetExpectedDamage(EstimateSkillDamage(actor, target, skillData));
                SetElementMatchupInfo(candidate, skillData, target);
            }

            SetStatusEffectInfo(candidate, skillData);

            _actionCandidates.Add(candidate);
        }
    }

    /// <summary>
    /// 전체 적 대상 스킬 후보 추가
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    private void AddAllEnemiesSkillCandidate(BattleUnit actor, SkillData skillData)
    {
        if (_aliveOpponents.Count <= 0)
        {
            return;
        }

        BattleActionRequest request = BattleActionRequest.CreateSkill(actor, skillData, null);
        EnemyAIActionCandidate candidate = new EnemyAIActionCandidate(
            request,
            skillData,
            null,
            false);

        if (skillData.SkillType == SkillEffectType.Damage)
        {
            int totalExpectedDamage = 0;

            for (int i = 0; i < _aliveOpponents.Count; i++)
            {
                totalExpectedDamage += EstimateSkillDamage(actor, _aliveOpponents[i], skillData);
            }

            candidate.SetExpectedDamage(totalExpectedDamage);
            SetElementMatchupInfo(candidate, skillData, _aliveOpponents);
        }

        SetStatusEffectInfo(candidate, skillData);

        _actionCandidates.Add(candidate);
    }

    /// <summary>
    /// 단일 아군 대상 스킬 후보 추가
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    private void AddSingleAllySkillCandidates(BattleUnit actor, SkillData skillData)
    {
        for (int i = 0; i < _aliveAllies.Count; i++)
        {
            BattleUnit target = _aliveAllies[i];

            if (target == null || target.IsAlive == false)
            {
                continue;
            }

            BattleActionRequest request = BattleActionRequest.CreateSkill(actor, skillData, target);
            EnemyAIActionCandidate candidate = new EnemyAIActionCandidate(
                request,
                skillData,
                target,
                false);

            if (skillData.SkillType == SkillEffectType.Heal)
            {
                candidate.SetExpectedHeal(EstimateSkillHeal(actor, target, skillData));
            }

            _actionCandidates.Add(candidate);
        }
    }

    /// <summary>
    /// 전체 아군 대상 스킬 후보 추가
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    private void AddAllAlliesSkillCandidate(BattleUnit actor, SkillData skillData)
    {
        if (_aliveAllies.Count <= 0)
        {
            return;
        }

        BattleActionRequest request = BattleActionRequest.CreateSkill(actor, skillData, null);
        EnemyAIActionCandidate candidate = new EnemyAIActionCandidate(
            request,
            skillData,
            null,
            false);

        if (skillData.SkillType == SkillEffectType.Heal)
        {
            int totalExpectedHeal = 0;

            for (int i = 0; i < _aliveAllies.Count; i++)
            {
                totalExpectedHeal += EstimateSkillHeal(actor, _aliveAllies[i], skillData);
            }

            candidate.SetExpectedHeal(totalExpectedHeal);
        }

        _actionCandidates.Add(candidate);
    }

    /// <summary>
    /// 자기 대상 스킬 후보 추가
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    private void AddSelfSkillCandidate(BattleUnit actor, SkillData skillData)
    {
        BattleActionRequest request = BattleActionRequest.CreateSkill(actor, skillData, null);
        EnemyAIActionCandidate candidate = new EnemyAIActionCandidate(
            request,
            skillData,
            actor,
            false);

        if (skillData.SkillType == SkillEffectType.Heal)
        {
            candidate.SetExpectedHeal(EstimateSkillHeal(actor, actor, skillData));
        }

        _actionCandidates.Add(candidate);
    }

    /// <summary>
    /// 기본 공격 예상 피해 계산
    /// </summary>
    /// <param name="actor">공격 유닛</param>
    /// <param name="target">대상 유닛</param>
    /// <returns>예상 피해</returns>
    private int EstimateBasicAttackDamage(BattleUnit actor, BattleUnit target)
    {
        if (actor == null || target == null)
        {
            return 0;
        }

        float rawDamage = actor.AttackPower - target.DefensePower * 0.5f;
        return Mathf.Max(1, Mathf.RoundToInt(rawDamage));
    }

    /// <summary>
    /// 스킬 예상 피해 계산
    /// </summary>
    /// <param name="actor">사용 유닛</param>
    /// <param name="target">대상 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <returns>예상 피해</returns>
    private int EstimateSkillDamage(
        BattleUnit actor,
        BattleUnit target,
        SkillData skillData)
    {
        if (actor == null || target == null || skillData == null)
        {
            return 0;
        }

        if (target.IsAbsorbTo(skillData.ElementType))
        {
            return 0;
        }

        if (target.IsNullTo(skillData.ElementType))
        {
            return 0;
        }

        float attackValue = skillData.DamageType == DamageType.Magical
            ? actor.MagicPower
            : actor.AttackPower;

        float defenseValue = skillData.DamageType == DamageType.Magical
            ? target.MagicDefensePower
            : target.DefensePower;

        float rawDamage = attackValue + skillData.Power - defenseValue * 0.5f;

        if (target.IsWeakTo(skillData.ElementType))
        {
            rawDamage *= 1.5f;
        }
        else if (target.IsResistTo(skillData.ElementType))
        {
            rawDamage *= 0.5f;
        }

        return Mathf.Max(1, Mathf.RoundToInt(rawDamage));
    }

    /// <summary>
    /// 스킬 예상 회복 계산
    /// </summary>
    /// <param name="actor">사용 유닛</param>
    /// <param name="target">대상 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <returns>예상 회복량</returns>
    private int EstimateSkillHeal(
        BattleUnit actor,
        BattleUnit target,
        SkillData skillData)
    {
        if (actor == null || target == null || skillData == null)
        {
            return 0;
        }

        int missingHp = target.MaxHp - target.CurrentHp;

        if (missingHp <= 0)
        {
            return 0;
        }

        float rawHeal = actor.MagicPower + skillData.Power;
        int expectedHeal = Mathf.RoundToInt(rawHeal);

        return Mathf.Clamp(expectedHeal, 0, missingHp);
    }

    /// <summary>
    /// 약점 공략 가능 여부 확인
    /// </summary>
    /// <param name="target">대상 유닛</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <returns>약점 공략 가능 여부</returns>
    private bool CanExploitWeakness(BattleUnit target, SkillData skillData)
    {
        if (target == null || skillData == null)
        {
            return false;
        }

        return target.IsWeakTo(skillData.ElementType);
    }

    /// <summary>
    /// 행동 후보 점수 계산
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    private void ScoreActionCandidates(
        BattleUnit actor,
        EnemyAIProfileData aiProfile)
    {
        for (int i = 0; i < _actionCandidates.Count; i++)
        {
            EnemyAIActionCandidate candidate = _actionCandidates[i];

            if (candidate == null || candidate.Request == null || candidate.IsInvalid)
            {
                continue;
            }

            candidate.ResetScore();

            AddCandidateScore(
                candidate,
                GetBaseActionScore(candidate, aiProfile),
                "기본");

            AddCandidateScore(
                candidate,
                GetDamageScore(candidate, aiProfile),
                "피해");

            AddCandidateScore(
                candidate,
                GetElementMatchupScore(candidate, aiProfile),
                "상성");

            AddCandidateScore(
                candidate,
                GetStatusEffectScore(candidate, aiProfile),
                "상태이상");

            AddCandidateScore(
                candidate,
                GetHealScore(actor, candidate, aiProfile),
                "회복");

            AddCandidateScore(
                candidate,
                GetTargetPreferenceScore(candidate, aiProfile),
                "대상선호");

            AddCandidateScore(
                candidate,
                GetRandomScore(aiProfile),
                "랜덤");
        }
    }

    /// <summary>
    /// 기본 행동 점수 반환
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>기본 행동 점수</returns>
    private float GetBaseActionScore(
        EnemyAIActionCandidate candidate,
        EnemyAIProfileData aiProfile)
    {
        if (candidate == null)
        {
            return 0f;
        }

        if (candidate.IsBasicAttack)
        {
            return 5f;
        }

        if (candidate.SkillData == null)
        {
            return 0f;
        }

        //// 별자리 확인용
        //if (candidate.SkillData.IsConstellationPathAttack)
        //{
        //    return 1000f;
        //}

        switch (candidate.SkillData.SkillType)
        {
            case SkillEffectType.Damage:
                return 10f * aiProfile.DamageWeight;

            case SkillEffectType.Heal:
                return 10f * aiProfile.HealWeight;

            case SkillEffectType.Buff:
                return 8f * aiProfile.BuffWeight;

            case SkillEffectType.Debuff:
                return 8f * aiProfile.DebuffWeight;

            case SkillEffectType.StatusEffect:
                return 8f * aiProfile.StatusEffectWeight;
        }

        return 0f;
    }

    /// <summary>
    /// 공격 행동 점수 반환
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>공격 행동 점수</returns>
    private float GetDamageScore(
        EnemyAIActionCandidate candidate,
        EnemyAIProfileData aiProfile)
    {
        if (candidate == null || candidate.IsDamageAction == false)
        {
            return 0f;
        }

        float score = 0f;

        score += candidate.ExpectedDamage * aiProfile.DamageWeight;

        if (candidate.CanKillTarget)
        {
            score += 40f * aiProfile.KillWeight;
        }

        return score;
    }

    /// <summary>
    /// 회복 행동 점수 반환
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="candidate">행동 후보</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>회복 행동 점수</returns>
    private float GetHealScore(
        BattleUnit actor,
        EnemyAIActionCandidate candidate,
        EnemyAIProfileData aiProfile)
    {
        if (candidate == null || candidate.IsHealAction == false)
        {
            return 0f;
        }

        float score = 0f;

        score += candidate.ExpectedHeal * aiProfile.HealWeight;

        if (candidate.Target != null)
        {
            float missingHpRatio = 1f - GetHpRatio(candidate.Target);
            score += missingHpRatio * 50f * aiProfile.HealWeight;
        }

        if (candidate.Target == actor && GetHpRatio(actor) <= aiProfile.SelfDefenseHpRatio)
        {
            score += 50f * aiProfile.SelfSurvivalWeight;
        }

        return score;
    }

    /// <summary>
    /// 대상 선호 점수 반환
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>대상 선호 점수</returns>
    private float GetTargetPreferenceScore(
        EnemyAIActionCandidate candidate,
        EnemyAIProfileData aiProfile)
    {
        if (candidate == null || candidate.Target == null)
        {
            return 0f;
        }

        float score = 0f;

        float targetHpRatio = GetHpRatio(candidate.Target);
        float lowHpScore = 1f - targetHpRatio;

        score += lowHpScore * 30f * aiProfile.LowHpTargetWeight;
        score += GetThreatScore(candidate.Target) * aiProfile.HighThreatTargetWeight;
        score += Random.Range(0f, 10f) * aiProfile.RandomTargetWeight;

        return score;
    }

    /// <summary>
    /// 대상 위협도 점수 반환
    /// </summary>
    /// <param name="unit">대상 유닛</param>
    /// <returns>위협도 점수</returns>
    private float GetThreatScore(BattleUnit unit)
    {
        if (unit == null)
        {
            return 0f;
        }

        float offensivePower = unit.AttackPower + unit.MagicPower;
        float speedPower = unit.Speed * 0.5f;

        return offensivePower + speedPower;
    }

    /// <summary>
    /// 랜덤 행동 점수 반환
    /// </summary>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>랜덤 행동 점수</returns>
    private float GetRandomScore(EnemyAIProfileData aiProfile)
    {
        if (aiProfile == null)
        {
            return 0f;
        }

        return Random.Range(0f, 10f) * aiProfile.RandomActionWeight;
    }

    /// <summary>
    /// 최고 점수 행동 후보 선택
    /// </summary>
    /// <returns>선택된 행동 후보</returns>
    private EnemyAIActionCandidate SelectBestCandidate()
    {
        EnemyAIActionCandidate bestCandidate = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < _actionCandidates.Count; i++)
        {
            EnemyAIActionCandidate candidate = _actionCandidates[i];

            if (candidate == null || candidate.Request == null || candidate.IsInvalid)
            {
                continue;
            }

            if (candidate.Score <= bestScore)
            {
                continue;
            }

            bestScore = candidate.Score;
            bestCandidate = candidate;
        }

        return bestCandidate;
    }

    /// <summary>
    /// 후보에 속성 상성 정보 설정
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="targets">대상 목록</param>
    private void SetElementMatchupInfo(
        EnemyAIActionCandidate candidate,
        SkillData skillData,
        IReadOnlyList<BattleUnit> targets)
    {
        if (candidate == null || skillData == null || targets == null)
        {
            return;
        }

        int weaknessCount = 0;
        int resistCount = 0;
        int nullCount = 0;
        int absorbCount = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit target = targets[i];

            if (target == null || target.IsAlive == false)
            {
                continue;
            }

            if (target.IsAbsorbTo(skillData.ElementType))
            {
                absorbCount++;
                continue;
            }

            if (target.IsNullTo(skillData.ElementType))
            {
                nullCount++;
                continue;
            }

            if (target.IsResistTo(skillData.ElementType))
            {
                resistCount++;
                continue;
            }

            if (target.IsWeakTo(skillData.ElementType))
            {
                weaknessCount++;
            }
        }

        candidate.SetElementMatchupCounts(
            weaknessCount,
            resistCount,
            nullCount,
            absorbCount);
    }

    /// <summary>
    /// 후보에 단일 대상 속성 상성 정보 설정
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="target">대상 유닛</param>
    private void SetElementMatchupInfo(
        EnemyAIActionCandidate candidate,
        SkillData skillData,
        BattleUnit target)
    {
        if (target == null)
        {
            return;
        }

        List<BattleUnit> targets = new List<BattleUnit>
    {
        target
    };

        SetElementMatchupInfo(candidate, skillData, targets);
    }

    /// <summary>
    /// 속성 상성 점수 반환
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>속성 상성 점수</returns>
    private float GetElementMatchupScore(
        EnemyAIActionCandidate candidate,
        EnemyAIProfileData aiProfile)
    {
        if (candidate == null || candidate.IsDamageAction == false)
        {
            return 0f;
        }

        float score = 0f;

        score += candidate.WeaknessHitCount * 30f * aiProfile.WeaknessWeight;
        score -= candidate.ResistHitCount * 15f * aiProfile.DamageWeight;
        score -= candidate.NullHitCount * 80f * aiProfile.DamageWeight;
        score -= candidate.AbsorbHitCount * 120f * aiProfile.DamageWeight;

        return score;
    }

    /// <summary>
    /// 후보에 상태이상 정보 설정
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <param name="skillData">스킬 데이터</param>
    private void SetStatusEffectInfo(
        EnemyAIActionCandidate candidate,
        SkillData skillData)
    {
        if (candidate == null || skillData == null)
        {
            return;
        }

        if (skillData.StatusChance <= 0f)
        {
            return;
        }

        candidate.SetStatusEffectInfo(
            skillData.StatusEffectType,
            skillData.StatusChance);
    }

    /// <summary>
    /// 상태이상 행동 점수 반환
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <param name="aiProfile">AI 성향 데이터</param>
    /// <returns>상태이상 행동 점수</returns>
    private float GetStatusEffectScore(
        EnemyAIActionCandidate candidate,
        EnemyAIProfileData aiProfile)
    {
        if (candidate == null || candidate.IsStatusEffectAction == false)
        {
            return 0f;
        }

        float score = 0f;

        score += candidate.StatusEffectChance * 40f * aiProfile.StatusEffectWeight;

        if (candidate.CanExploitWeakness)
        {
            score += 10f * aiProfile.WeaknessWeight;
        }

        return score;
    }

    /// <summary>
    /// 행동 후보 유효성 검사
    /// </summary>
    private void ValidateActionCandidates()
    {
        for (int i = 0; i < _actionCandidates.Count; i++)
        {
            EnemyAIActionCandidate candidate = _actionCandidates[i];

            if (candidate == null)
            {
                continue;
            }

            ValidateHealCandidate(candidate);
            ValidateDamageCandidate(candidate);
        }
    }

    /// <summary>
    /// 회복 후보 유효성 검사
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    private void ValidateHealCandidate(EnemyAIActionCandidate candidate)
    {
        if (candidate == null || candidate.IsHealAction == false)
        {
            return;
        }

        if (candidate.ExpectedHeal > 0)
        {
            return;
        }

        candidate.SetInvalid("회복량 없음");
    }

    /// <summary>
    /// 공격 후보 유효성 검사
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    private void ValidateDamageCandidate(EnemyAIActionCandidate candidate)
    {
        if (candidate == null || candidate.IsDamageAction == false)
        {
            return;
        }

        if (candidate.HasBadElementMatchup)
        {
            candidate.SetInvalid("무효 또는 흡수 상성");
            return;
        }

        if (candidate.ExpectedDamage > 0)
        {
            return;
        }

        candidate.SetInvalid("피해량 없음");
    }

    /// <summary>
    /// 행동 후보에 점수 추가
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <param name="score">추가 점수</param>
    /// <param name="reason">점수 사유</param>
    private void AddCandidateScore(
        EnemyAIActionCandidate candidate,
        float score,
        string reason)
    {
        if (candidate == null)
        {
            return;
        }

        if (Mathf.Approximately(score, 0f))
        {
            return;
        }

        candidate.AddScore(score, reason);
    }

    /// <summary>
    /// 행동 후보 목록 로그 출력
    /// </summary>
    /// <param name="actor">행동 유닛</param>
    /// <param name="selectedCandidate">선택된 후보</param>
    private void LogActionCandidates(
        BattleUnit actor,
        EnemyAIActionCandidate selectedCandidate)
    {
        if (IsDebugLogEnabled == false)
        {
            return;
        }

        Debug.Log($"[EnemyBattleAI] Candidate List / Actor: {actor.UnitName}");

        for (int i = 0; i < _actionCandidates.Count; i++)
        {
            EnemyAIActionCandidate candidate = _actionCandidates[i];

            if (candidate == null)
            {
                continue;
            }

            Debug.Log(GetCandidateLogText(i, candidate));
        }

        if (selectedCandidate != null)
        {
            Debug.Log(
                $"[EnemyBattleAI] Selected: {GetCandidateName(selectedCandidate)} / " +
                $"Score: {selectedCandidate.Score:F1} / " +
                $"Reason: {selectedCandidate.ScoreReason}");
        }
    }

    /// <summary>
    /// 행동 후보 로그 문구 반환
    /// </summary>
    /// <param name="index">후보 인덱스</param>
    /// <param name="candidate">행동 후보</param>
    /// <returns>로그 문구</returns>
    private string GetCandidateLogText(
        int index,
        EnemyAIActionCandidate candidate)
    {
        string invalidText = candidate.IsInvalid
            ? $"True / Reason: {candidate.InvalidReason}"
            : "False";

        return
            $"[{index}] {GetCandidateName(candidate)} / " +
            $"Score: {candidate.Score:F1} / " +
            $"Damage: {candidate.ExpectedDamage} / " +
            $"Heal: {candidate.ExpectedHeal} / " +
            $"Weak: {candidate.WeaknessHitCount} / " +
            $"Resist: {candidate.ResistHitCount} / " +
            $"Null: {candidate.NullHitCount} / " +
            $"Absorb: {candidate.AbsorbHitCount} / " +
            $"Invalid: {invalidText} / " +
            $"Reason: {candidate.ScoreReason}";
    }

    /// <summary>
    /// 행동 후보 이름 반환
    /// </summary>
    /// <param name="candidate">행동 후보</param>
    /// <returns>행동 후보 이름</returns>
    private string GetCandidateName(EnemyAIActionCandidate candidate)
    {
        if (candidate == null)
        {
            return "None";
        }

        string actionName = candidate.SkillData != null
            ? candidate.SkillData.SkillName
            : "Basic Attack";

        string targetName = candidate.Target != null
            ? candidate.Target.UnitName
            : "All";

        return $"{actionName} -> {targetName}";
    }
}