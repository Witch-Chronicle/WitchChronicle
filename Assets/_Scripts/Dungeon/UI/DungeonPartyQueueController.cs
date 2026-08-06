using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 필드에서 현재 파티 구성원만큼 DungeonCharacterStatusView를 동적 생성해서 표시.
/// PartyQueueController와 달리 본인 턴 강조/스케일 애니메이션 없음 - 그냥 현재 파티원 목록만 반영.
/// 던전 중간에 파티 편성이 바뀌는 일은 없다는 전제라 씬 진입 시 한 번만 스폰.
/// </summary>
public class DungeonPartyQueueController : MonoBehaviour
{

    public static DungeonPartyQueueController Instance { get; private set; }

    [Header("Dynamic Spawn")]
    [SerializeField] private DungeonCharacterStatusView _statusViewPrefab;
    [SerializeField] private Transform _contentParent; // HorizontalLayoutGroup이 붙어있는 오브젝트

    private readonly List<DungeonCharacterStatusView> _spawnedViews = new List<DungeonCharacterStatusView>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        SpawnPartyViews();
    }

    private void SpawnPartyViews()
    {
        if (PersistentCharacterManager.Instance == null)
        {
            Debug.LogWarning("[DungeonPartyQueueController] PersistentCharacterManager.Instance가 없습니다.");
            return;
        }

        if (_statusViewPrefab == null || _contentParent == null)
        {
            Debug.LogWarning("[DungeonPartyQueueController] _statusViewPrefab 또는 _contentParent가 연결되지 않았습니다.");
            return;
        }

        ClearSpawnedViews();

        List<PersistentCharacterUnit> activeParty = new List<PersistentCharacterUnit>();
        PersistentCharacterManager.Instance.GetActivePartyMembers(activeParty);

        for (int i = 0; i < activeParty.Count; i++)
        {
            PersistentCharacterUnit unit = activeParty[i];

            if (unit == null) continue;

            DungeonCharacterStatusView view = Instantiate(_statusViewPrefab, _contentParent);
            view.gameObject.SetActive(true);
            view.Bind(unit);

            _spawnedViews.Add(view);
        }
    }

    private void ClearSpawnedViews()
    {
        for (int i = 0; i < _spawnedViews.Count; i++)
        {
            if (_spawnedViews[i] != null)
            {
                Destroy(_spawnedViews[i].gameObject);
            }
        }

        _spawnedViews.Clear();
    }
}