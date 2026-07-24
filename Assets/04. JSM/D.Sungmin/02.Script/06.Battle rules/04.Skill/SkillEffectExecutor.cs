using System.Collections.Generic;
using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 스킬 실행부
    /// SkillData를 받아 데미지·회복·상태이상·버프 등을 실제로 적용
    /// 단일 대상: Execute()
    /// 전체 대상: ExecuteMultiple() - MP는 1번만 차감
    /// </summary>
    public class SkillEffectExecutor
    {
        private readonly StatusEffectController _statusEffectController;
        private readonly StatusEffectDatabase _statusEffectDatabase;
        private readonly BuffController _buffController;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="statusEffectController">상태이상 관리자</param>
        /// <param name="statusEffectDatabase">상태이상 데이터베이스</param>
        /// <param name="buffController">버프/디버프 관리자</param>
        public SkillEffectExecutor(
            StatusEffectController statusEffectController,
            StatusEffectDatabase statusEffectDatabase,
            BuffController buffController)
        {
            _statusEffectController = statusEffectController;
            _statusEffectDatabase = statusEffectDatabase;
            _buffController = buffController;
        }

        /// <summary>
        /// 스킬 실행 (단일 대상)
        /// </summary>
        public SkillExecuteResult Execute(BattleUnit caster, BattleUnit target, SkillData skillData)
        {
            SkillExecuteResult result = new SkillExecuteResult();

            if (ValidateCaster(caster, skillData) == false)
            {
                result.Success = false;
                return result;
            }

            if (target == null)
            {
                Debug.LogWarning("[SkillEffectExecutor] 대상이 null입니다");
                result.Success = false;
                return result;
            }

            // MP 차감 + 침묵 체크 (1번만)
            if (ConsumeCastCost(caster, skillData) == false)
            {
                result.Success = false;
                return result;
            }

            // 실제 효과 적용
            ApplyEffectToTarget(caster, target, skillData, ref result);

            result.Success = true;
            return result;
        }

        /// <summary>
        /// 스킬 실행 (다중 대상)
        /// MP 차감·침묵 체크는 1번만 하고, 각 대상에 효과 반복 적용
        /// 전체 스킬(AllAllies, AllEnemies)이나 다중 표적 스킬에서 사용
        /// </summary>
        /// <returns>대상별 실행 결과 리스트 (성공 시), 실패 시 빈 리스트</returns>
        public List<SkillExecuteResult> ExecuteMultiple(BattleUnit caster, List<BattleUnit> targets, SkillData skillData)
        {
            List<SkillExecuteResult> results = new List<SkillExecuteResult>();

            if (ValidateCaster(caster, skillData) == false)
            {
                return results;
            }

            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning("[SkillEffectExecutor] 대상 리스트가 비어있습니다");
                return results;
            }

            // MP 차감 + 침묵 체크 (1번만)
            if (ConsumeCastCost(caster, skillData) == false)
            {
                return results;
            }

            // 각 대상에 효과 적용
            for (int i = 0; i < targets.Count; i++)
            {
                BattleUnit target = targets[i];
                if (target == null)
                {
                    continue;
                }

                SkillExecuteResult subResult = new SkillExecuteResult();
                ApplyEffectToTarget(caster, target, skillData, ref subResult);
                subResult.Success = true;
                results.Add(subResult);
            }

            return results;
        }

        /// <summary>
        /// 시전자 유효성 검사 (Execute/ExecuteMultiple 공통)
        /// </summary>
        private bool ValidateCaster(BattleUnit caster, SkillData skillData)
        {
            if (caster == null || skillData == null)
            {
                Debug.LogWarning("[SkillEffectExecutor] 시전자 또는 스킬 데이터가 null입니다");
                return false;
            }

            if (caster.IsAlive == false)
            {
                Debug.LogWarning($"[SkillEffectExecutor] {caster.UnitName}은 사망 상태입니다");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 시전 비용 처리 - MP 차감 + 침묵 체크
        /// </summary>
        private bool ConsumeCastCost(BattleUnit caster, SkillData skillData)
        {
            // 1. MP 확인 및 차감
            if (caster.CanUseSkill(skillData) == false)
            {
                Debug.Log($"[SkillEffectExecutor] {caster.UnitName}의 MP 부족");
                return false;
            }

            caster.UseMp(skillData.MpCost);

            // 2. 침묵 상태 확인 (스킬 봉인)
            if (_statusEffectController != null && _statusEffectController.CanUseSkill(caster) == false)
            {
                Debug.Log($"[SkillEffectExecutor] {caster.UnitName}은 침묵 상태입니다");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 대상에 효과 적용 - SkillType별 분기
        /// </summary>
        private void ApplyEffectToTarget(BattleUnit caster, BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            switch (skillData.SkillType)
            {
                case SkillEffectType.Damage:
                    ExecuteDamage(caster, target, skillData, ref result);
                    break;

                case SkillEffectType.Heal:
                    ExecuteHeal(target, skillData, ref result);
                    break;

                case SkillEffectType.HealMp:
                    ExecuteHealMp(target, skillData, ref result);
                    break;

                case SkillEffectType.StatusEffect:
                    ExecuteStatusEffect(target, skillData, ref result);
                    break;

                case SkillEffectType.Buff:
                case SkillEffectType.Debuff:
                    ExecuteBuff(target, skillData, ref result);
                    break;

                case SkillEffectType.Revive:
                    ExecuteRevive(target, skillData, ref result);
                    break;

                default:
                    Debug.LogWarning($"[SkillEffectExecutor] 알 수 없는 SkillEffectType: {skillData.SkillType}");
                    break;
            }
        }

        /// <summary>
        /// 데미지 스킬 실행
        /// </summary>
        private void ExecuteDamage(BattleUnit caster, BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false)
            {
                return;
            }

            // 데미지 계산 (버프 배율 반영)
            DamageResult damageResult = DamageResolver.Calculate(caster, target, skillData, _buffController);
            result.DamageResult = damageResult;

            // 미스가 아니면 데미지 적용
            if (damageResult.IsMiss == false && damageResult.FinalDamage > 0)
            {
                target.TakeDamage(damageResult.FinalDamage);

                // 피격 시 자동 해제 상태이상 처리 (수면 등)
                if (_statusEffectController != null)
                {
                    _statusEffectController.OnUnitHit(target);
                }

                Debug.Log($"[Skill] {caster.UnitName} → {target.UnitName}: {skillData.SkillName} {damageResult.FinalDamage} 데미지 ({damageResult.Affinity})");
            }
            else if (damageResult.IsMiss)
            {
                Debug.Log($"[Skill] {caster.UnitName} → {target.UnitName}: {skillData.SkillName} 빗나감");
            }

            // 데미지 스킬에 부가된 상태이상 확률 처리
            if (target.IsAlive && damageResult.IsMiss == false)
            {
                TryApplyStatusEffect(target, skillData, ref result);
            }
        }

        /// <summary>
        /// HP 회복 스킬 실행
        /// </summary>
        private void ExecuteHeal(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false)
            {
                return;
            }

            int healAmount = skillData.Power;
            target.Heal(healAmount);
            result.HealAmount = healAmount;

            Debug.Log($"[Skill] {target.UnitName}: {skillData.SkillName} HP {healAmount} 회복");
        }

        /// <summary>
        /// MP 회복 스킬 실행
        /// BattleUnit.RestoreMp() 메서드가 코어 담당자에 의해 추가되면 활성화 필요
        /// </summary>
        private void ExecuteHealMp(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false)
            {
                return;
            }

            int mpAmount = skillData.Power;

            // TODO: BattleUnit에 RestoreMp(int amount) 메서드 추가 대기 중
            // target.RestoreMp(mpAmount);

            result.MpHealAmount = mpAmount;

            Debug.Log($"[Skill] {target.UnitName}: {skillData.SkillName} MP {mpAmount} 회복 (RestoreMp 대기 중)");
        }

        /// <summary>
        /// 상태이상 부여 전용 스킬 실행
        /// </summary>
        private void ExecuteStatusEffect(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false)
            {
                return;
            }

            TryApplyStatusEffect(target, skillData, ref result);
        }

        /// <summary>
        /// 버프/디버프 스킬 실행
        /// SkillData에 참조된 BuffData를 대상에게 적용
        /// </summary>
        private void ExecuteBuff(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false)
            {
                return;
            }

            if (_buffController == null)
            {
                Debug.LogWarning("[SkillEffectExecutor] BuffController가 null입니다");
                return;
            }

            if (skillData.BuffData == null)
            {
                Debug.LogWarning($"[SkillEffectExecutor] {skillData.SkillName}에 BuffData가 지정되지 않았습니다");
                return;
            }

            _buffController.ApplyBuff(target, skillData.BuffData);
            result.AppliedBuff = skillData.BuffData;

            string buffLabel = skillData.BuffData.IsBuff ? "버프" : "디버프";
            Debug.Log($"[Skill] {target.UnitName}: {skillData.SkillName} {buffLabel} 적용 ({skillData.BuffData.BuffName})");
        }

        /// <summary>
        /// 부활 스킬 실행
        /// 사망 상태인 대상만 부활 가능
        /// </summary>
        private void ExecuteRevive(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive)
            {
                Debug.Log($"[Skill] {target.UnitName}은 살아있어 부활 대상이 아닙니다");
                return;
            }

            // Power를 회복량으로 사용
            int reviveHp = Mathf.Max(1, skillData.Power);
            target.Heal(reviveHp);
            result.RevivedTarget = target;

            Debug.Log($"[Skill] {target.UnitName}: {skillData.SkillName}으로 부활 (HP {reviveHp})");
        }

        /// <summary>
        /// 스킬 데이터의 상태이상 확률 판정 및 부여
        /// </summary>
        private void TryApplyStatusEffect(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (skillData.StatusEffectType == StatusEffectType.None)
            {
                return;
            }

            if (skillData.StatusChance <= 0f)
            {
                return;
            }

            if (_statusEffectController == null || _statusEffectDatabase == null)
            {
                Debug.LogWarning("[SkillEffectExecutor] StatusEffectController 또는 Database가 null입니다");
                return;
            }

            // 확률 판정
            if (Random.value > skillData.StatusChance)
            {
                return;
            }

            // 데이터 조회
            StatusEffectData statusData = _statusEffectDatabase.GetData(skillData.StatusEffectType);

            if (statusData == null)
            {
                Debug.LogWarning($"[SkillEffectExecutor] StatusEffectData 없음: {skillData.StatusEffectType}");
                return;
            }

            // 부여
            _statusEffectController.ApplyStatusEffect(target, statusData);
            result.AppliedStatusEffect = skillData.StatusEffectType;
        }
    }

    /// <summary>
    /// 스킬 실행 결과
    /// UI, 이펙트 시스템이 이 결과를 받아 연출
    /// </summary>
    public struct SkillExecuteResult
    {
        public bool Success;
        public DamageResult DamageResult;
        public int HealAmount;              // HP 회복량
        public int MpHealAmount;            // MP 회복량 (신규)
        public StatusEffectType AppliedStatusEffect;
        public BuffData AppliedBuff;
        public BattleUnit RevivedTarget;
    }
}