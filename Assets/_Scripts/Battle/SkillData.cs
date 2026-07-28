using UnityEngine;
using Battle.Rules;

[CreateAssetMenu(menuName = "Witch Chronicle/Skill")]
public class SkillData : ScriptableObject
{
    [Header("Skill Info")]
    [SerializeField] private string _skillId;
    [SerializeField] private string _skillName;
    [SerializeField] private string _description;
    [Tooltip("스킬 등급 (0=기본 공격, 1=최상급, 2=중급, 3=하급)")]
    [SerializeField] private int _tier;

    [Header("Skill Settings")]
    [SerializeField] private int _mpCost;
    [SerializeField] private ElementType _elementType;
    [SerializeField] private SkillEffectType _skillType;
    [SerializeField] private DamageType _damageType;
    [SerializeField] private int _power;
    [SerializeField] private TargetType _targetType;

    [Header("Status Effect")]
    [SerializeField] private StatusEffectType _statusEffectType;
    [SerializeField] private float _statusChance;

    [Header("Buff/Debuff")]
    [Tooltip("Buff/Debuff 타입 스킬일 때 참조할 BuffData")]
    [SerializeField] private BuffData _buffData;

    [Header("Presentation - Common")]
    [Tooltip("연출 유형 (Projectile=투사체, Area=광역, SelfTarget=자기/힐형)")]
    [SerializeField] private SkillPresentationType _presentationType;
    [SerializeField] private Sprite _skillIcon;
    [SerializeField] private AudioClip _voiceClip;
    [Tooltip("연출 시작 후 데미지가 들어가기까지 지연(초). 이펙트가 대상에 닿는 시점에 맞춘다. 0이면 컨트롤러의 전역 Impact Delay 사용")]
    [SerializeField] private float _impactDelay = 0f;

    [Header("Presentation - Sound")]
    [SerializeField] private AudioClip _castSfx;
    [SerializeField] private AudioClip _hitSfx;

    [Header("Presentation - VFX")]
    [Tooltip("시전 이펙트 프리팹 (광역형은 필수, 투사체형은 선택)")]
    [SerializeField] private GameObject _castVfxPrefab;
    [Tooltip("투사체 이펙트 프리팹 (Projectile 유형에서만 사용)")]
    [SerializeField] private GameObject _projectileVfxPrefab;
    [Tooltip("명중/발동 이펙트 프리팹 (전 유형 필수)")]
    [SerializeField] private GameObject _hitVfxPrefab;

    [Header("Presentation - Constellation Attack")]
    [Tooltip("연결 시 사전 카메라 연출 후 별자리 패리 공격 실행")]
    [SerializeField]
    private ConstellationSequenceData _constellationSequenceData;

    [Header("Presentation - Draw Guide (마법진 그리기)")]
    [Tooltip("SkillDrawController가 사용할 궤적 가이드 JSON (fire_ball.json 등). " +
             "SkillShapeTemplate.ParsePoints로 파싱되는 포맷과 동일해야 함.")]
    [SerializeField] private TextAsset _drawGuideJson;
    [Tooltip("그리기 화면 Header에 표시할 예시 이미지 (LineRenderer 가이드라인 대신 이걸로 모양을 보여줌)")]
    [SerializeField] private Sprite _drawExampleSprite;

    public string SkillId => _skillId;
    public string SkillName => _skillName;
    public string Description => _description;
    public int Tier => _tier;

    public int MpCost => _mpCost;
    public ElementType ElementType => _elementType;
    public SkillEffectType SkillType => _skillType;
    public DamageType DamageType => _damageType;
    public int Power => _power;
    public TargetType TargetType => _targetType;

    public StatusEffectType StatusEffectType => _statusEffectType;
    public float StatusChance => _statusChance;

    public BuffData BuffData => _buffData;

    public SkillPresentationType PresentationType => _presentationType;
    public Sprite SkillIcon => _skillIcon;
    public AudioClip VoiceClip => _voiceClip;
    public float ImpactDelay => _impactDelay;

    public AudioClip CastSfx => _castSfx;
    public AudioClip HitSfx => _hitSfx;

    public GameObject CastVfxPrefab => _castVfxPrefab;
    public GameObject ProjectileVfxPrefab => _projectileVfxPrefab;
    public GameObject HitVfxPrefab => _hitVfxPrefab;

    public ConstellationSequenceData ConstellationSequenceData =>
    _constellationSequenceData;

    public bool IsConstellationAttack =>
        _constellationSequenceData != null;

    public TextAsset DrawGuideJson => _drawGuideJson;
    public Sprite DrawExampleSprite => _drawExampleSprite;
}