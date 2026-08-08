using UnityEngine;

/// <summary>
/// 별자리 공격 연출 데이터
/// 대상 분배, 투사체 이동, 발사 간격 정의
/// </summary>
[CreateAssetMenu(
    fileName = "ConstellationPathAttack",
    menuName = "WitchChronicle/Constellation Path/Attack Data")]
public class ConstellationPathAttackData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _attackId;

    [Header("Attack Pattern")]
    [Tooltip("광역 동시 타격, 균등 순차 타격, 단일 대상 연속 타격")]
    [SerializeField] private ConstellationPathAttackPattern _attackPattern;

    [Header("Time Direction")]
    [Tooltip("공격 사전 연출 시작 후 시간 감속이 시작되기까지의 시간")]
    [SerializeField, Min(0f)] private float _slowDownStartDelay = 0.3f;

    [Header("Projectile Motion")]
    [SerializeField] private ConstellationPathProjectileMotionType _motionType;
    [Tooltip("각 투사체 또는 공격 라운드 사이의 발사 간격")]
    [SerializeField, Min(0f)] private float _launchInterval = 0.12f;
    [Tooltip("투사체가 대상까지 이동하는 시간")]
    [SerializeField, Min(0.01f)] private float _travelDuration = 0.8f;
    [Tooltip("곡선 이동 시 중간 지점의 추가 높이")]
    [SerializeField, Min(0f)] private float _arcHeight = 2f;
    [Tooltip("메테오 생성 위치의 대상 기준 높이")]
    [SerializeField, Min(0f)] private float _meteorHeight = 8f;

    [Header("Projectile VFX")]
    [SerializeField] private GameObject _projectileVfxPrefab;
    [SerializeField] private GameObject _hitVfxPrefab;
    [Tooltip("공격자 위치 기준 투사체 생성 보정")]
    [SerializeField] private Vector3 _spawnOffset = Vector3.up;
    [Tooltip("대상 위치 기준 충돌 지점 보정")]
    [SerializeField] private Vector3 _targetOffset = Vector3.up;
    [Tooltip("투사체 크기 배율")]
    [SerializeField, Min(0.01f)] private float _projectileScale = 1f;

    [Header("Damage Delivery")]
    [Tooltip("공격 단위당 데미지 적용 방식")]
    [SerializeField] private ConstellationPathDamageDeliveryType _damageDeliveryType;
    [Tooltip("Tick 방식일 때 공격 단위 하나의 데미지 분할 횟수")]
    [SerializeField, Min(1)] private int _tickCount = 10;
    [Tooltip("각 데미지 틱 사이의 간격")]
    [SerializeField, Min(0f)] private float _tickInterval = 0.05f;

    public string AttackId => _attackId;
    public ConstellationPathAttackPattern AttackPattern => _attackPattern;
    public float SlowDownStartDelay => _slowDownStartDelay;
    public ConstellationPathProjectileMotionType MotionType => _motionType;
    public float LaunchInterval => _launchInterval;
    public float TravelDuration => _travelDuration;
    public float ArcHeight => _arcHeight;
    public float MeteorHeight => _meteorHeight;
    public GameObject ProjectileVfxPrefab => _projectileVfxPrefab;
    public GameObject HitVfxPrefab => _hitVfxPrefab;
    public Vector3 SpawnOffset => _spawnOffset;
    public Vector3 TargetOffset => _targetOffset;
    public float ProjectileScale => _projectileScale;
    public ConstellationPathDamageDeliveryType DamageDeliveryType => _damageDeliveryType;
    public int TickCount => Mathf.Max(1, _tickCount);
    public float TickInterval => _tickInterval;

    /// <summary>
    /// 별자리 공격 데이터 유효성 검사
    /// </summary>
    /// <param name="errorMessage">검사 실패 메시지</param>
    /// <returns>유효 여부</returns>
    public bool TryValidate(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(_attackId))
        {
            errorMessage = "AttackId가 비어 있음";
            return false;
        }

        if (_travelDuration <= 0f)
        {
            errorMessage = "투사체 이동 시간은 0보다 커야 함";
            return false;
        }

        if (_damageDeliveryType == ConstellationPathDamageDeliveryType.Tick && _tickCount <= 0)
        {
            errorMessage = "Tick 방식의 틱 횟수는 1 이상이어야 함";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// 인스펙터 데이터 검사
    /// </summary>
    [ContextMenu("Validate Attack Data")]
    private void ValidateAttackData()
    {
        if (TryValidate(out string errorMessage))
        {
            Debug.Log($"별자리 공격 데이터 검사 성공: {_attackId}", this);
            return;
        }

        Debug.LogWarning($"별자리 공격 데이터 검사 실패: {errorMessage}", this);
    }
}