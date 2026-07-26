using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 버프/디버프 데이터 (ScriptableObject)
    /// Multiplier > 1 → 버프, Multiplier < 1 → 디버프
    /// </summary>
    [CreateAssetMenu(fileName = "New Buff Data", menuName = "Witch Chronicle/Buff Data")]
    public class BuffData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string _buffId;
        [SerializeField] private string _buffName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("효과")]
        [SerializeField] private BuffType _buffType = BuffType.MagicAttack;

        [Tooltip("배율 (1.2 = +20% 버프, 0.8 = -20% 디버프)")]
        [SerializeField] private float _multiplier = 1.2f;

        [Tooltip("지속 턴 수")]
        [SerializeField] private int _duration = 3;

        [Header("스택")]
        [SerializeField] private bool _canStack = false;
        [SerializeField] private int _maxStack = 1;

        [Header("UI")]
        [SerializeField] private Sprite _buffIcon;

        [Header("Presentation - Sound")]
        [Tooltip("버프 적용 순간 재생될 SFX")]
        [SerializeField] private AudioClip _applySfx;

        [Header("Presentation - VFX")]
        [Tooltip("버프 적용 순간 대상에 재생될 이펙트")]
        [SerializeField] private GameObject _applyVfxPrefab;
        [Tooltip("버프 지속 중 대상에 유지되는 이펙트 (선택)")]
        [SerializeField] private GameObject _loopVfxPrefab;

        // Properties
        public string BuffId => _buffId;
        public string BuffName => _buffName;
        public string Description => _description;
        public BuffType BuffType => _buffType;
        public float Multiplier => _multiplier;
        public int Duration => _duration;
        public bool CanStack => _canStack;
        public int MaxStack => _maxStack;
        public Sprite BuffIcon => _buffIcon;

        public AudioClip ApplySfx => _applySfx;
        public GameObject ApplyVfxPrefab => _applyVfxPrefab;
        public GameObject LoopVfxPrefab => _loopVfxPrefab;

        /// <summary>
        /// Multiplier > 1 이면 버프, 아니면 디버프
        /// </summary>
        public bool IsBuff => _multiplier > 1f;
    }
}