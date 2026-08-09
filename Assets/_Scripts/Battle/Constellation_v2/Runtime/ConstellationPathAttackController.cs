using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 공격 전체 시퀀스 관리
/// 카메라, 시간 제어, 입력, 방어막, 투사체 흐름 연결
/// </summary>
public class ConstellationPathAttackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ConstellationPathBattleManager _pathBattleManager;
    [SerializeField] private ConstellationPathTimeController _timeController;
    [SerializeField] private BattleCameraDirector _cameraDirector;
    [SerializeField] private ConstellationPathVfxPlayer _vfxPlayer;

    private readonly ConstellationPathShieldState _shieldState = new ConstellationPathShieldState();

    private Coroutine _attackRoutine;
    private bool _hasResult;
    private ConstellationPathResult _lastResult;

    private readonly List<ConstellationPathHitEntry> _hitSchedule = new List<ConstellationPathHitEntry>();

    public bool IsRunning => _attackRoutine != null;
    public bool HasResult => _hasResult;
    public ConstellationPathResult LastResult => _lastResult;
    public ConstellationPathShieldState ShieldState => _shieldState;

    public IReadOnlyList<ConstellationPathHitEntry> HitSchedule => _hitSchedule;

    /// <summary>
    /// 내부 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_pathBattleManager == null)
        {
            _pathBattleManager = GetComponent<ConstellationPathBattleManager>();
        }

        if (_timeController == null)
        {
            _timeController = GetComponent<ConstellationPathTimeController>();
        }

        if (_cameraDirector == null)
        {
            _cameraDirector = FindFirstObjectByType<BattleCameraDirector>();
        }

        if (_vfxPlayer == null)
        {
            _vfxPlayer = GetComponent<ConstellationPathVfxPlayer>();
        }
    }

    /// <summary>
    /// 비활성화 시 별자리 공격 중단
    /// </summary>
    private void OnDisable()
    {
        StopAttack();
    }

    /// <summary>
    /// 시간 정지와 별자리 입력 단계 시작
    /// </summary>
    /// <param name="sequenceData">별자리 경로 데이터</param>
    /// <param name="onComplete">입력 완료 콜백</param>
    /// <returns>시작 성공 여부</returns>
    public bool StartInputPhase(
        ConstellationPathSequenceData sequenceData,
        Action<ConstellationPathResult> onComplete = null)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[ConstellationPath] 이미 별자리 공격 실행 중", this);
            return false;
        }

        if (!ValidateInputPhase(sequenceData))
        {
            return false;
        }

        _hasResult = false;
        _lastResult = default;
        _attackRoutine = StartCoroutine(InputPhaseRoutine(sequenceData, onComplete));

        return true;
    }

    /// <summary>
    /// 별자리 공격 연출 단계 시작
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">대표 공격 대상</param>
    /// <param name="attackTargets">실제 공격 대상 목록</param>
    /// <param name="shieldTargets">방어막 적용 대상 목록</param>
    /// <param name="skill">사용 스킬</param>
    /// <param name="onComplete">공격 단계 완료 콜백</param>
    /// <param name="onAttackStarted">실제 공격 모션 시작 콜백</param>
    /// <param name="onResumeStarted">시간 복귀 시작 콜백</param>
    /// <param name="onBeforeResume">시간 복귀 전 결과 확정 콜백</param>
    /// <returns>시작 성공 여부</returns>
    public bool StartAttackPhase(
        BattleUnit attacker,
        BattleUnit target,
        IReadOnlyList<BattleUnit> attackTargets,
        IReadOnlyList<BattleUnit> shieldTargets,
        SkillData skill,
        Action<ConstellationPathResult> onComplete = null,
        Action onAttackStarted = null,
        Action onResumeStarted = null,
        Action<ConstellationPathResult> onBeforeResume = null)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[ConstellationPath] 이미 별자리 공격 실행 중", this);
            return false;
        }

        if (!ValidateAttackPhase(attacker, skill))
        {
            return false;
        }

        _hasResult = false;
        _lastResult = default;

        _attackRoutine = StartCoroutine(
            AttackPhaseRoutine(
                attacker,
                target,
                attackTargets,
                shieldTargets,
                skill,
                onComplete,
                onAttackStarted,
                onResumeStarted,
                onBeforeResume));

        return true;
    }

    /// <summary>
    /// 감속, 입력, 시간 복귀 순차 진행
    /// </summary>
    /// <param name="sequenceData">별자리 경로 데이터</param>
    /// <param name="onComplete">입력 완료 콜백</param>
    private IEnumerator InputPhaseRoutine(
        ConstellationPathSequenceData sequenceData,
        Action<ConstellationPathResult> onComplete,
        Action<ConstellationPathResult> onBeforeResume = null,
        Action onResumeStarted = null)
    {
        bool isSlowDownCompleted = false;
        _timeController.SlowDown(() => isSlowDownCompleted = true);

        while (!isSlowDownCompleted)
        {
            yield return null;
        }

        bool isStarted = _pathBattleManager.StartConstellationPath(sequenceData);

        if (!isStarted)
        {
            Debug.LogWarning("[ConstellationPath] 별자리 입력 시작 실패", this);

            yield return ResumeTimeRoutine();

            _attackRoutine = null;
            yield break;
        }

        while (_pathBattleManager.IsRunning)
        {
            yield return null;
        }

        if (!_pathBattleManager.TryGetLastResult(out ConstellationPathResult result))
        {
            Debug.LogWarning("[ConstellationPath] 별자리 입력 결과 없음", this);

            yield return ResumeTimeRoutine();

            _attackRoutine = null;
            yield break;
        }

        _lastResult = result;
        _hasResult = true;

        // 결과 확정 후 방어막 및 공격 스케줄 준비
        onBeforeResume?.Invoke(result);

        // 실제 공격 재개 허용
        onResumeStarted?.Invoke();

        // 초저속 → 기존 속도 복귀
        yield return ResumeTimeRoutine();

        _attackRoutine = null;
        onComplete?.Invoke(result);
    }

    /// <summary>
    /// 카메라 전환 및 별자리 공격 사전 단계 진행
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">대표 공격 대상</param>
    /// <param name="skill">사용 스킬</param>
    /// <param name="onComplete">별자리 입력 완료 콜백</param>
    private IEnumerator AttackPhaseRoutine(
        BattleUnit attacker,
        BattleUnit target,
        IReadOnlyList<BattleUnit> attackTargets,
        IReadOnlyList<BattleUnit> shieldTargets,
        SkillData skill,
        Action<ConstellationPathResult> onComplete,
        Action onAttackStarted,
        Action onResumeStarted,
        Action<ConstellationPathResult> onBeforeResume)
    {
        bool isCameraCompleted = false;

        if (_cameraDirector != null)
        {
            _cameraDirector.PlayConstellationAttackIntro(attacker, target, () => isCameraCompleted = true);
        }
        else
        {
            isCameraCompleted = true;
        }

        while (!isCameraCompleted)
        {
            yield return null;
        }

        ConstellationPathAttackData attackData = skill.ConstellationPathAttackData;

        if (attackData.SlowDownStartDelay > 0f)
        {
            yield return new WaitForSeconds(attackData.SlowDownStartDelay);
        }

        // 실제 공격 모션 시작
        onAttackStarted?.Invoke();

        // 같은 프레임부터 감속 → 별자리 입력
        yield return InputPhaseRoutine(
            skill.ConstellationPathSequenceData,
            onComplete,
            onBeforeResume: result =>
            {
                InitializeShield(
                    shieldTargets,
                    result);

                BuildHitSchedule(
                    attackTargets,
                    skill.ConstellationPathAttackData,
                    result);

                onBeforeResume?.Invoke(result);
            },
            onResumeStarted: onResumeStarted);
    }

    /// <summary>
    /// 기존 시간 속도 복귀 대기
    /// </summary>
    private IEnumerator ResumeTimeRoutine()
    {
        bool isResumeCompleted = false;
        _timeController.Resume(() => isResumeCompleted = true);

        while (!isResumeCompleted)
        {
            yield return null;
        }
    }

    /// <summary>
    /// 입력 단계 실행 조건 검사
    /// </summary>
    /// <param name="sequenceData">별자리 경로 데이터</param>
    /// <returns>실행 가능 여부</returns>
    private bool ValidateInputPhase(ConstellationPathSequenceData sequenceData)
    {
        if (_pathBattleManager == null || !_pathBattleManager.isActiveAndEnabled)
        {
            Debug.LogWarning("[ConstellationPath] PathBattleManager 참조 없음", this);
            return false;
        }

        if (_timeController == null || !_timeController.isActiveAndEnabled)
        {
            Debug.LogWarning("[ConstellationPath] TimeController 참조 없음", this);
            return false;
        }

        if (sequenceData == null)
        {
            Debug.LogWarning("[ConstellationPath] SequenceData 없음", this);
            return false;
        }

        if (!sequenceData.TryValidate(out string errorMessage))
        {
            Debug.LogWarning($"[ConstellationPath] SequenceData 오류: {errorMessage}", sequenceData);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 별자리 공격 단계 실행 조건 검사
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="skill">사용 스킬</param>
    /// <returns>실행 가능 여부</returns>
    private bool ValidateAttackPhase(BattleUnit attacker, SkillData skill)
    {
        if (attacker == null)
        {
            Debug.LogWarning("[ConstellationPath] 공격 유닛 없음", this);
            return false;
        }

        if (skill == null)
        {
            Debug.LogWarning("[ConstellationPath] SkillData 없음", this);
            return false;
        }

        if (!ValidateInputPhase(skill.ConstellationPathSequenceData))
        {
            return false;
        }

        if (skill.ConstellationPathAttackData == null)
        {
            Debug.LogWarning("[ConstellationPath] AttackData 없음", skill);
            return false;
        }

        if (!skill.ConstellationPathAttackData.TryValidate(out string errorMessage))
        {
            Debug.LogWarning($"[ConstellationPath] AttackData 오류: {errorMessage}", skill.ConstellationPathAttackData);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 별자리 공격 강제 중단
    /// </summary>
    public void StopAttack()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        _pathBattleManager?.StopConstellationPath();
        _vfxPlayer?.StopAllVfx();
        _timeController?.RestoreImmediate();

        _shieldState.Clear();
        _hitSchedule.Clear();
        _hasResult = false;
        _lastResult = default;
    }

    /// <summary>
    /// 별자리 강공격 정상 종료 정리
    /// </summary>
    public void CompleteAttack()
    {
        _shieldState.Clear();
        _hitSchedule.Clear();
        _vfxPlayer?.ClearTargetAnchors();
    }

    /// <summary>
    /// 별자리 결과 기준 대상별 방어막 생성
    /// </summary>
    /// <param name="targets">방어막 적용 대상</param>
    /// <param name="result">별자리 입력 결과</param>
    public void InitializeShield(IReadOnlyList<BattleUnit> targets, ConstellationPathResult result)
    {
        _shieldState.Clear();

        if (targets == null || targets.Count == 0)
        {
            return;
        }

        if (result.CompletedNodeCount <= 0)
        {
            return;
        }

        _shieldState.Initialize(targets, result.CompletedNodeCount);
    }

    /// <summary>
    /// 별자리 방어막 상태 종료
    /// </summary>
    public void ClearShield()
    {
        _shieldState.Clear();
    }

    /// <summary>
    /// 별자리 공격 대상 및 공격 횟수 기준 타격 스케줄 생성
    /// </summary>
    /// <param name="targets">공격 대상</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <param name="result">별자리 입력 결과</param>
    private void BuildHitSchedule(
        IReadOnlyList<BattleUnit> targets,
        ConstellationPathAttackData attackData,
        ConstellationPathResult result)
    {
        _hitSchedule.Clear();

        if (targets == null || targets.Count == 0 || attackData == null)
        {
            return;
        }

        ConstellationPathHitSchedule.Build(
            targets,
            result.TotalNodeCount,
            attackData.AttackPattern,
            _hitSchedule);
    }

    /// <summary>
    /// 단일 공격 단위 방어 판정
    /// </summary>
    /// <param name="hitEntry">공격 단위</param>
    /// <param name="isBlocked">방어 성공 여부</param>
    /// <param name="remainingCharge">판정 후 남은 방어 횟수</param>
    /// <returns>판정 실행 여부</returns>
    public bool TryResolveHit(
        ConstellationPathHitEntry hitEntry,
        out bool isBlocked,
        out int remainingCharge)
    {
        isBlocked = false;
        remainingCharge = 0;

        if (hitEntry.Target == null || !hitEntry.Target.IsAlive)
        {
            return false;
        }

        isBlocked = _shieldState.TryBlock(hitEntry.Target, out remainingCharge);
        return true;
    }

    /// <summary>
    /// 단일 공격 단위 방어 및 데미지 전달 방식 판정
    /// </summary>
    /// <param name="hitEntry">공격 단위</param>
    /// <param name="unitDamage">공격 단위 총 데미지</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <param name="resolution">판정 결과</param>
    /// <returns>판정 실행 여부</returns>
    public bool ResolveAttackUnit(
        ConstellationPathHitEntry hitEntry,
        int unitDamage,
        ConstellationPathAttackData attackData,
        out ConstellationPathHitResolution resolution)
    {
        resolution = default;

        if (attackData == null || unitDamage <= 0)
        {
            return false;
        }

        if (!TryResolveHit(hitEntry, out bool isBlocked, out int remainingCharge))
        {
            return false;
        }

        List<ConstellationPathDamageSlice> damageSlices = new List<ConstellationPathDamageSlice>();

        if (!isBlocked)
        {
            damageSlices = ConstellationPathDamageDistributor.Build(unitDamage, attackData);
        }

        resolution = new ConstellationPathHitResolution(
            hitEntry.Target,
            isBlocked,
            remainingCharge,
            damageSlices);

        return true;
    }

    /// <summary>
    /// 별자리 공격 단위 VFX 재생
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">공격 연출 데이터</param>
    /// <param name="onImpact">VFX 충돌 콜백</param>
    /// <param name="onComplete">VFX 완료 콜백</param>
    /// <returns>VFX 재생 여부</returns>
    public bool PlayAttackUnitVfx(
        BattleUnit attacker,
        BattleUnit target,
        ConstellationPathAttackData attackData,
        Action onImpact,
        Action onComplete = null)
    {
        if (_vfxPlayer == null || attacker == null || target == null || attackData == null)
        {
            onImpact?.Invoke();
            onComplete?.Invoke();
            return false;
        }

        switch (attackData.MotionType)
        {
            case ConstellationPathProjectileMotionType.Straight:
                _vfxPlayer.PlayStraightProjectile(
                    attacker,
                    target,
                    attackData,
                    onImpact,
                    onComplete);

                return true;

            case ConstellationPathProjectileMotionType.Arc:
                _vfxPlayer.PlayArcProjectile(
                    attacker,
                    target,
                    attackData,
                    onImpact,
                    onComplete);

                return true;

            case ConstellationPathProjectileMotionType.Meteor:
                _vfxPlayer.PlayMeteorProjectile(
                    attacker,
                    target,
                    attackData,
                    onImpact,
                    onComplete);

                return true;

            case ConstellationPathProjectileMotionType.TimedVfx:
                _vfxPlayer.PlayTimedVfx(
                    attacker,
                    target,
                    attackData,
                    onImpact,
                    onComplete);

                return true;
        }

        onImpact?.Invoke();
        onComplete?.Invoke();
        return false;
    }

    /// <summary>
    /// 별자리 공격 명중 VFX 재생
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    public void PlayHitVfx(
        BattleUnit target,
        ConstellationPathAttackData attackData)
    {
        _vfxPlayer?.PlayHitVfx(
            target,
            attackData);
    }

    /// <summary>
    /// 별자리 공격 방어 VFX 재생
    /// </summary>
    /// <param name="target">방어 대상</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    public void PlayBlockVfx(
        BattleUnit target,
        ConstellationPathAttackData attackData)
    {
        _vfxPlayer?.PlayBlockVfx(
            target,
            attackData);
    }

    /// <summary>
    /// 별자리 Tick 지속 VFX 시작
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <returns>생성된 Tick VFX</returns>
    public GameObject PlayTickVfx(
        BattleUnit target,
        ConstellationPathAttackData attackData)
    {
        if (_vfxPlayer == null) return null;

        return _vfxPlayer.PlayTickVfx(
            target,
            attackData);
    }

    /// <summary>
    /// 별자리 Tick 지속 VFX 종료
    /// </summary>
    /// <param name="tickVfx">종료 대상 VFX</param>
    public void StopTickVfx(GameObject tickVfx)
    {
        _vfxPlayer?.StopTickVfx(tickVfx);
    }
}