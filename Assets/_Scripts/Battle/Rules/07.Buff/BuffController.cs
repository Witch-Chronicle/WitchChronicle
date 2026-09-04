using System.Collections.Generic;

namespace Battle.Rules
{
    /// <summary>
    /// 전투 중 모든 유닛의 버프/디버프 관리
    /// BattleController에서 인스턴스 생성 → 턴 종료 시 ProcessTurnEnd 호출
    /// DamageResolver / BattleController가 배율 조회
    /// </summary>
    public class BuffController
    {
        private readonly Dictionary<BattleUnit, List<ActiveBuff>> _buffs
            = new Dictionary<BattleUnit, List<ActiveBuff>>();

        // 이번 라운드에 적용하거나 갱신한 버프
        private readonly HashSet<ActiveBuff> _appliedThisRound = new HashSet<ActiveBuff>();

        /// <summary>
        /// 버프/디버프 적용
        /// - 같은 BuffData가 이미 있고 스택 불가 → 지속시간 갱신
        /// - 같은 BuffData가 이미 있고 스택 가능 → 스택 증가 + 지속시간 갱신
        /// - 없으면 신규 추가
        /// </summary>
        public void ApplyBuff(BattleUnit target, BuffData buffData)
        {
            if (target == null || buffData == null) return;

            if (!_buffs.TryGetValue(target, out var list))
            {
                list = new List<ActiveBuff>();
                _buffs[target] = list;
            }

            ActiveBuff existing = list.Find(b => b.Data == buffData);

            if (existing != null)
            {
                if (buffData.CanStack)
                    existing.AddStack();

                existing.RefreshDuration();
                _appliedThisRound.Add(existing); // 갱신한 라운드의 지속시간 차감 제외
            }
            else
            {
                ActiveBuff newBuff = new ActiveBuff(buffData);
                list.Add(newBuff);
                _appliedThisRound.Add(newBuff);
            }
        }
         
        /// <summary>
        /// 특정 버프 제거
        /// </summary>
        public void RemoveBuff(BattleUnit target, BuffData buffData)
        {
            if (!_buffs.TryGetValue(target, out var list)) return;
            list.RemoveAll(b => b.Data == buffData);
        }

        /// <summary>
        /// 턴 종료 시 모든 버프 지속시간 감소, 만료된 것 제거
        /// </summary>
        public void ProcessTurnEnd()
        {
            foreach (var kvp in _buffs)
            {
                var list = kvp.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].DecreaseTurn())
                        list.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 마공 배율 조회 (여러 버프 중첩 시 곱셈)
        /// </summary>
        public float GetMagicAttackMultiplier(BattleUnit unit)
        {
            return GetMultiplierByType(unit, BuffType.MagicAttack);
        }

        /// <summary>
        /// 마방 배율 조회
        /// </summary>
        public float GetMagicDefenseMultiplier(BattleUnit unit)
        {
            return GetMultiplierByType(unit, BuffType.MagicDefense);
        }

        /// <summary>
        /// 속도 배율 조회 (BattleController가 턴 순서 계산 시 사용)
        /// </summary>
        public float GetSpeedMultiplier(BattleUnit unit)
        {
            return GetMultiplierByType(unit, BuffType.Speed);
        }

        private float GetMultiplierByType(BattleUnit unit, BuffType type)
        {
            if (unit == null || !_buffs.TryGetValue(unit, out var list))
                return 1f;

            float result = 1f;
            foreach (var buff in list)
            {
                if (buff.Data.BuffType == type)
                    result *= buff.GetEffectiveMultiplier();
            }
            return result;
        }

        /// <summary>
        /// UI 표시용 - 특정 유닛의 활성 버프 리스트
        /// </summary>
        public IReadOnlyList<ActiveBuff> GetActiveBuffs(BattleUnit unit)
        {
            if (unit == null || !_buffs.TryGetValue(unit, out var list))
                return System.Array.Empty<ActiveBuff>();
            return list;
        }

        /// <summary>
        /// 전투 종료 시 전체 초기화
        /// </summary>
        public void ClearAll()
        {
            _buffs.Clear();
            _appliedThisRound.Clear(); // 라운드 기록 초기화
        }

        /// <summary>
        /// 일반 라운드 시작 시 적용/갱신 기록 초기화
        /// </summary>
        public void BeginRound()
        {
            _appliedThisRound.Clear();
        }

        /// <summary>
        /// 일반 라운드 종료 시 버프 지속시간 감소
        /// 이번 라운드에 적용하거나 갱신한 버프는 차감 제외
        /// 기존 ProcessTurnEnd와 중복 호출 금지
        /// </summary>
        public void ProcessRoundEnd()
        {
            foreach (var kvp in _buffs)
            {
                var list = kvp.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (_appliedThisRound.Contains(list[i])) continue;

                    if (list[i].DecreaseTurn())
                        list.RemoveAt(i);
                }
            }
        }
    }
}