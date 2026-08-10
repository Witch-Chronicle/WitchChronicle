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
    [Tooltip("곡사 투사체가 위쪽으로 퍼질 수 있는 전체 각도")]
    [SerializeField, Range(1f, 180f)] private float _arcLaunchAngle = 120f;
    [Tooltip("곡사 경로 제어점 최소 거리")]
    [SerializeField, Min(0.01f)] private float _arcControlDistanceMin = 2f;
    [Tooltip("곡사 경로 제어점 최대 거리")]
    [SerializeField, Min(0.01f)] private float _arcControlDistanceMax = 4f;
    [Tooltip("곡사 투사체가 좌우로 퍼지는 최대 범위")]
    [SerializeField, Min(0f)] private float _arcSpread = 3f;
    [Tooltip("메테오 생성 위치의 대상 기준 높이")]
    [SerializeField, Min(0f)] private float _meteorHeight = 8f;

    [Header("Projectile VFX")]
    [Header("Projectile VFX")]
    [SerializeField] private GameObject _projectileVfxPrefab;
    [Tooltip("공격이 실제 대상에게 명중했을 때 생성할 VFX")]
    [SerializeField] private GameObject _hitVfxPrefab;
    [Tooltip("공격이 별자리 방어막에 막혔을 때 생성할 VFX")]
    [SerializeField] private GameObject _blockVfxPrefab;
    [Tooltip("Tick 공격이 대상에게 적용되는 동안 유지할 VFX")]
    [SerializeField] private GameObject _tickVfxPrefab;
    [Tooltip("공격자 위치 기준 투사체 생성 보정")]
    [SerializeField] private Vector3 _spawnOffset = Vector3.up;
    [Tooltip("대상 위치 기준 충돌 지점 보정")]
    [SerializeField] private Vector3 _targetOffset = Vector3.up;
    [Tooltip("투사체 크기 배율")]
    [SerializeField, Min(0.01f)] private float _projectileScale = 1f;

    [Header("Timed VFX")]
    [Tooltip("TimedVfx 방식에서 재생할 VFX")]
    [SerializeField] private GameObject _timedVfxPrefab;
    [Tooltip("TimedVfx 생성 기준 위치")]
    [SerializeField] private ConstellationPathTimedVfxSpawnType _timedVfxSpawnType;
    [Tooltip("VFX 생성 위치 보정")]
    [SerializeField] private Vector3 _timedVfxOffset;
    [Tooltip("AboveTarget 방식의 대상 기준 생성 높이")]
    [SerializeField, Min(0f)] private float _timedVfxHeight = 8f;
    [Tooltip("VFX 생성 후 실제 공격 판정까지의 시간")]
    [SerializeField, Min(0f)] private float _timedVfxImpactDelay = 0.3f;
    [Tooltip("VFX 전체 유지 시간")]
    [SerializeField, Min(0.01f)] private float _timedVfxDuration = 1f;
    [Tooltip("VFX 크기 배율")]
    [SerializeField, Min(0.01f)] private float _timedVfxScale = 1f;
    [Tooltip("생성 시 공격 대상을 바라보도록 회전")]
    [SerializeField] private bool _timedVfxFaceTarget;
    [Tooltip("VFX가 유지되는 동안 대상 위치를 따라감")]
    [SerializeField] private bool _timedVfxFollowTarget;

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
    public float ArcLaunchAngle => _arcLaunchAngle;
    public float ArcControlDistanceMin => _arcControlDistanceMin;
    public float ArcControlDistanceMax => _arcControlDistanceMax;
    public float ArcSpread => _arcSpread;
    public float MeteorHeight => _meteorHeight;
    public GameObject ProjectileVfxPrefab => _projectileVfxPrefab;
    public GameObject HitVfxPrefab => _hitVfxPrefab;
    public GameObject BlockVfxPrefab => _blockVfxPrefab;
    public GameObject TickVfxPrefab => _tickVfxPrefab;
    public GameObject TimedVfxPrefab => _timedVfxPrefab;
    public ConstellationPathTimedVfxSpawnType TimedVfxSpawnType => _timedVfxSpawnType;
    public Vector3 TimedVfxOffset => _timedVfxOffset;
    public float TimedVfxHeight => _timedVfxHeight;
    public float TimedVfxImpactDelay => _timedVfxImpactDelay;
    public float TimedVfxDuration => _timedVfxDuration;
    public float TimedVfxScale => _timedVfxScale;
    public bool TimedVfxFaceTarget => _timedVfxFaceTarget;
    public bool TimedVfxFollowTarget => _timedVfxFollowTarget;
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

        if (_arcControlDistanceMax < _arcControlDistanceMin)
        {
            errorMessage = "곡사 제어점 최대 거리는 최소 거리보다 크거나 같아야 함";
            return false;
        }

        if (_motionType == ConstellationPathProjectileMotionType.TimedVfx)
        {
            if (_timedVfxPrefab == null)
            {
                errorMessage = "TimedVfx 방식의 VFX Prefab이 없음";
                return false;
            }

            if (_timedVfxDuration <= 0f)
            {
                errorMessage = "TimedVfx 유지 시간은 0보다 커야 함";
                return false;
            }

            if (_timedVfxImpactDelay > _timedVfxDuration)
            {
                errorMessage = "TimedVfx ImpactDelay는 전체 유지 시간보다 길 수 없음";
                return false;
            }
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