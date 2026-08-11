using System.Collections.Generic;
using UnityEngine;
public class DungeonPartyQueueController : MonoBehaviour
{
    public static DungeonPartyQueueController Instance { get; private set; }
    [Header("Dynamic Spawn")]
    [SerializeField] private DungeonCharacterStatusView _statusViewPrefab;
    [SerializeField] private Transform _contentParent; // HorizontalLayoutGroup이 붙어있는 오브젝트
    [SerializeField] private GameObject _tipsPanel;
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
    private void OnEnable()
    {
        TrySubscribeBattleContext();
    }
    private void OnDisable()
    {
        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnBattleStarted -= HandleBattleStarted;
        }
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
        TrySubscribeBattleContext();
    }

    /// <summary>
    /// BattleUIContext가 이 스크립트보다 늦게 초기화될 수 있어 OnEnable/Start 양쪽에서 재시도.
    /// </summary>
    private void TrySubscribeBattleContext()
    {
        if (BattleUIContext.Instance == null)
        {
            return;
        }
        BattleUIContext.Instance.OnBattleStarted -= HandleBattleStarted;
        BattleUIContext.Instance.OnBattleStarted += HandleBattleStarted;
    }
    /// <summary>
    /// 전투가 시작되면(Battle 씬이 Additive로 올라오면) 던전 파티 상태 UI를 숨긴다.
    /// </summary>
    private void HandleBattleStarted()
    {
        HideContent();
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
    public void HideContent()
    {
        _contentParent.gameObject.SetActive(false);
        _tipsPanel.SetActive(false);
    }
    public void ShowContent()
    {
        if (_contentParent == null)
        {
            return;
        }
        if (_contentParent.gameObject.activeSelf == false)
        {
            _contentParent.gameObject.SetActive(true);
            _tipsPanel.SetActive(true);
        }
    }
}