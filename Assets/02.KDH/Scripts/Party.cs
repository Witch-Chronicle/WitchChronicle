using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// 파티 4인 접근 창구 + 씬 전환 영속성.
/// 씬의 빈 오브젝트("Party")에 붙이고 인스펙터에 4명 등록 — 순서: 아리엘, 라이아, 페이, 셀레네.
/// 캐릭터 4명(+카메라)을 이 오브젝트의 "자식"으로 두면 씬이 바뀌어도 레벨·경험치·HP가 유지된다.
///
/// 씬 전환 규약:
///   - 각 씬에 PartySpawnPoint를 하나 배치 → 씬 로드 시 파티가 그 위치로 이동
///   - 다른 씬에 테스트용 Party 세트가 또 있어도 됨 (런타임에 중복은 자동 파괴 — 씬 단독 테스트 가능)
///
/// 사용처:
///   UI  → Party.Instance.Members[탭 번호] 로 캐릭터별 StatController 접근
///   전투 → Party.Instance.AddExpToAll(경험치) 로 승리 보상 지급
///   던전 복귀 → Party.Instance.MoveTo(저장해둔 위치, 회전) 로 위치 복원
/// </summary>
public class Party : MonoBehaviour
{
    public static Party Instance { get; private set; }

    [SerializeField] private List<StatController> _members = new();   // [0]=아리엘 고정

    /// 파티 전원 (읽기 전용)
    public IReadOnlyList<StatController> Members => _members;

    /// 아리엘 (편의 창구)
    public StatController Leader => _members.Count > 0 ? _members[0] : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject);   // 씬이 바뀌어도 파티(자식 캐릭터 포함) 유지
    }

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    /// <summary>
    /// 싱글톤 해제
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// 씬 로드 시: 스폰 포인트가 있으면 파티를 그 위치로, 없으면(전투 씬 등) 파티를 숨김
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;

        var spawn = FindAnyObjectByType<PartySpawnPoint>();
        SetMembersActive(spawn != null);
        if (spawn != null)
            MoveTo(spawn.transform.position, spawn.transform.rotation);
    }

    /// 파티 캐릭터 표시/숨김 (Party 오브젝트 자체는 항상 살아 있어야 하므로 멤버만 토글)
    private void SetMembersActive(bool active)
    {
        foreach (var member in _members)
            if (member != null) member.gameObject.SetActive(active);
    }

    /// 파티 전원을 지정 위치로 순간이동 (씬 스폰, 전투 후 던전 위치 복원 등)
    public void MoveTo(Vector3 position, Quaternion rotation)
    {
        // 리더(아리엘): CharacterController는 켜진 상태로 transform을 옮기면 무시되므로 껐다 켠다
        var leader = Leader;
        if (leader != null)
        {
            var controller = leader.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            leader.transform.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = true;
        }

        // NPC들: 리더 뒤쪽에 좌우로 흩어 배치
        for (int i = 1; i < _members.Count; i++)
        {
            Vector3 offset = rotation * new Vector3((i - 2) * 1.2f, 0f, -1.5f);
            Vector3 target = position + offset;

            var agent = _members[i].GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled &&
                NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                _members[i].transform.SetPositionAndRotation(target, rotation);
            }
        }
    }

    /// <summary>
    /// 현재 파티 원본 캐릭터에게 경험치 지급
    /// </summary>
    /// <param name="amount">지급 경험치</param>
    public void AddExpToAll(int amount)
    {
        if (PersistentCharacterManager.Instance == null)
        {
            Debug.LogWarning("[Party] PersistentCharacterManager 없음");
            return;
        }

        List<PersistentCharacterUnit> activePartyMembers = new List<PersistentCharacterUnit>();
        PersistentCharacterManager.Instance.GetActivePartyMembers(activePartyMembers);

        for (int i = 0; i < activePartyMembers.Count; i++)
        {
            PersistentCharacterUnit character = activePartyMembers[i];

            if (character == null || character.StatController == null)
            {
                continue;
            }

            character.StatController.AddExp(amount);
        }
    }

    /// <summary>
    /// 외부(MainSpawner)가 스폰한 파티 필드 멤버 목록을 주입
    /// </summary>
    /// <param name="members">스폰된 순서대로의 StatController 목록 (0번 = 리더)</param>
    public void SetMembers(List<StatController> members)
    {
        _members.Clear();
        if (members != null)
            _members.AddRange(members);
    }
}
