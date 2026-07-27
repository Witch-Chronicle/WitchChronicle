using System.Collections.Generic;
using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 모든 유닛의 상태이상을 통합 관리
    /// 부여, 해제, 조회, 매턴 처리 담당
    /// </summary>
    public class StatusEffectController
    {
        // 유닛별 활성 상태이상 목록
        private readonly Dictionary<BattleUnit, List<ActiveStatusEffect>> _activeEffects
            = new Dictionary<BattleUnit, List<ActiveStatusEffect>>();

        // 재사용용 임시 리스트 (매턴 처리 시 콜렉션 수정 회피)
        private readonly List<ActiveStatusEffect> _tempEffectList = new List<ActiveStatusEffect>();

        // ============ 연출용 알림 이벤트 (판정 없음, 통지만) ============
        /// <summary>상태이상이 부여됐을 때 (대상, 종류)</summary>
        public event System.Action<BattleUnit, StatusEffectType> OnApplied;
        /// <summary>상태이상이 해제/만료됐을 때 (대상, 종류)</summary>
        public event System.Action<BattleUnit, StatusEffectType> OnRemoved;

        // ============ 조회 ============

        /// <summary>
        /// 특정 유닛에게 걸린 모든 상태이상 반환
        /// </summary>
        public IReadOnlyList<ActiveStatusEffect> GetActiveEffects(BattleUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            if (_activeEffects.TryGetValue(unit, out List<ActiveStatusEffect> effects))
            {
                return effects;
            }

            return null;
        }

        /// <summary>
        /// 특정 유닛이 특정 상태이상에 걸려있는지 확인
        /// </summary>
        public bool HasStatusEffect(BattleUnit unit, StatusEffectType type)
        {
            return GetActiveEffect(unit, type) != null;
        }

        /// <summary>
        /// 특정 상태이상 인스턴스 반환 (없으면 null)
        /// </summary>
        public ActiveStatusEffect GetActiveEffect(BattleUnit unit, StatusEffectType type)
        {
            if (unit == null)
            {
                return null;
            }

            if (_activeEffects.TryGetValue(unit, out List<ActiveStatusEffect> effects) == false)
            {
                return null;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].StatusEffectType == type)
                {
                    return effects[i];
                }
            }

            return null;
        }

        // ============ 부여 ============

        /// <summary>
        /// 상태이상 부여
        /// 이미 걸려있으면 중첩 규칙에 따라 처리
        /// </summary>
        /// <param name="target">부여 대상</param>
        /// <param name="data">상태이상 데이터</param>
        /// <returns>부여 성공 여부</returns>
        public bool ApplyStatusEffect(BattleUnit target, StatusEffectData data)
        {
            if (target == null || data == null)
            {
                return false;
            }

            if (target.IsAlive == false)
            {
                return false;
            }

            // 이미 걸려있는지 확인
            ActiveStatusEffect existing = GetActiveEffect(target, data.StatusEffectType);

            if (existing != null)
            {
                // 이미 있으면 중첩 처리
                existing.AddStack();
                Debug.Log($"[StatusEffect] {target.UnitName}: {data.StatusName} 갱신/중첩");
                OnApplied?.Invoke(target, data.StatusEffectType);
                return true;
            }

            // 새로 부여
            ActiveStatusEffect newEffect = new ActiveStatusEffect(data, target);

            if (_activeEffects.TryGetValue(target, out List<ActiveStatusEffect> effects) == false)
            {
                effects = new List<ActiveStatusEffect>();
                _activeEffects[target] = effects;
            }

            effects.Add(newEffect);
            Debug.Log($"[StatusEffect] {target.UnitName}: {data.StatusName} 부여");
            OnApplied?.Invoke(target, data.StatusEffectType);
            return true;
        }

        // ============ 해제 ============

        /// <summary>
        /// 특정 유닛의 특정 상태이상 해제
        /// </summary>
        /// <returns>해제 성공 여부</returns>
        public bool RemoveStatusEffect(BattleUnit unit, StatusEffectType type)
        {
            if (unit == null)
            {
                return false;
            }

            if (_activeEffects.TryGetValue(unit, out List<ActiveStatusEffect> effects) == false)
            {
                return false;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                if (effects[i].StatusEffectType == type)
                {
                    effects[i].Remove();
                    effects.RemoveAt(i);
                    Debug.Log($"[StatusEffect] {unit.UnitName}: {type} 해제");
                    OnRemoved?.Invoke(unit, type);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 특정 유닛의 모든 상태이상 해제
        /// </summary>
        public void RemoveAllStatusEffects(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            if (_activeEffects.TryGetValue(unit, out List<ActiveStatusEffect> effects) == false)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffectType removedType = effects[i].StatusEffectType;
                effects[i].Remove();
                OnRemoved?.Invoke(unit, removedType);
            }

            effects.Clear();
        }

        // ============ 턴 처리 ============

        /// <summary>
        /// 턴 시작 시 처리 (지속 피해 적용)
        /// 화상, 독 등 tick damage 상태이상의 매턴 피해
        /// </summary>
        /// <param name="unit">현재 턴 유닛</param>
        public void ProcessTurnStart(BattleUnit unit)
        {
            if (unit == null || unit.IsAlive == false)
            {
                return;
            }

            if (_activeEffects.TryGetValue(unit, out List<ActiveStatusEffect> effects) == false)
            {
                return;
            }

            _tempEffectList.Clear();
            _tempEffectList.AddRange(effects);

            for (int i = 0; i < _tempEffectList.Count; i++)
            {
                ActiveStatusEffect effect = _tempEffectList[i];

                if (effect.Data == null || effect.Data.HasTickDamage == false)
                {
                    continue;
                }

                int tickDamage = effect.CalculateTickDamage();

                if (tickDamage <= 0)
                {
                    continue;
                }

                unit.TakeDamage(tickDamage);
                Debug.Log($"[StatusEffect] {unit.UnitName}: {effect.Data.StatusName} {tickDamage} 지속 피해");

                if (unit.IsAlive == false)
                {
                    // 지속 피해로 사망 시 상태이상 정리
                    RemoveAllStatusEffects(unit);
                    return;
                }
            }
        }

        /// <summary>
        /// 턴 종료 시 처리 (지속턴 감소, 만료 상태이상 제거)
        /// </summary>
        /// <param name="unit">현재 턴 유닛</param>
        public void ProcessTurnEnd(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            if (_activeEffects.TryGetValue(unit, out List<ActiveStatusEffect> effects) == false)
            {
                return;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                ActiveStatusEffect effect = effects[i];
                bool expired = effect.DecreaseTurn();

                if (expired)
                {
                    Debug.Log($"[StatusEffect] {unit.UnitName}: {effect.Data.StatusName} 만료");
                    effect.Remove();
                    effects.RemoveAt(i);
                    OnRemoved?.Invoke(unit, effect.StatusEffectType);
                }
            }
        }

        // ============ 행동 판정 ============

        /// <summary>
        /// 유닛이 행동 가능한지 판정
        /// 수면·마비 등에 의한 행동 실패 처리
        /// </summary>
        /// <param name="unit">확인 대상</param>
        /// <returns>true면 행동 가능, false면 행동 실패</returns>
        public bool CanAct(BattleUnit unit)
        {
            if (unit == null || unit.IsAlive == false)
            {
                return false;
            }

            if (_activeEffects.TryGetValue(unit, out List<ActiveStatusEffect> effects) == false)
            {
                return true;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffectData data = effects[i].Data;

                if (data == null)
                {
                    continue;
                }

                // 완전 행동 불가 (수면)
                if (data.PreventsAction)
                {
                    return false;
                }

                // 확률 행동 실패 (마비, 혼란)
                if (data.ActionFailChance > 0f && Random.value < data.ActionFailChance)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 유닛이 스킬을 사용할 수 있는지 판정
        /// 침묵 상태 확인
        /// </summary>
        public bool CanUseSkill(BattleUnit unit)
        {
            if (unit == null || unit.IsAlive == false)
            {
                return false;
            }

            if (_activeEffects.TryGetValue(unit, out List<ActiveStatusEffect> effects) == false)
            {
                return true;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Data == null)
                {
                    continue;
                }

                if (effects[i].Data.PreventsSkill)
                {
                    return false;
                }
            }

            return true;
        }

        // ============ 피격 시 처리 ============

        /// <summary>
        /// 피격 시 자동 해제 상태이상 처리 (수면 등)
        /// </summary>
        /// <param name="unit">피격 유닛</param>
        public void OnUnitHit(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            if (_activeEffects.TryGetValue(unit, out List<ActiveStatusEffect> effects) == false)
            {
                return;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                if (effects[i].Data == null)
                {
                    continue;
                }

                if (effects[i].Data.RemoveOnHit)
                {
                    Debug.Log($"[StatusEffect] {unit.UnitName}: {effects[i].Data.StatusName} 피격 해제");
                    effects[i].Remove();
                    effects.RemoveAt(i);
                }
            }
        }

        // ============ 전투 종료 ============

        /// <summary>
        /// 전투 종료 시 모든 상태이상 초기화
        /// </summary>
        public void ClearAll()
        {
            foreach (KeyValuePair<BattleUnit, List<ActiveStatusEffect>> pair in _activeEffects)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    pair.Value[i].Remove();
                }

                pair.Value.Clear();
            }

            _activeEffects.Clear();
        }
    }
}