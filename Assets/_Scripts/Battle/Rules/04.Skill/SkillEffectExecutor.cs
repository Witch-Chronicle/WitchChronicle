using System.Collections.Generic;
using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 스킬 실행부
    /// SkillData를 받아 데미지·회복·상태이상·버프·정화 등을 실제로 적용
    /// 단일 대상: Execute()
    /// 전체 대상: ExecuteMultiple() - MP는 1번만 차감
    /// </summary>
    public class SkillEffectExecutor
    {
        private readonly StatusEffectController _statusEffectController;
        private readonly StatusEffectDatabase _statusEffectDatabase;
        private readonly BuffController _buffController;

        public SkillEffectExecutor(
            StatusEffectController statusEffectController,
            StatusEffectDatabase statusEffectDatabase,
            BuffController buffController)
        {
            _statusEffectController = statusEffectController;
            _statusEffectDatabase = statusEffectDatabase;
            _buffController = buffController;
        }

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

            if (ConsumeCastCost(caster, skillData) == false)
            {
                result.Success = false;
                return result;
            }

            ApplyEffectToTarget(caster, target, skillData, ref result);

            result.Success = true;
            return result;
        }

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

            if (ConsumeCastCost(caster, skillData) == false)
            {
                return results;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                BattleUnit target = targets[i];
                if (target == null) continue;

                SkillExecuteResult subResult = new SkillExecuteResult();
                ApplyEffectToTarget(caster, target, skillData, ref subResult);
                subResult.Success = true;
                results.Add(subResult);
            }

            return results;
        }

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

        private bool ConsumeCastCost(BattleUnit caster, SkillData skillData)
        {
            if (caster.CanUseSkill(skillData) == false)
            {
                Debug.Log($"[SkillEffectExecutor] {caster.UnitName}의 MP 부족");
                return false;
            }

            caster.UseMp(skillData.MpCost);

            if (_statusEffectController != null && _statusEffectController.CanUseSkill(caster) == false)
            {
                Debug.Log($"[SkillEffectExecutor] {caster.UnitName}은 침묵 상태입니다");
                return false;
            }

            return true;
        }

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

                case SkillEffectType.CureStatus:      // ⭐ 신규
                    ExecuteCureStatus(target, skillData, ref result);
                    break;

                default:
                    Debug.LogWarning($"[SkillEffectExecutor] 알 수 없는 SkillEffectType: {skillData.SkillType}");
                    break;
            }
        }

        private void ExecuteDamage(BattleUnit caster, BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false) return;

            DamageResult damageResult = DamageResolver.Calculate(caster, target, skillData, _buffController);
            result.DamageResult = damageResult;

            if (damageResult.IsMiss == false && damageResult.FinalDamage > 0)
            {
                target.TakeDamage(damageResult.FinalDamage);

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

            if (target.IsAlive && damageResult.IsMiss == false)
            {
                TryApplyStatusEffect(target, skillData, ref result);
            }
        }

        private void ExecuteHeal(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false) return;

            int healAmount = skillData.Power;
            target.Heal(healAmount);
            result.HealAmount = healAmount;

            Debug.Log($"[Skill] {target.UnitName}: {skillData.SkillName} HP {healAmount} 회복");
        }

        private void ExecuteHealMp(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false) return;

            int mpAmount = skillData.Power;

            // TODO: BattleUnit에 RestoreMp(int amount) 메서드 추가 대기 중
            // target.RestoreMp(mpAmount);

            result.MpHealAmount = mpAmount;

            Debug.Log($"[Skill] {target.UnitName}: {skillData.SkillName} MP {mpAmount} 회복 (RestoreMp 대기 중)");
        }

        private void ExecuteStatusEffect(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false) return;
            TryApplyStatusEffect(target, skillData, ref result);
        }

        private void ExecuteBuff(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false) return;

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

        private void ExecuteRevive(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive)
            {
                Debug.Log($"[Skill] {target.UnitName}은 살아있어 부활 대상이 아닙니다");
                return;
            }

            int reviveHp = Mathf.Max(1, skillData.Power);
            target.Heal(reviveHp);
            result.RevivedTarget = target;

            Debug.Log($"[Skill] {target.UnitName}: {skillData.SkillName}으로 부활 (HP {reviveHp})");
        }

        /// <summary>
        /// 정화 스킬 실행 - 특정 상태이상 하나 해제
        /// SkillData의 CureStatusEffectType에 지정된 상태이상만 해제
        /// </summary>
        private void ExecuteCureStatus(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (target.IsAlive == false) return;

            if (_statusEffectController == null)
            {
                Debug.LogWarning("[SkillEffectExecutor] StatusEffectController가 null입니다");
                return;
            }

            StatusEffectType cureType = skillData.CureStatusEffectType;

            if (cureType == StatusEffectType.None)
            {
                Debug.LogWarning($"[SkillEffectExecutor] {skillData.SkillName}의 CureStatusEffectType이 None입니다");
                return;
            }

            bool removed = _statusEffectController.RemoveStatusEffect(target, cureType);
            result.CuredStatusEffect = cureType;
            result.CureSuccess = removed;

            if (removed)
            {
                Debug.Log($"[Skill] {target.UnitName}: {skillData.SkillName}, {cureType} 해제");
            }
            else
            {
                Debug.Log($"[Skill] {target.UnitName}은 {cureType} 상태가 아님 (MP는 소모됨)");
            }
        }

        private void TryApplyStatusEffect(BattleUnit target, SkillData skillData, ref SkillExecuteResult result)
        {
            if (skillData.StatusEffectType == StatusEffectType.None) return;
            if (skillData.StatusChance <= 0f) return;

            if (_statusEffectController == null || _statusEffectDatabase == null)
            {
                Debug.LogWarning("[SkillEffectExecutor] StatusEffectController 또는 Database가 null입니다");
                return;
            }

            if (Random.value > skillData.StatusChance) return;

            StatusEffectData statusData = _statusEffectDatabase.GetData(skillData.StatusEffectType);

            if (statusData == null)
            {
                Debug.LogWarning($"[SkillEffectExecutor] StatusEffectData 없음: {skillData.StatusEffectType}");
                return;
            }

            _statusEffectController.ApplyStatusEffect(target, statusData);
            result.AppliedStatusEffect = skillData.StatusEffectType;
        }
    }

    public struct SkillExecuteResult
    {
        public bool Success;
        public DamageResult DamageResult;
        public int HealAmount;
        public int MpHealAmount;
        public StatusEffectType AppliedStatusEffect;
        public BuffData AppliedBuff;
        public BattleUnit RevivedTarget;
        public StatusEffectType CuredStatusEffect;   
        public bool CureSuccess;                     
    }
}