using UnityEngine;

/// <summary>
/// 적 AI 성향 데이터
/// </summary>
[CreateAssetMenu(menuName = "Witch Chronicle/Enemy AI Profile")]
public class EnemyAIProfileData : ScriptableObject
{
    [Header("Profile Info")]
    [SerializeField] private string _profileId;
    [SerializeField] private string _profileName;
    [SerializeField] private EnemyAIArchetype _archetype;

    [Header("Target Preference")]
    [Range(0f, 5f)]
    [SerializeField] private float _lowHpTargetWeight = 1f;

    [Range(0f, 5f)]
    [SerializeField] private float _highThreatTargetWeight = 1f;

    [Range(0f, 5f)]
    [SerializeField] private float _randomTargetWeight = 0.5f;

    [Header("Action Preference")]
    [Range(0f, 5f)]
    [SerializeField] private float _basicAttackWeight = 1f;
    [Range(0f, 5f)]
    [SerializeField] private float _damageWeight = 1f;
    [Range(0f, 5f)]
    [SerializeField] private float _killWeight = 2f;
    [Range(0f, 5f)]
    [SerializeField] private float _weaknessWeight = 1.5f;
    [Range(0f, 5f)]
    [SerializeField] private float _healWeight = 1f;
    [Range(0f, 5f)]
    [SerializeField] private float _buffWeight = 1f;
    [Range(0f, 5f)]
    [SerializeField] private float _debuffWeight = 1f;
    [Range(0f, 5f)]
    [SerializeField] private float _statusEffectWeight = 1f;

    [Header("Survival")]
    [Range(0f, 1f)]
    [SerializeField] private float _selfDefenseHpRatio = 0.35f;

    [Range(0f, 5f)]
    [SerializeField] private float _selfSurvivalWeight = 1f;

    [Header("Randomness")]
    [Range(0f, 5f)]
    [SerializeField] private float _randomActionWeight = 0.5f;

    public string ProfileId => _profileId;
    public string ProfileName => _profileName;
    public EnemyAIArchetype Archetype => _archetype;

    public float LowHpTargetWeight => _lowHpTargetWeight;
    public float HighThreatTargetWeight => _highThreatTargetWeight;
    public float RandomTargetWeight => _randomTargetWeight;

    public float BasicAttackWeight => _basicAttackWeight;
    public float DamageWeight => _damageWeight;
    public float KillWeight => _killWeight;
    public float WeaknessWeight => _weaknessWeight;
    public float HealWeight => _healWeight;
    public float BuffWeight => _buffWeight;
    public float DebuffWeight => _debuffWeight;
    public float StatusEffectWeight => _statusEffectWeight;

    public float SelfDefenseHpRatio => _selfDefenseHpRatio;
    public float SelfSurvivalWeight => _selfSurvivalWeight;

    public float RandomActionWeight => _randomActionWeight;
}