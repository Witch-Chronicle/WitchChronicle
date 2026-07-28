using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 전투 카메라 연출 제어
/// </summary>
public class BattleCameraDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private BattleUIContext _battleUIContext;

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera _playerBackCamera;
    [SerializeField] private CinemachineCamera _targetOverviewCamera;
    [SerializeField] private CinemachineCamera _skillLowAngleCamera;
    [SerializeField] private CinemachineCamera _skillDrawCamera;
    [SerializeField] private CinemachineCamera _singleTargetOverviewCamera;
    [SerializeField] private CinemachineCamera _groupTargetOverviewCamera;

    [Header("Priority")]
    [SerializeField] private int _activePriority = 30;
    [SerializeField] private int _inactivePriority = 0;

    [Header("Player Back View")]
    [SerializeField] private float _backDistance = 4.5f;
    [SerializeField] private float _backHeight = 2.0f;
    [SerializeField] private float _backLookHeight = 1.2f;
    [SerializeField] private float _backLookForward = 2.0f;
    [SerializeField] private float _backFov = 50f;
    [SerializeField] private float _backSideOffset = -1.2f;
    [SerializeField] private float _backRoll = 0f;
    [SerializeField] private float _backWaitDuration = 0.35f;
    [Tooltip("같은 카메라가 이미 활성화된 상태에서 다른 캐릭터로 넘어갈 때(A 턴 -> B 턴) 부드럽게 이동하는 시간")]
    [SerializeField] private float _backTweenDuration = 0.4f;

    [Header("Target Overview View")]
    [SerializeField] private float _overviewBackDistance = 5.0f;
    [SerializeField] private float _overviewHeight = 6.5f;
    [SerializeField] private float _overviewFocusHeight = 0.8f;
    [SerializeField] private float _overviewFov = 55f;
    [SerializeField] private float _overviewSideOffset = 0f;
    [SerializeField] private float _overviewFocusForward = 1.5f;
    [SerializeField] private float _overviewRoll = 0f;
    [SerializeField] private float _overviewWaitDuration = 0.35f;

    [Header("Skill Battlefield View")]
    [SerializeField] private float _skillBackDistance = 2.2f;
    [SerializeField] private float _skillSideOffset = 1.45f;
    [SerializeField] private float _skillHeight = 0.55f;
    [SerializeField] private float _skillFocusHeight = 1.05f;
    [Range(0f, 1f)] [SerializeField] private float _skillTargetFocusWeight = 0.72f;
    [SerializeField] private float _skillFov = 78f;
    [SerializeField] private float _skillRoll = 0f;
    [SerializeField] private float _skillWaitDuration = 0.4f;

    [Header("Skill Draw View (마법진 그리기)")]
    [SerializeField] private float _skillDrawDistance = 4.5f;
    [SerializeField] private float _skillDrawHeight = 2.0f;
    [SerializeField] private float _skillDrawLookHeight = 1.2f;
    [SerializeField] private float _skillDrawLookForward = 2.0f;
    [SerializeField] private float _skillDrawFov = 50f;
    [SerializeField] private float _skillDrawSideOffset = -1.2f;
    [SerializeField] private float _skillDrawRoll = 0f;
    [SerializeField] private float _skillDrawWaitDuration = 0.35f;

    [Header("Single Target Overview (기본공격 / 단일 적 스킬)")]
    [Tooltip("타겟의 forward 방향으로 이 거리만큼 이동한 지점에서 타겟을 바라봄 (타겟 정면 근접 샷)")]
    [SerializeField] private float _singleFrontDistance = 1.8f;
    [SerializeField] private float _singleSideOffset = 0f;
    [SerializeField] private float _singleHeight = 1.2f;
    [SerializeField] private float _singleLookHeight = 1.0f;
    [SerializeField] private float _singleFov = 60f;
    [SerializeField] private float _singleRoll = 0f;
    [SerializeField] private float _singleWaitDuration = 0.35f;

    [Header("Group Target Overview - 적 전체 (정면)")]
    [SerializeField] private float _groupEnemyFrontDistance = 5.0f;
    [SerializeField] private float _groupEnemySideOffset = 0f;
    [SerializeField] private float _groupEnemyHeight = 2.0f;
    [SerializeField] private float _groupEnemyFocusHeight = 1.0f;
    [SerializeField] private float _groupEnemyFov = 55f;
    [SerializeField] private float _groupEnemyRoll = 0f;

    [Header("Group Target Overview - 아군 전체 (뒷모습)")]
    [SerializeField] private float _groupAllyBackDistance = 6.0f;
    [SerializeField] private float _groupAllySideOffset = 0f;
    [SerializeField] private float _groupAllyHeight = 2.5f;
    [SerializeField] private float _groupAllyFocusHeight = 1.0f;
    [SerializeField] private float _groupAllyFov = 55f;
    [SerializeField] private float _groupAllyRoll = 0f;

    [SerializeField] private float _groupWaitDuration = 0.35f;

    private Coroutine _waitRoutine;
    private CinemachineCamera _activeCamera;

    /// <summary>
    /// 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_battleManager == null)
        {
            _battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (_battleUIContext == null)
        {
            _battleUIContext = FindFirstObjectByType<BattleUIContext>();
        }
    }

    /// <summary>
    /// 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (_battleUIContext == null)
        {
            return;
        }

        _battleUIContext.OnTurnStarted += HandleTurnStarted;
        _battleUIContext.OnBattleEnded += HandleBattleEnded;
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_battleUIContext == null)
        {
            return;
        }

        _battleUIContext.OnTurnStarted -= HandleTurnStarted;
        _battleUIContext.OnBattleEnded -= HandleBattleEnded;
    }

    /// <summary>
    /// 턴 시작 카메라 처리
    /// </summary>
    /// <param name="unit">턴 유닛</param>
    private void HandleTurnStarted(BattleUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        if (unit.TeamType != BattleTeamType.Player)
        {
            return;
        }

        PlayPlayerBackView(unit);
    }

    /// <summary>
    /// 전투 종료 카메라 처리
    /// </summary>
    /// <param name="winner">승리 팀</param>
    private void HandleBattleEnded(BattleTeamType winner)
    {
        StopWaitRoutine();
    }

    /// <summary>
    /// 플레이어 등 뒤 구도 재생.
    /// _playerBackCamera가 이미 활성 상태(예: A 턴 -> B 턴 전환)면 Priority 블렌드가 안 먹히므로
    /// DOTween으로 부드럽게 이동, 아니면 기존처럼 즉시 스냅 + Priority 블렌드.
    /// </summary>
    /// <param name="unit">기준 유닛</param>
    /// <param name="onComplete">완료 콜백</param>
    public void PlayPlayerBackView(BattleUnit unit, Action onComplete = null)
    {
        if (TryGetActorTransform(unit, out Transform actorTransform) == false)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 focusPosition =
            actorTransform.position +
            actorTransform.forward * _backLookForward +
            Vector3.up * _backLookHeight;

        Vector3 cameraPosition =
            actorTransform.position -
            actorTransform.forward * _backDistance +
            actorTransform.right * _backSideOffset +
            Vector3.up * _backHeight;

        ApplyOrTweenCameraPose(
            _playerBackCamera,
            cameraPosition,
            focusPosition,
            _backFov,
            _backRoll,
            _backWaitDuration,
            _backTweenDuration,
            onComplete);
    }

    /// <summary>
    /// 대상 선택 부감 구도 재생
    /// </summary>
    /// <param name="unit">기준 유닛</param>
    /// <param name="onComplete">완료 콜백</param>
    public void PlayTargetOverview(BattleUnit unit, Action onComplete = null)
    {
        if (TryGetActorTransform(unit, out Transform actorTransform) == false)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 battleCenter = GetBattleCenter(actorTransform.position);

        Vector3 focusPosition =
            battleCenter +
            actorTransform.forward * _overviewFocusForward +
            Vector3.up * _overviewFocusHeight;

        Vector3 cameraPosition =
            battleCenter -
            actorTransform.forward * _overviewBackDistance +
            actorTransform.right * _overviewSideOffset +
            Vector3.up * _overviewHeight;

        ApplyCameraPose(
            _targetOverviewCamera,
            cameraPosition,
            focusPosition,
            _overviewFov,
            _overviewRoll);

        ActivateCamera(
            _targetOverviewCamera,
            _overviewWaitDuration,
            onComplete);
    }

    /// <summary>
    /// 스킬 선택 전장 로우앵글 구도 재생
    /// </summary>
    /// <param name="unit">기준 유닛</param>
    /// <param name="onComplete">완료 콜백</param>
    public void PlaySkillLowAngle(BattleUnit unit, Action onComplete = null)
    {
        if (TryGetActorTransform(unit, out Transform actorTransform) == false)
        {
            onComplete?.Invoke();
            return;
        }

        BattleTeamType opponentTeam = GetOpposingTeam(unit.TeamType);

        Vector3 fallbackTargetPosition =
            actorTransform.position +
            actorTransform.forward * 6f;

        Vector3 targetCenter =
            GetTeamCenter(
                opponentTeam,
                fallbackTargetPosition);

        CalculateBattleRelativePose(
            actorTransform,
            targetCenter,
            _skillBackDistance,
            _skillSideOffset,
            _skillHeight,
            _skillTargetFocusWeight,
            _skillFocusHeight,
            out Vector3 cameraPosition,
            out Vector3 focusPosition);

        ApplyCameraPose(
            _skillLowAngleCamera,
            cameraPosition,
            focusPosition,
            _skillFov,
            _skillRoll);

        ActivateCamera(
            _skillLowAngleCamera,
            _skillWaitDuration,
            onComplete);
    }

    /// <summary>
    /// 마법진 그리기(SkillDrawController) 구도 재생.
    /// PlayerBackView와 동일한 구조(등 뒤에서 캐릭터를 보는 구도) - 필요에 맞게 파라미터만 별도로 조정 가능.
    /// </summary>
    /// <param name="unit">기준 유닛</param>
    /// <param name="onComplete">완료 콜백</param>
    public void PlaySkillDrawView(BattleUnit unit, Action onComplete = null)
    {
        if (TryGetActorTransform(unit, out Transform actorTransform) == false)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 focusPosition =
            actorTransform.position +
            actorTransform.forward * _skillDrawLookForward +
            Vector3.up * _skillDrawLookHeight;

        Vector3 cameraPosition =
            actorTransform.position -
            actorTransform.forward * _skillDrawDistance +
            actorTransform.right * _skillDrawSideOffset +
            Vector3.up * _skillDrawHeight;

        ApplyCameraPose(
            _skillDrawCamera,
            cameraPosition,
            focusPosition,
            _skillDrawFov,
            _skillDrawRoll);

        ActivateCamera(
            _skillDrawCamera,
            _skillDrawWaitDuration,
            onComplete);
    }

    /// <summary>
    /// 단일 대상(기본 공격 / SingleEnemy 스킬) 정면 근접 구도 재생.
    /// 타겟의 forward 방향으로 이동한 지점에서 타겟을 바라봐서, 타겟의 정면이 보이게 함.
    /// </summary>
    /// <param name="target">바라볼 대상 유닛</param>
    /// <param name="onComplete">완료 콜백</param>
    public void PlaySingleTargetOverview(BattleUnit target, Action onComplete = null)
    {
        if (TryGetActorTransform(target, out Transform targetTransform) == false)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 focusPosition =
            targetTransform.position +
            Vector3.up * _singleLookHeight;

        Vector3 cameraPosition =
            targetTransform.position +
            targetTransform.forward * _singleFrontDistance +
            targetTransform.right * _singleSideOffset +
            Vector3.up * _singleHeight;

        ApplyCameraPose(
            _singleTargetOverviewCamera,
            cameraPosition,
            focusPosition,
            _singleFov,
            _singleRoll);

        ActivateCamera(
            _singleTargetOverviewCamera,
            _singleWaitDuration,
            onComplete);
    }

    /// <summary>
    /// 그룹 대상(AllEnemies / SingleAlly / AllAllies 스킬) 구도 재생.
    /// targetTeam이 Enemy면 적 전체 중심 앞쪽에서 적들의 정면이 보이는 구도,
    /// targetTeam이 Player면 아군 전체 중심 뒤쪽에서 아군들의 뒷모습이 보이는 구도(PlayerBackView와 유사).
    /// </summary>
    /// <param name="actor">기준(행동 주체) 유닛 - forward 축 계산용</param>
    /// <param name="targetTeam">바라볼 대상 팀</param>
    /// <param name="onComplete">완료 콜백</param>
    public void PlayGroupTargetOverview(BattleUnit actor, BattleTeamType targetTeam, Action onComplete = null)
    {
        if (TryGetActorTransform(actor, out Transform actorTransform) == false)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 groupCenter = GetTeamCenter(targetTeam, actorTransform.position);

        Vector3 cameraPosition;
        Vector3 focusPosition;
        float fov;
        float roll;

        if (targetTeam == BattleTeamType.Enemy)
        {
            // 적 전체 정면 구도: 파티 쪽(actor 기준 -forward)에서 적 그룹 중심을 바라봄.
            // 적들은 파티를 향해 서있다는 전제이므로, 파티 쪽에서 보면 적들의 정면이 보임.
            cameraPosition =
                groupCenter -
                actorTransform.forward * _groupEnemyFrontDistance +
                actorTransform.right * _groupEnemySideOffset +
                Vector3.up * _groupEnemyHeight;

            focusPosition =
                groupCenter +
                Vector3.up * _groupEnemyFocusHeight;

            fov = _groupEnemyFov;
            roll = _groupEnemyRoll;
        }
        else
        {
            // 아군 전체 뒷모습 구도: 아군 그룹 중심 기준 뒤쪽(actor 기준 -forward)에서
            // 그룹 중심 자체를 정면으로 포커싱 (죽었든 살았든 상관없이 중심 위치 기준).
            cameraPosition =
                groupCenter -
                actorTransform.forward * _groupAllyBackDistance +
                actorTransform.right * _groupAllySideOffset +
                Vector3.up * _groupAllyHeight;

            focusPosition =
                groupCenter +
                Vector3.up * _groupAllyFocusHeight;

            fov = _groupAllyFov;
            roll = _groupAllyRoll;
        }

        ApplyCameraPose(
            _groupTargetOverviewCamera,
            cameraPosition,
            focusPosition,
            fov,
            roll);

        ActivateCamera(
            _groupTargetOverviewCamera,
            _groupWaitDuration,
            onComplete);
    }

    /// <summary>
    /// Q/E로 타겟이 바뀔 때 SingleTargetOverview 카메라가 이미 활성화된 상태에서 새 타겟으로
    /// 부드럽게 재조준. 같은 카메라라 Priority 전환(블렌드)이 안 먹히므로 DOTween으로 직접 이동.
    /// </summary>
    /// <param name="newTarget">새로 비출 대상 유닛</param>
    /// <param name="duration">이동 시간</param>
    public void RetargetSingleTargetOverview(BattleUnit newTarget, float duration = 0.25f)
    {
        if (_singleTargetOverviewCamera == null) return;

        if (TryGetActorTransform(newTarget, out Transform targetTransform) == false) return;

        Vector3 focusPosition =
            targetTransform.position +
            Vector3.up * _singleLookHeight;

        Vector3 cameraPosition =
            targetTransform.position +
            targetTransform.forward * _singleFrontDistance +
            targetTransform.right * _singleSideOffset +
            Vector3.up * _singleHeight;

        Quaternion cameraRotation = GetLookRotation(cameraPosition, focusPosition);
        cameraRotation *= Quaternion.Euler(0f, 0f, _singleRoll);

        Transform camTransform = _singleTargetOverviewCamera.transform;

        camTransform.DOKill();
        camTransform.DOMove(cameraPosition, duration).SetEase(Ease.OutQuad);
        camTransform.DORotateQuaternion(cameraRotation, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 같은 카메라가 이미 활성 상태인지에 따라 분기: 이미 활성 상태면(Priority 블렌드가 안 먹히므로)
    /// DOTween으로 부드럽게 재조준, 아니면 기존처럼 즉시 스냅 + Priority 블렌드로 카메라 전환.
    /// </summary>
    private void ApplyOrTweenCameraPose(
        CinemachineCamera camera,
        Vector3 cameraPosition,
        Vector3 focusPosition,
        float fov,
        float roll,
        float waitDuration,
        float tweenDuration,
        Action onComplete)
    {
        if (camera == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (_activeCamera == camera)
        {
            Quaternion cameraRotation = GetLookRotation(cameraPosition, focusPosition);
            cameraRotation *= Quaternion.Euler(0f, 0f, roll);

            Transform camTransform = camera.transform;

            camTransform.DOKill();
            camTransform.DOMove(cameraPosition, tweenDuration).SetEase(Ease.OutQuad);
            camTransform.DORotateQuaternion(cameraRotation, tweenDuration).SetEase(Ease.OutQuad);

            LensSettings lens = camera.Lens;
            float startFov = lens.FieldOfView;
            DOTween.To(() => startFov, x =>
            {
                LensSettings currentLens = camera.Lens;
                currentLens.FieldOfView = x;
                camera.Lens = currentLens;
            }, fov, tweenDuration);

            StopWaitRoutine();

            if (onComplete != null)
            {
                _waitRoutine = StartCoroutine(WaitAndInvoke(tweenDuration, onComplete));
            }

            return;
        }

        ApplyCameraPose(camera, cameraPosition, focusPosition, fov, roll);
        ActivateCamera(camera, waitDuration, onComplete);
    }

    /// <summary>
    /// Cinemachine Camera 위치, 회전, 렌즈 적용
    /// </summary>
    /// <param name="cinemachineCamera">대상 Cinemachine Camera</param>
    /// <param name="cameraPosition">카메라 위치</param>
    /// <param name="focusPosition">주시 위치</param>
    /// <param name="fov">시야각</param>
    private void ApplyCameraPose(
        CinemachineCamera cinemachineCamera,
        Vector3 cameraPosition,
        Vector3 focusPosition,
        float fov,
        float roll)
    {
        if (cinemachineCamera == null)
        {
            return;
        }

        cinemachineCamera.transform.DOKill();

        Quaternion cameraRotation = GetLookRotation(cameraPosition, focusPosition);
        cameraRotation *= Quaternion.Euler(0f, 0f, roll);

        cinemachineCamera.transform.SetPositionAndRotation(
            cameraPosition,
            cameraRotation);

        LensSettings lens = cinemachineCamera.Lens;
        lens.FieldOfView = fov;
        cinemachineCamera.Lens = lens;
    }

    /// <summary>
    /// 카메라 활성화
    /// </summary>
    /// <param name="targetCamera">활성화할 카메라</param>
    /// <param name="waitDuration">완료 대기 시간</param>
    /// <param name="onComplete">완료 콜백</param>
    private void ActivateCamera(
        CinemachineCamera targetCamera,
        float waitDuration,
        Action onComplete)
    {
        SetCameraPriority(_playerBackCamera, _inactivePriority);
        SetCameraPriority(_targetOverviewCamera, _inactivePriority);
        SetCameraPriority(_skillLowAngleCamera, _inactivePriority);
        SetCameraPriority(_skillDrawCamera, _inactivePriority);
        SetCameraPriority(_singleTargetOverviewCamera, _inactivePriority);
        SetCameraPriority(_groupTargetOverviewCamera, _inactivePriority);

        SetCameraPriority(targetCamera, _activePriority);

        _activeCamera = targetCamera;

        StopWaitRoutine();

        if (onComplete == null)
        {
            return;
        }

        _waitRoutine = StartCoroutine(WaitAndInvoke(waitDuration, onComplete));
    }

    /// <summary>
    /// Priority 설정
    /// </summary>
    /// <param name="cinemachineCamera">대상 카메라</param>
    /// <param name="priorityValue">Priority 값</param>
    private void SetCameraPriority(CinemachineCamera cinemachineCamera, int priorityValue)
    {
        if (cinemachineCamera == null)
        {
            return;
        }

        PrioritySettings priority = cinemachineCamera.Priority;
        priority.Enabled = true;
        priority.Value = priorityValue;
        cinemachineCamera.Priority = priority;
    }

    /// <summary>
    /// 완료 콜백 지연 호출
    /// </summary>
    /// <param name="duration">대기 시간</param>
    /// <param name="onComplete">완료 콜백</param>
    private IEnumerator WaitAndInvoke(float duration, Action onComplete)
    {
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        _waitRoutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 대기 루틴 중단
    /// </summary>
    private void StopWaitRoutine()
    {
        if (_waitRoutine == null)
        {
            return;
        }

        StopCoroutine(_waitRoutine);
        _waitRoutine = null;
    }

    /// <summary>
    /// BattleUnit 기준 Actor Transform 검색
    /// </summary>
    /// <param name="unit">검색 유닛</param>
    /// <param name="actorTransform">검색된 Transform</param>
    /// <returns>검색 성공 여부</returns>
    private bool TryGetActorTransform(BattleUnit unit, out Transform actorTransform)
    {
        actorTransform = null;

        if (unit == null || _battleManager == null)
        {
            return false;
        }

        if (_battleManager.TryGetActor(unit, out BattleActor actor) == false)
        {
            return false;
        }

        if (actor == null)
        {
            return false;
        }

        actorTransform = actor.transform;
        return true;
    }

    /// <summary>
    /// 전장 중심 계산
    /// </summary>
    /// <param name="fallbackPosition">대체 위치</param>
    /// <returns>전장 중심</returns>
    private Vector3 GetBattleCenter(Vector3 fallbackPosition)
    {
        if (_battleManager == null || _battleManager.SpawnedActors == null)
        {
            return fallbackPosition;
        }

        Vector3 total = Vector3.zero;
        int count = 0;

        for (int i = 0; i < _battleManager.SpawnedActors.Count; i++)
        {
            BattleActor actor = _battleManager.SpawnedActors[i];

            if (actor == null)
            {
                continue;
            }

            total += actor.transform.position;
            count++;
        }

        if (count == 0)
        {
            return fallbackPosition;
        }

        return total / count;
    }

    /// <summary>
    /// 특정 팀 소속 유닛들의 중심 위치 계산 (GroupTargetOverview용)
    /// </summary>
    /// <param name="team">대상 팀</param>
    /// <param name="fallbackPosition">대체 위치</param>
    /// <returns>팀 중심 위치</returns>
    private Vector3 GetTeamCenter(BattleTeamType team, Vector3 fallbackPosition)
    {
        if (_battleManager == null || _battleManager.SpawnedActors == null)
        {
            return fallbackPosition;
        }

        Vector3 total = Vector3.zero;
        int count = 0;

        for (int i = 0; i < _battleManager.SpawnedActors.Count; i++)
        {
            BattleActor actor = _battleManager.SpawnedActors[i];

            if (actor == null || actor.TeamType != team)
            {
                continue;
            }

            total += actor.transform.position;
            count++;
        }

        if (count == 0)
        {
            return fallbackPosition;
        }

        return total / count;
    }

    /// <summary>
    /// 주시 회전 계산
    /// </summary>
    /// <param name="cameraPosition">카메라 위치</param>
    /// <param name="focusPosition">주시 위치</param>
    /// <returns>계산 회전</returns>
    private Quaternion GetLookRotation(Vector3 cameraPosition, Vector3 focusPosition)
    {
        Vector3 direction = focusPosition - cameraPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    /// <summary>
    /// 별자리 공격 사전 카메라 연출
    /// 공격자 강조 후 전투 부감 구도 전환.
    /// - attacker가 아군이면: SkillLowAngle(근접 로우앵글) -> TargetOverview 순서로 연출.
    /// - attacker가 적이면: SkillLowAngle은 원래 플레이어 스킬 선택용 연출이라 적에게 붙으면 어색하므로
    ///   건너뛰고 바로 TargetOverview로만 전환.
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="onComplete">연출 완료 콜백</param>
    public void PlayConstellationAttackIntro(
        BattleUnit attacker,
        BattleUnit target,
        Action onComplete = null)
    {
        if (attacker == null)
        {
            onComplete?.Invoke();
            return;
        }

        BattleUnit overviewUnit =
            target != null
                ? target
                : attacker;

        if (attacker.TeamType == BattleTeamType.Enemy)
        {
            PlayTargetOverview(
                overviewUnit,
                onComplete);

            return;
        }

        PlaySkillLowAngle(
            attacker,
            () =>
            {
                PlayTargetOverview(
                    overviewUnit,
                    onComplete);
            });
    }

    /// <summary>
    /// 행동자와 대상 위치 기준 전투 카메라 위치 계산
    /// </summary>
    /// <param name="actorTransform">행동자 Transform</param>
    /// <param name="targetPosition">대상 위치</param>
    /// <param name="backDistance">뒤쪽 거리</param>
    /// <param name="sideOffset">좌우 오프셋</param>
    /// <param name="cameraHeight">카메라 높이</param>
    /// <param name="targetFocusWeight">대상 주시 비중</param>
    /// <param name="focusHeight">주시점 높이</param>
    /// <param name="cameraPosition">계산된 카메라 위치</param>
    /// <param name="focusPosition">계산된 주시 위치</param>
    private void CalculateBattleRelativePose(
        Transform actorTransform,
        Vector3 targetPosition,
        float backDistance,
        float sideOffset,
        float cameraHeight,
        float targetFocusWeight,
        float focusHeight,
        out Vector3 cameraPosition,
        out Vector3 focusPosition)
    {
        Vector3 planarTargetPosition = targetPosition;
        planarTargetPosition.y = actorTransform.position.y;

        Vector3 viewForward =
            planarTargetPosition -
            actorTransform.position;

        if (viewForward.sqrMagnitude <= 0.0001f)
        {
            viewForward = actorTransform.forward;
            viewForward.y = 0f;
        }

        if (viewForward.sqrMagnitude <= 0.0001f)
        {
            viewForward = Vector3.forward;
        }

        viewForward.Normalize();

        Vector3 viewRight =
            Vector3.Cross(
                Vector3.up,
                viewForward).normalized;

        cameraPosition =
            actorTransform.position -
            viewForward * backDistance +
            viewRight * sideOffset +
            Vector3.up * cameraHeight;

        Vector3 focusGroundPosition =
            Vector3.Lerp(
                actorTransform.position,
                planarTargetPosition,
                Mathf.Clamp01(targetFocusWeight));

        focusPosition =
            focusGroundPosition +
            Vector3.up * focusHeight;
    }

    /// <summary>
    /// 상대 팀 타입 반환
    /// </summary>
    /// <param name="teamType">기준 팀</param>
    /// <returns>상대 팀</returns>
    private BattleTeamType GetOpposingTeam(BattleTeamType teamType)
    {
        return teamType == BattleTeamType.Player
            ? BattleTeamType.Enemy
            : BattleTeamType.Player;
    }
}