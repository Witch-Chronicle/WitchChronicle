using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 낚시 상태 머신 + 어종 추첨 + 낚싯대 관리 + 세션 인벤토리.
/// </summary>
public class FishingManager : MonoBehaviour
{
    public static FishingManager Instance { get; private set; }

    [Header("어종 데이터 (전체 목록)")]
    [SerializeField] private List<FishItemData> allFishes = new List<FishItemData>();

    [Header("낚싯대")]
    [Tooltip("보유 중인 낚싯대 목록.")]
    [SerializeField] private List<RodItemData> ownedRods = new List<RodItemData>();
    [Tooltip("현재 장착 중인 낚싯대. 비어있으면 ownedRods[0] 자동 사용.")]
    [SerializeField] private RodItemData currentRod;

    [Header("타이밍 설정")]
    [SerializeField] private float castingDuration = 0.5f;
    [SerializeField] private float minWaitTime = 3f;
    [SerializeField] private float maxWaitTime = 8f;
    [SerializeField] private float biteWindow = 1.5f;

    // 상태
    private FishingState _state = FishingState.Idle;
    private Coroutine _stateRoutine;
    private FishItemData _hookedFish;
    private int _sessionCatchCount = 0;
    private readonly List<FishItemData> _caughtFishesThisSession = new List<FishItemData>();

    // 이벤트
    public event Action<FishingState> OnStateChanged;
    public event Action<FishItemData> OnFishHooked;
    public event Action<FishItemData> OnFishCaught;
    public event Action<FishingReelController.FailReason> OnFishEscaped;
    public event Action<RodItemData> OnRodEquipped;

    // 공개 프로퍼티
    public FishingState State => _state;
    public FishItemData HookedFish => _hookedFish;
    public int SessionCatchCount => _sessionCatchCount;
    public IReadOnlyList<FishItemData> CaughtFishesThisSession => _caughtFishesThisSession;
    public IReadOnlyList<RodItemData> OwnedRods => ownedRods;
    public RodItemData CurrentRod => currentRod;
    public int CurrentRodRank => currentRod != null ? currentRod.rodRank : 1;
    public FishGrade MaxCatchableGrade => currentRod != null ? currentRod.maxCatchableGrade : FishGrade.Common;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 낚싯대 미지정 시 첫 번째 자동 장착
        if (currentRod == null && ownedRods.Count > 0)
            currentRod = ownedRods[0];
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────────────────
    // 외부 API
    // ─────────────────────────────────────────────────────────

    public void StartFishing()
    {
        if (_state != FishingState.Idle)
        {
            Debug.LogWarning($"[FishingManager] StartFishing 무시: 현재 상태 {_state}");
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
        }
        else
        {
            OnFishEscaped?.Invoke(reason);
        }

        ChangeState(FishingState.Result);
    }

    public void EquipRod(RodItemData rod)
    {
        if (rod == null || !ownedRods.Contains(rod)) return;
        if (currentRod == rod) return;
        currentRod = rod;
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
    // 내부 로직
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

        // 낚싯대 보정 없이 랜덤 대기 시간만 사용
        float wait = UnityEngine.Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(wait);

        _hookedFish = PickRandomFish();
        if (_hookedFish == null)
        {
            Debug.LogWarning("[FishingManager] 낚을 수 있는 물고기가 없음.");
        }

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
        ChangeState(FishingState.Result);
    }

    private void ChangeState(FishingState next)
    {
        _state = next;
        Debug.Log($"[FishingManager] 상태 → {next}");
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

        // ⚠️ 3번 팀원(HJH) AddItem이 public 되면 아래 주석 해제
        // PlayerInventory.Instance.AddItem(fish, 1);
        Debug.Log($"[FishingManager] 인벤토리 지급: {fish.itemName} x1 (AddItem public 대기중)");
    }
}