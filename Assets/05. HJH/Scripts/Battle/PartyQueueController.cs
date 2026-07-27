using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Characters 오브젝트에 붙어서 화면 하단에 아군 파티 상태를 가로로 표시.
/// - 전투 시작 시 파티 인원수만큼 _statusViewPrefab을 동적으로 생성.
///   HorizontalLayoutGroup이 알아서 간격 맞춰 정렬해줌.
/// - 본인 턴이면 그 캐릭터 뷰의 Visual(BattleCharacterStatusView.VisualRoot) 실제 크기(sizeDelta)를
///   확대(예: 300x150 -> 345x172.5), 나머지는 기본 크기로 복귀.
///   Slot(레이아웃이 관리하는 루트)은 건드리지 않으므로 옆 캐릭터 위치가 흔들리지 않음.
/// </summary>
public class PartyQueueController : MonoBehaviour
{
    [Header("Dynamic Spawn")]
    [SerializeField] private BattleCharacterStatusView _statusViewPrefab;
    [SerializeField] private Transform _contentParent; // HorizontalLayoutGroup이 붙어있는 Characters 오브젝트

    [Header("Size Animation")]
    [SerializeField] private float _duration = 0.25f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [Tooltip("본인 턴일 때 VisualRoot의 sizeDelta에 곱해지는 배율 (예: 1.15 -> 300x150이면 345x172.5)")]
    [SerializeField] private float _selectedSizeMultiplier = 1.15f;

    private readonly List<BattleCharacterStatusView> _spawnedViews = new List<BattleCharacterStatusView>();
    private readonly List<Vector2> _baseSizes = new List<Vector2>();

    private bool _isSubscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        RefreshInitialState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>
    /// 전투 시작 시 파티 인원수만큼 뷰를 동적으로 생성하고 순서대로 바인딩.
    /// </summary>
    private void HandleBattleStarted()
    {
        if (BattleUIContext.Instance == null) return;
        if (_statusViewPrefab == null || _contentParent == null)
        {
            Debug.LogWarning("[PartyQueueController] _statusViewPrefab 또는 _contentParent가 연결되지 않았습니다.");
            return;
        }

        ClearSpawnedViews();

        IReadOnlyList<BattleUnit> party = BattleUIContext.Instance.PartyUnits;

        for (int i = 0; i < party.Count; i++)
        {
            BattleCharacterStatusView view = Instantiate(_statusViewPrefab, _contentParent);
            view.gameObject.SetActive(true);
            view.Bind(party[i]);

            RectTransform visualRoot = view.VisualRoot;
            Vector2 baseSize = visualRoot != null ? visualRoot.sizeDelta : Vector2.zero;

            _spawnedViews.Add(view);
            _baseSizes.Add(baseSize);
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
        _baseSizes.Clear();
    }

    /// <summary>
    /// 아군 턴 시작 시: 그 캐릭터의 Visual만 실제 크기(sizeDelta) 확대, 나머지는 기본 크기로 복귀.
    /// 동시에 이번 라운드 턴 순서를 조회해서 각 뷰의 순번(OrderTxt)도 갱신.
    /// </summary>
    private void HandleTurnStarted(BattleUnit unit)
    {
        bool isPlayerTurn = unit != null && unit.TeamType == BattleTeamType.Player;

        Dictionary<BattleUnit, int> orderLookup = BuildTurnOrderLookup();

        for (int i = 0; i < _spawnedViews.Count; i++)
        {
            BattleCharacterStatusView view = _spawnedViews[i];
            if (view == null) continue;

            bool isDead = view.BoundUnit != null && view.BoundUnit.IsAlive == false;
            int roundOrderNumber = view.BoundUnit != null && orderLookup.TryGetValue(view.BoundUnit, out int order) ? order : 0;
            view.UpdateOrder(roundOrderNumber, isDead);

            RectTransform visualRoot = view.VisualRoot;
            if (visualRoot == null) continue;

            bool isSelected = isPlayerTurn && view.BoundUnit == unit;

            Vector2 targetSize = isSelected
                ? _baseSizes[i] * _selectedSizeMultiplier
                : _baseSizes[i];

            visualRoot.DOKill();
            visualRoot.DOSizeDelta(targetSize, _duration).SetEase(_ease);
        }
    }

    /// <summary>
    /// 이번 라운드 전체 턴 순서를 조회해서 유닛 -> 순번(1-based) 딕셔너리로 변환.
    /// </summary>
    private Dictionary<BattleUnit, int> BuildTurnOrderLookup()
    {
        Dictionary<BattleUnit, int> lookup = new Dictionary<BattleUnit, int>();

        if (BattleUIContext.Instance == null) return lookup;

        List<BattleUnit> fullOrder = new List<BattleUnit>();
        BattleUIContext.Instance.GetCurrentTurnOrder(fullOrder, true);

        for (int i = 0; i < fullOrder.Count; i++)
        {
            if (fullOrder[i] != null)
            {
                lookup[fullOrder[i]] = i + 1;
            }
        }

        return lookup;
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;
        if (BattleUIContext.Instance == null) return;

        BattleUIContext.Instance.OnBattleStarted += HandleBattleStarted;
        BattleUIContext.Instance.OnTurnStarted += HandleTurnStarted;

        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (_isSubscribed == false) return;

        if (BattleUIContext.Instance == null)
        {
            _isSubscribed = false;
            return;
        }

        BattleUIContext.Instance.OnBattleStarted -= HandleBattleStarted;
        BattleUIContext.Instance.OnTurnStarted -= HandleTurnStarted;

        _isSubscribed = false;
    }

    private void RefreshInitialState()
    {
        if (BattleUIContext.Instance == null) return;

        if (BattleUIContext.Instance.PartyUnits != null && BattleUIContext.Instance.PartyUnits.Count > 0)
        {
            HandleBattleStarted();
        }

        if (BattleUIContext.Instance.CurrentUnit != null)
        {
            HandleTurnStarted(BattleUIContext.Instance.CurrentUnit);
        }
    }
}