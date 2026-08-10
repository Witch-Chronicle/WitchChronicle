using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 낚시 상태 머신 + 어종 추첨 + 낚싯대 관리 + 세션 인벤토리.
/// 낚싯대 보유 여부는 PlayerInventory를 실시간으로 스캔해서 판정한다.
/// </summary>
public class FishingManager : MonoBehaviour
{
    public static FishingManager Instance { get; private set; }

    [Header("어종 데이터 (전체 목록)")]
    [SerializeField] private List<FishItemData> allFishes = new List<FishItemData>();

    [Header("낚싯대 - 게임에 존재하는 모든 낚싯대 SO")]
    [Tooltip("모든 낚싯대 SO를 등록. 실제 보유 여부는 인벤토리에서 판정.")]
    [SerializeField] private List<RodItemData> allRods = new List<RodItemData>();

    [Header("타이밍 설정")]
    [SerializeField] private float castingDuration = 0.5f;
    [SerializeField] private float minWaitTime = 3f;
    [SerializeField] private float maxWaitTime = 8f;
    [SerializeField] private float biteWindow = 1.5f;

    [Header("Animator Hook (선택)")]
    [SerializeField] private WitchChronicle.Fishing.FishingAnimatorHook animatorHook;

    // 상태
    private FishingState _state = FishingState.Idle;
    private Coroutine _stateRoutine;
    private FishItemData _hookedFish;
    private int _sessionCatchCount = 0;
    private readonly List<FishItemData> _caughtFishesThisSession = new List<FishItemData>();

    // 낚싯대 (인벤 기반)
    private RodItemData _currentRod;

    // 세션
    private FishingSpot _currentSpot;
    private bool _isSessionActive;

    // 이벤트
    public event Action<FishingState> OnStateChanged;
    public event Action<FishItemData> OnFishHooked;
    public event Action<FishItemData> OnFishCaught;
    public event Action<FishingReelController.FailReason> OnFishEscaped;
    public event Action<RodItemData> OnRodEquipped;
    public event Action OnRodInventoryChanged; // 인벤 낚싯대 목록 변경 시
    public event Action OnFishingSessionStarted;
    public event Action OnFishingSessionEnded;

    // 공개 프로퍼티
    public FishingState State => _state;
    public FishItemData HookedFish => _hookedFish;
    public int SessionCatchCount => _sessionCatchCount;
    public IReadOnlyList<FishItemData> CaughtFishesThisSession => _caughtFishesThisSession;
    public IReadOnlyList<RodItemData> AllRods => allRods;
    public RodItemData CurrentRod => _currentRod;
    public int CurrentRodRank => _currentRod != null ? _currentRod.rodRank : 1;
    public FishGrade MaxCatchableGrade => _currentRod != null ? _currentRod.maxCatchableGrade : FishGrade.Common;
    public bool IsSessionActive => _isSessionActive;

    /// <summary>낚싯대 하나라도 보유 중인지</summary>
    public bool HasAnyRod => GetOwnedRods().Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 인벤 변경 이벤트 구독 → 낚싯대 목록 자동 갱신
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += HandleInventoryChanged;
        }

        // 초기 장착 상태 갱신
        RefreshCurrentRodFromInventory();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= HandleInventoryChanged;

        if (Instance == this) Instance = null;
    }

    private void HandleInventoryChanged()
    {
        RefreshCurrentRodFromInventory();
        OnRodInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 인벤에 있는 낚싯대 목록 반환 (SO 기준으로 중복 제거).
    /// </summary>
    public List<RodItemData> GetOwnedRods()
    {
        var owned = new List<RodItemData>();
        if (PlayerInventory.Instance == null) return owned;

        foreach (var rod in allRods)
        {
            if (rod == null) continue;
            if (PlayerInventory.Instance.GetTotalQuantity(rod) > 0)
                owned.Add(rod);
        }
        return owned;
    }

    /// <summary>
    /// 인벤 기준으로 현재 장착 낚싯대를 유효하게 유지.
    /// - 장착 중인 게 인벤에 없어졌으면 → 자동으로 다른 낚싯대로 교체 (없으면 null)
    /// - 아무것도 장착 안 된 상태에서 인벤에 낚싯대 생겼으면 → 자동 장착
    /// </summary>
    private void RefreshCurrentRodFromInventory()
    {
        var owned = GetOwnedRods();

        // 현재 장착 낚싯대가 인벤에 있으면 유지
        if (_currentRod != null && owned.Contains(_currentRod))
            return;

        // 인벤에 낚싯대 있으면 첫 번째 자동 장착
        if (owned.Count > 0)
        {
            _currentRod = owned[0];
            OnRodEquipped?.Invoke(_currentRod);
            Debug.Log($"[FishingManager] 낚싯대 자동 장착: {_currentRod.itemName}");
        }
        else
        {
            // 하나도 없으면 해제
            if (_currentRod != null)
            {
                _currentRod = null;
                OnRodEquipped?.Invoke(null);
                Debug.Log("[FishingManager] 낚싯대 없음 → 해제");
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // 세션 관리
    // ─────────────────────────────────────────────────────────

    public void BindAnimatorHook(WitchChronicle.Fishing.FishingAnimatorHook hook)
    {
        if (hook != null) animatorHook = hook;
    }

    public void EnterFishing(FishingSpot spot)
    {
        if (_isSessionActive)
        {
            Debug.LogWarning("[FishingManager] 이미 세션 진행 중.");
            return;
        }

        _currentSpot = spot;
        _isSessionActive = true;

        _sessionCatchCount = 0;
        _caughtFishesThisSession.Clear();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        animatorHook?.OnEnterFishing();
        OnFishingSessionStarted?.Invoke();

        // ★ 낚싯대 없으면 Idle만 유지하고 자동 시작 안 함
        // UI에서 "줄 풀기" 눌렀을 때 낚싯대 유무 판정
        if (HasAnyRod)
        {
            StartFishing();
        }
        else
        {
            _state = FishingState.Idle;
            OnStateChanged?.Invoke(_state);
        }

        Debug.Log("[FishingManager] 낚시 세션 시작");
    }

    public void ExitFishing()
    {
        if (!_isSessionActive) return;

        EndSession();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        animatorHook?.OnExitFishing();
        OnFishingSessionEnded?.Invoke();

        if (_currentSpot != null)
        {
            _currentSpot.ExitFishing();
            _currentSpot = null;
        }

        _isSessionActive = false;
        Debug.Log("[FishingManager] 낚시 세션 종료");
    }

    // ─────────────────────────────────────────────────────────
    // 낚시 사이클 API
    // ─────────────────────────────────────────────────────────

    public void StartFishing()
    {
        if (_state != FishingState.Idle)
        {
            Debug.LogWarning($"[FishingManager] StartFishing 무시: 현재 상태 {_state}");
            return;
        }

        // ★ 낚싯대 재검증
        if (!HasAnyRod || _currentRod == null)
        {
            Debug.LogWarning("[FishingManager] 낚싯대 없어서 낚시 불가");
            return;
        }

        _stateRoutine = StartCoroutine(Co_Casting());
    }

    public void OnCatchButtonPressed()
    {
        if (_state == FishingState.Bite)
        {
            if (_stateRoutine != null) StopCoroutine(_stateRoutine);
            ChangeState(FishingState.Reeling);
        }
        else if (_state == FishingState.Waiting)
        {
            Debug.Log("[FishingManager] 너무 일찍 낚아챘어요!");
            FailAndReset(FishingReelController.FailReason.Escape);
        }
    }

    public void CompleteReeling(bool success, FishItemData fish, FishingReelController.FailReason reason)
    {
        if (_state != FishingState.Reeling) return;

        if (success && fish != null)
        {
            _sessionCatchCount++;
            _caughtFishesThisSession.Add(fish);
            OnFishCaught?.Invoke(fish);
            GiveFishToInventory(fish);

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.AddProgress(QuestObjectiveType.CatchFish, fish.itemId.ToString(), 1);
            }

            animatorHook?.OnCatchSuccess();
        }
        else
        {
            OnFishEscaped?.Invoke(reason);
            animatorHook?.OnCatchFail();
        }

        ChangeState(FishingState.Result);
    }

    /// <summary>
    /// 낚싯대 장착 변경. 인벤에 있는 낚싯대만 장착 가능.
    /// </summary>
    public void EquipRod(RodItemData rod)
    {
        if (rod == null) return;

        var owned = GetOwnedRods();
        if (!owned.Contains(rod))
        {
            Debug.LogWarning($"[FishingManager] 인벤에 없는 낚싯대: {rod.itemName}");
            return;
        }

        if (_currentRod == rod) return;
        _currentRod = rod;
        OnRodEquipped?.Invoke(rod);
        Debug.Log($"[FishingManager] 낚싯대 장착: {rod.itemName}");
    }

    public void ReturnToIdle()
    {
        _hookedFish = null;
        ChangeState(FishingState.Idle);
    }

    public void EndSession()
    {
        if (_stateRoutine != null) StopCoroutine(_stateRoutine);
        _hookedFish = null;
        _sessionCatchCount = 0;
        _caughtFishesThisSession.Clear();
        _state = FishingState.Idle;
    }

    // ─────────────────────────────────────────────────────────
    // 내부
    // ─────────────────────────────────────────────────────────

    private IEnumerator Co_Casting()
    {
        ChangeState(FishingState.Casting);
        yield return new WaitForSeconds(castingDuration);
        _stateRoutine = StartCoroutine(Co_Waiting());
    }

    private IEnumerator Co_Waiting()
    {
        ChangeState(FishingState.Waiting);
        float wait = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(wait);

        _hookedFish = PickRandomFish();
        if (_hookedFish == null)
            Debug.LogWarning("[FishingManager] 낚을 수 있는 물고기가 없음.");

        OnFishHooked?.Invoke(_hookedFish);
        _stateRoutine = StartCoroutine(Co_BiteWindow());
    }

    private IEnumerator Co_BiteWindow()
    {
        ChangeState(FishingState.Bite);
        yield return new WaitForSeconds(biteWindow);

        if (_state == FishingState.Bite)
        {
            Debug.Log("[FishingManager] 물고기 놓침 (시간 초과)");
            OnFishEscaped?.Invoke(FishingReelController.FailReason.Escape);
            animatorHook?.OnCatchFail();
            _hookedFish = null;
            ChangeState(FishingState.Result);
        }
    }

    private FishItemData PickRandomFish()
    {
        int rodRank = CurrentRodRank;
        FishGrade maxGrade = MaxCatchableGrade;

        var pool = allFishes
            .Where(f => f != null
                        && (int)f.grade <= (int)maxGrade
                        && f.minRodRank <= rodRank)
            .ToList();

        if (pool.Count == 0)
        {
            Debug.LogWarning($"[FishingManager] 조건에 맞는 물고기 없음 (rodRank={rodRank}, maxGrade={maxGrade})");
            return null;
        }

        float totalWeight = pool.Sum(f => f.spawnWeight);
        if (totalWeight <= 0f) return pool[UnityEngine.Random.Range(0, pool.Count)];

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cursor = 0f;
        foreach (var fish in pool)
        {
            cursor += fish.spawnWeight;
            if (roll <= cursor) return fish;
        }
        return pool[pool.Count - 1];
    }

    private void FailAndReset(FishingReelController.FailReason reason)
    {
        if (_stateRoutine != null) StopCoroutine(_stateRoutine);
        _hookedFish = null;
        OnFishEscaped?.Invoke(reason);
        animatorHook?.OnCatchFail();
        ChangeState(FishingState.Result);
    }

    private void ChangeState(FishingState next)
    {
        _state = next;
        Debug.Log($"[FishingManager] 상태 → {next}");

        switch (next)
        {
            case FishingState.Casting: animatorHook?.OnCastStart(); break;
            case FishingState.Bite:    animatorHook?.OnBite(); break;
            case FishingState.Reeling: animatorHook?.OnReelStart(); break;
        }

        OnStateChanged?.Invoke(next);
    }

    private void GiveFishToInventory(FishItemData fish)
    {
        if (fish == null) return;
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[FishingManager] PlayerInventory.Instance 없음");
            return;
        }

        PlayerInventory.Instance.AddItem(fish, 1);
        Debug.Log($"[FishingManager] 인벤토리 지급: {fish.itemName} x1");
    }
}