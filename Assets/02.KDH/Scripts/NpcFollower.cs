using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// NPC 필드 팔로우 AI (자유 추적 / leash 방식 — 엔드필드 스타일).
/// 고정 대형 없음: 아리엘 반경 안에 있으면 가만히 있고, 멀어지면 따라옴.
/// 목적지는 아리엘의 정확한 위치가 아니라 "자기가 오던 방향 쪽 근처"로 잡아서
/// 여럿이 한 점에 뭉치지 않고 자연스럽게 흩어져 따라옴.
[RequireComponent(typeof(NavMeshAgent))]
public class NpcFollower : MonoBehaviour
{
    [Header("추적 대상")]
    [SerializeField] private Transform _anchor;                 // 아리엘

    [Header("추적 거리 — 언제 출발하고 어디서 멈추는지")]
    [SerializeField] private float _followStartDistance = 4.5f; // 이보다 멀어지면 따라가기 시작
    [SerializeField] private float _stopDistance = 2.5f;        // 아리엘과 이 정도 거리에서 멈춤
    [SerializeField] private float _teleportDistance = 20f;     // 이 이상 벌어지면 근처로 순간이동

    [Header("이동 속도")]
    [SerializeField] private float _walkSpeed = 4.5f;           // 기본 속도 (NPC마다 다르게 권장: 4.3/4.6/4.9)
    [SerializeField] private float _runSpeed = 7f;              // 많이 뒤처졌을 때 속도
    [SerializeField] private float _runDistance = 8f;           // 이보다 멀면 달리기로 전환

    [Header("무리 분산 — 이동 중 뭉침 방지")]
    [SerializeField] private float _lane = 0f;                   // 좌우 차선 (라이아 -1.2 / 셀레네 0 / 페이 1.2)
    [SerializeField] private float _separationRadius = 1.6f;     // 동료와 이 거리 안이면 서로 밀어냄
    [SerializeField] private float _separationStrength = 1.5f;   // 밀어내는 세기 (3 이상은 지그재그 주의)

    private static readonly List<NpcFollower> _all = new();      // 씬의 모든 팔로워 (분리 계산용)

    private NavMeshAgent _agent;
    private bool _following;
    private Vector3 _followDir;   // 이번 추적에서 내가 설 방향 (아리엘 기준)

    // 애니메이션 연동 (PlayerController와 같은 방식 — 파라미터가 있으면 채우고 없으면 무시)
    private Animator _animator;
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int GroundedParam = Animator.StringToHash("Grounded");
    private static readonly int MotionSpeedParam = Animator.StringToHash("MotionSpeed");
    private bool _hasGroundedParam;
    private bool _hasMotionSpeedParam;

    private void OnEnable() => _all.Add(this);
    private void OnDisable() => _all.Remove(this);

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = 0.2f;

        _animator = GetComponentInChildren<Animator>();   // 모델이 자식으로 붙으면 자동 연결
        if (_animator != null)
        {
            foreach (var parameter in _animator.parameters)
            {
                if (parameter.nameHash == GroundedParam) _hasGroundedParam = true;
                if (parameter.nameHash == MotionSpeedParam) _hasMotionSpeedParam = true;
            }
        }
    }

    private void LateUpdate()
    {
        // 이동 속도 → 애니메이션 (Idle/Walk/Run 전환)
        if (_animator == null) return;
        _animator.SetFloat(SpeedParam, _agent.velocity.magnitude);
        if (_hasGroundedParam) _animator.SetBool(GroundedParam, true);           // NavMesh 위 = 항상 접지
        if (_hasMotionSpeedParam) _animator.SetFloat(MotionSpeedParam, 1f);      // 재생 배속
    }

    private void Update()
    {
        if (_anchor == null) return;

        float distance = Vector3.Distance(transform.position, _anchor.position);

        // 씬 전환·컷씬 직후 등으로 너무 멀어졌으면 아리엘 근처로 순간이동
        if (distance > _teleportDistance)
        {
            Vector3 near = _anchor.position - _anchor.forward * _stopDistance;
            if (NavMesh.SamplePosition(near, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                _agent.Warp(hit.position);
            _following = false;
            return;
        }

        // 히스테리시스: 출발(4.5m)과 정지(2.5m) 기준이 달라 경계에서 덜덜거리지 않음
        if (!_following && distance > _followStartDistance)
        {
            _following = true;

            // 추적 시작 시 개인 방향 결정: 지금 내가 있는 쪽 + 랜덤 ±30°
            // → NPC들이 서로 다른 방향에서 접근해 한 점에 뭉치지 않음
            Vector3 dir = (transform.position - _anchor.position).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = -_anchor.forward;
            _followDir = Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f) * dir;
        }

        if (!_following) return;   // 반경 안 → 가만히 있음

        _agent.speed = distance > _runDistance ? _runSpeed : _walkSpeed;

        // 목적지 = 아리엘 위치 + 내 방향 × 정지 거리 (아리엘이 움직이면 같이 갱신됨)
        Vector3 desired = _anchor.position + _followDir * _stopDistance;

        // 이동 중 차선 적용: 진행 방향의 옆으로 _lane만큼 비켜서 달림 → 셋이 같은 줄로 안 겹침
        // 도착이 가까워지면 차선을 줄여서 목적지를 빙 돌지 않게 함
        Vector3 toAnchor = (_anchor.position - transform.position).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, toAnchor);
        float laneScale = Mathf.Clamp01((distance - _stopDistance) / 3f);
        desired += side * (_lane * laneScale);

        // 분리(separation): 가까운 동료로부터 밀려나는 방향을 목적지에 반영
        // → 추격 경로가 겹쳐도 이동 중에 서로 벌어짐
        desired += ComputeSeparation() * _separationStrength;

        _agent.SetDestination(desired);

        if (distance <= _stopDistance + 0.3f)
        {
            _following = false;
            _agent.ResetPath();    // 도착 — 각자 자기 방향 자리에 멈춤
        }
    }

    /// 가까운 동료들로부터 밀려나는 방향 벡터 (가까울수록 강함)
    private Vector3 ComputeSeparation()
    {
        Vector3 push = Vector3.zero;
        foreach (var other in _all)
        {
            if (other == this) continue;
            Vector3 away = transform.position - other.transform.position;
            float dist = away.magnitude;
            if (dist < 0.01f || dist > _separationRadius) continue;
            push += away.normalized * (1f - dist / _separationRadius);
        }
        push.y = 0f;
        return push;
    }

    /// 영입/파티 구성 시 기준 대상 지정 (⑤ 영입 트리거에서 사용 가능)
    public void SetAnchor(Transform anchor) => _anchor = anchor;
}
