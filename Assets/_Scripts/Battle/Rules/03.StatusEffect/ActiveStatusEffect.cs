using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 특정 유닛에게 실제로 걸려있는 상태이상 인스턴스
    /// 남은 턴, 중첩 수 등 런타임 정보 보관
    /// </summary>
    public class ActiveStatusEffect
    {
        private readonly StatusEffectData _data;
        private readonly BattleUnit _target;
        private int _remainingTurns;
        private int _stackCount;

        public StatusEffectData Data => _data;
        public BattleUnit Target => _target;
        public int RemainingTurns => _remainingTurns;
        public int StackCount => _stackCount;

        public StatusEffectType StatusEffectType => _data != null ? _data.StatusEffectType : StatusEffectType.None;

        /// <summary>
        /// 상태이상 인스턴스 생성
        /// </summary>
        /// <param name="data">상태이상 정의 데이터</param>
        /// <param name="target">부여 대상</param>
        public ActiveStatusEffect(StatusEffectData data, BattleUnit target)
        {
            _data = data;
            _target = target;
            _remainingTurns = data != null ? data.Duration : 0;
            _stackCount = 1;
        }

        /// <summary>
        /// 지속 턴 감소 (턴 종료 시 호출)
        /// </summary>
        /// <returns>지속 턴이 다 되어 만료 되었으면 true</returns>
        public bool DecreaseTurn()
        {
            _remainingTurns--;
            return _remainingTurns <= 0;
        }

        /// <summary>
        /// 중첩 추가 (같은 상태이상이 또 걸릴 때)
        /// </summary>
        public void AddStack()
        {
            if (_data == null || _data.CanStack == false)
            {
                // 중첩 불가면 지속턴만 갱신
                _remainingTurns = _data != null ? _data.Duration : _remainingTurns;
                return;
            }

            _stackCount = Mathf.Min(_stackCount + 1, _data.MaxStack);
            _remainingTurns = _data.Duration;   // 지속턴 리셋
        }

        /// <summary>
        /// 즉시 해제
        /// </summary>
        public void Remove()
        {
            _remainingTurns = 0;
            _stackCount = 0;
        }

        /// <summary>
        /// 매턴 지속 피해 계산
        /// </summary>
        /// <returns>매턴 피해량 (0 이상, 지속 피해 없으면 0)</returns>
        public int CalculateTickDamage()
        {
            if (_data == null || _target == null)
            {
                return 0;
            }

            int baseDamage = _data.CalculateTickDamage(_target.MaxHp);
            return baseDamage * _stackCount;    // 중첩 수만큼 피해 배증
        }
    }
}
