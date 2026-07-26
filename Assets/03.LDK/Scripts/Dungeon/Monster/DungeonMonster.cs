using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DungeonMonster : MonoBehaviour
{
    public enum AIState { Idle, Chasing, Combat }

    [SerializeField] private ParticleSystem _noticePaticle;

    [Header("Chase Settings")]
    [SerializeField] private float _detectionDistance = 10.0f;
    [SerializeField] private float _combatDistance = 2.0f;
    [SerializeField] private float _loseTargetDistance = 15.0f; // 추적 포기 거리
    [SerializeField] private float _pathUpdateInterval = 0.35f;

    public event Action OnCombatStarted;

    private NavMeshAgent _agent;
    private Transform _targetPlayer;
    private Coroutine _chaseCoroutine;
    
    private float _detectionDistanceSqr;
    private float _combatDistanceSqr;

    public AIState CurrentState { get; private set; } = AIState.Idle;

    private RoomNode _ownerRoom;

    private MonsterAnimationController _monsterAnimation;

    public void Initialize(RoomNode room)
    {
        _ownerRoom = room;
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        // 연산 최적화를 위한 제곱근 캐싱
        _detectionDistanceSqr = _detectionDistance * _detectionDistance;
        _combatDistanceSqr = _combatDistance * _combatDistance;
        
        _agent.stoppingDistance = _combatDistance;
    }

    public void Start()
    {
        FindPlayer();

        SetState(AIState.Idle);

        _monsterAnimation = GetComponentInChildren<MonsterAnimationController>();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) _targetPlayer = player.transform;
    }

    private void Update()
    {
        // 런타임 중 플레이어 참조가 끊길 경우 재검색
        if (_targetPlayer == null) 
        {
            FindPlayer();
            if (_targetPlayer == null) return;
        }

        // 플레이어와의 거리 측정 (sqrMagnitude로 연산 최적화)
        float distanceSqr = (_targetPlayer.position - transform.position).sqrMagnitude;

        // 현재 상태에 따라 유동적으로 상태 변환 검사
        switch (CurrentState)
        {
            case AIState.Idle:
                // 감지 거리 안으로 들어오면 추적 개시
                if (distanceSqr <= _detectionDistanceSqr)
                {
                    if(IsPlayerInsideRoom())
                    {
                        SetState(AIState.Chasing);
                    }
                }
                break;

            case AIState.Chasing:
                if (!IsPlayerInsideRoom())
                {
                    SetState(AIState.Idle);
                    break;
                }
                if(distanceSqr <= _combatDistanceSqr)
                {
                    SetState(AIState.Combat);
                }
                break;
                
            case AIState.Combat:
                break;
        }
    }

    private bool IsPlayerInsideRoom()
    {
        if (_ownerRoom == null)
        {
            return false;
        }


        Vector3 playerPos = _targetPlayer.position;


        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(playerPos.x),
            Mathf.RoundToInt(playerPos.z)
        );


        return _ownerRoom.Bounds.Contains(gridPos);
    }

    private void SetState(AIState newState)
    {
        if (CurrentState == newState) return;

        // 이전 상태 탈출 처리
        if (CurrentState == AIState.Chasing)
        {
            StopChaseRoutine();
        }

        CurrentState = newState;

        // 새로운 상태 진입 처리
        switch (CurrentState)
        {
            case AIState.Idle:
                _targetPlayer = null; // 타겟 참조 해제
                ResetAgentPath();
                _monsterAnimation.SetIsMoving(false);
                break;

            case AIState.Chasing:
                if (_agent.isActiveAndEnabled)
                {
                    if(_noticePaticle != null)
                    {
                        _noticePaticle.Play();
                    }
                    _agent.isStopped = false;
                    _chaseCoroutine = StartCoroutine(ChaseTargetRoutine());
                    _monsterAnimation.SetIsMoving(true);
                }
                break;

            case AIState.Combat:
                if (_agent.isActiveAndEnabled)
                {
                    _agent.isStopped = true;
                    ResetAgentPath();
                }
                _monsterAnimation.SetIsMoving(false);
                ShowMessageManager.Instance.ShowMessage("전투 개시");
                Debug.Log("1. 몬스터가 전투 이벤트를 호출함");
                OnCombatStarted?.Invoke(); 
                break;
        }
    }

    private IEnumerator ChaseTargetRoutine()
    {
        var wait = new WaitForSeconds(_pathUpdateInterval);

        while (CurrentState == AIState.Chasing)
        {
            if (_targetPlayer != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.SetDestination(_targetPlayer.position);
            }
            yield return wait;
        }
    }

    private void StopChaseRoutine()
    {
        if (_chaseCoroutine != null)
        {
            StopCoroutine(_chaseCoroutine);
            _chaseCoroutine = null;
        }
    }

    private void ResetAgentPath()
    {
        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }
    }
}