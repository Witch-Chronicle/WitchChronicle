using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

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

    [Header("Target Overview View")]
    [SerializeField] private float _overviewBackDistance = 5.0f;
    [SerializeField] private float _overviewHeight = 6.5f;
    [SerializeField] private float _overviewFocusHeight = 0.8f;
    [SerializeField] private float _overviewFov = 55f;
    [SerializeField] private float _overviewSideOffset = 0f;
    [SerializeField] private float _overviewFocusForward = 1.5f;
    [SerializeField] private float _overviewRoll = 0f;
    [SerializeField] private float _overviewWaitDuration = 0.35f;

    [Header("Skill Low Angle View")]
    [SerializeField] private float _skillFrontDistance = 1.6f;
    [SerializeField] private float _skillSideOffset = -0.5f;
    [SerializeField] private float _skillHeight = 0.45f;
    [SerializeField] private float _skillLookHeight = 1.8f;
    [SerializeField] private float _skillFov = 68f;
    [SerializeField] private float _skillRoll = -10f;
    [SerializeField] private float _skillWaitDuration = 0.4f;

    private Coroutine _waitRoutine;

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
    /// 플레이어 등 뒤 구도 재생
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

        ApplyCameraPose(
            _playerBackCamera,
            cameraPosition,
            focusPosition,
            _backFov,
            _backRoll);

        ActivateCamera(
            _playerBackCamera,
            _backWaitDuration,
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
    /// 스킬 선택 로우앵글 구도 재생
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

        Vector3 focusPosition =
            actorTransform.position +
            Vector3.up * _skillLookHeight;

        Vector3 cameraPosition =
            actorTransform.position +
            actorTransform.forward * _skillFrontDistance +
            actorTransform.right * _skillSideOffset +
            Vector3.up * _skillHeight;

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

        SetCameraPriority(targetCamera, _activePriority);

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
    /// 공격자 강조 후 전투 부감 구도 전환
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

        PlaySkillLowAngle(
            attacker,
            () =>
            {
                BattleUnit overviewUnit =
                    target != null
                        ? target
                        : attacker;

                PlayTargetOverview(
                    overviewUnit,
                    onComplete);
            });
    }
}