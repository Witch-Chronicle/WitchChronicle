using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 상태이상 정의 데이터
    /// 각 상태이상 종류마다 하나씩 SO로 생성
    /// (화상, 독, 수면, 마비, 침묵, 혼란)
    /// </summary>
    [CreateAssetMenu(menuName = "WitchChronicle/StatusEffectData")]
    public class StatusEffectData : ScriptableObject
    {
        [Header("Status Info")]
        [SerializeField] private StatusEffectType _statusEffectType;
        [SerializeField] private string _statusName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Duration")]
        [Tooltip("지속 턴 수 (0 이하면 해제되지 않음)")]
        [SerializeField] private int _duration = 3;

        [Header("Stack")]
        [Tooltip("같은 상태이상 중첩 가능 여부")]
        [SerializeField] private bool _canStack = false;
        [Tooltip("최대 중첩 수 (canStack이 true일 때만 사용)")]
        [SerializeField] private int _maxStack = 1;

        [Header("Tick Damage")]
        [Tooltip("매턴 지속 피해 사용 여부 (화상, 독)")]
        [SerializeField] private bool _hasTickDamage = false;
        [Tooltip("매턴 피해량 (최대 HP 비율, 0.05 = 5%)")]
        [SerializeField] private float _tickDamageRatio = 0.0f;
        [Tooltip("고정 피해량 (0보다 크면 tickDamageRatio 대신 사용)")]
        [SerializeField] private int _tickDamageFixed = 0;

        [Header("Behavior Restriction")]
        [Tooltip("행동 불가 (수면, 마비 등)")]
        [SerializeField] private bool _preventsAction = false;
        [Tooltip("행동 실패 확률 (마비: 0.5, 혼란: 0.3 등)")]
        [SerializeField] private float _actionFailChance = 0.0f;
        [Tooltip("스킬 사용 불가 (침묵)")]
        [SerializeField] private bool _preventsSkill = false;
        [Tooltip("혼란: 공격이 빗나가(데미지 없음) 확률 (0~1)")]
        [SerializeField] private float _confusionMissChance = 0.0f;

        [Header("Removal Condition")]
        [Tooltip("피격 시 해제 (수면)")]
        [SerializeField] private bool _removeOnHit = false;

        // ============ 프로퍼티 ============
        public StatusEffectType StatusEffectType => _statusEffectType;
        public string StatusName => _statusName;
        public string Description => _description;
        public Sprite Icon => _icon;

        public int Duration => _duration;

        public bool CanStack => _canStack;
        public int MaxStack => _maxStack;

        public bool HasTickDamage => _hasTickDamage;
        public float TickDamageRatio => _tickDamageRatio;
        public int TickDamageFixed => _tickDamageFixed;

        public bool PreventsAction => _preventsAction;
        public float ActionFailChance => _actionFailChance;
        public bool PreventsSkill => _preventsSkill;
        public float ConfusionMissChance => _confusionMissChance;

        public bool RemoveOnHit => _removeOnHit;

        /// <summary>
        /// 대상에게 매턴 적용할 피해량 계산
        /// </summary>
        /// <param name="targetMaxHp">대상 최대 HP</param>
        /// <returns>매턴 피해량 (0 이상)</returns>
        public int CalculateTickDamage(int targetMaxHp)
        {
            if (_hasTickDamage == false)
            {
                return 0;
            }

            // 고정 피해가 지정되어 있으면 우선 적용
            if (_tickDamageFixed > 0)
            {
                return _tickDamageFixed;
            }

            // 비율 피해 계산
            int ratioDamage = Mathf.RoundToInt(targetMaxHp * _tickDamageRatio);
            return Mathf.Max(1, ratioDamage);
        }
    }
}