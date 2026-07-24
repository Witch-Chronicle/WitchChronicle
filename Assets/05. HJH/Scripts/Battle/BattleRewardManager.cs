using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterRewardResult
{
    public string CharacterName;
    public int ExpGained;
    public int LevelBefore;
    public int LevelAfter;
    public int CurrentExp;    // 지급 후, 현재 레벨 내에서의 진행 경험치
    public int RequiredExp;   // 지급 후, 다음 레벨업까지 필요한 경험치

    public bool DidLevelUp => LevelAfter > LevelBefore;
}


/// <summary>
/// 전투 종료 시(승패 무관) BattleManager에 스폰된 적 Actor들을 훑어서
/// 사망한 적의 보상(Exp, Gold, Item)을 합산 후 파티/인벤토리에 지급.
/// </summary>
[RequireComponent(typeof(BattleManager))]
public class BattleRewardManager : MonoBehaviour
{
    [SerializeField] private BattleManager _battleManager;

    private bool _isSubscribed;

    /// <summary>
    /// 보상 계산/지급이 끝나면 호출됨. (총 골드, 캐릭터별 결과 목록, 획득 아이템 목록)
    /// </summary>
    public event Action<int, List<CharacterRewardResult>, List<DropResult>> OnRewardsCalculated;

    private void Awake()
    {
        if (_battleManager == null)
        {
            _battleManager = GetComponent<BattleManager>();
        }
    }

    private void Start()
    {
        SubscribeToBattleContext();
    }

    private void OnDisable()
    {
        UnsubscribeFromBattleContext();
    }

    private void SubscribeToBattleContext()
    {
        if (_isSubscribed)
        {
            return;
        }

        if (BattleUIContext.Instance == null)
        {
            Debug.LogWarning("[BattleRewardManager] BattleUIContext.Instance가 null입니다.");
            return;
        }

        BattleUIContext.Instance.OnBattleEnded += HandleBattleEnded;
        _isSubscribed = true;
    }

    private void UnsubscribeFromBattleContext()
    {
        if (_isSubscribed == false || BattleUIContext.Instance == null)
        {
            _isSubscribed = false;
            return;
        }

        BattleUIContext.Instance.OnBattleEnded -= HandleBattleEnded;
        _isSubscribed = false;
    }

    /// <summary>
    /// 전투 종료 시 사망한 적을 기준으로 Exp/Gold/Item 계산 후 지급, 결과를 이벤트로 공개.
    /// </summary>
    private void HandleBattleEnded(BattleTeamType winner)
    {
        if (_battleManager == null)
        {
            Debug.LogWarning("[BattleRewardManager] BattleManager 참조가 없습니다.");
            return;
        }

        List<BattleActor> deadEnemyActors = GetDeadEnemyActors();

        int accumulatedExp = CalculateAccumulatedExp(deadEnemyActors);
        int accumulatedGold = CalculateAccumulatedGold(deadEnemyActors);

        List<DropResult> drops = RollAndApplyDrops(deadEnemyActors);

        List<CharacterRewardResult> results = ApplyExpAndGoldAndBuildResults(accumulatedExp, accumulatedGold);

        Debug.Log($"[BattleRewardManager] 보상 지급 완료. Exp: {accumulatedExp}, Gold: {accumulatedGold}, Item 종류: {drops.Count}");

        OnRewardsCalculated?.Invoke(accumulatedGold, results, drops);
    }

    private List<BattleActor> GetDeadEnemyActors()
    {
        List<BattleActor> enemyActors = new List<BattleActor>();
        _battleManager.GetEnemyActors(enemyActors);

        List<BattleActor> deadActors = new List<BattleActor>();

        for (int i = 0; i < enemyActors.Count; i++)
        {
            BattleActor actor = enemyActors[i];

            if (actor == null || actor.HasBattleUnit == false || actor.BattleUnit.IsAlive)
            {
                continue;
            }

            deadActors.Add(actor);
        }

        return deadActors;
    }

    private int CalculateAccumulatedExp(List<BattleActor> deadEnemyActors)
    {
        int total = 0;

        for (int i = 0; i < deadEnemyActors.Count; i++)
        {
            EnemyBattleData enemyData = deadEnemyActors[i].EnemyBattleData;

            if (enemyData == null)
            {
                continue;
            }

            total += enemyData.ExpReward;
        }

        return total;
    }

    private int CalculateAccumulatedGold(List<BattleActor> deadEnemyActors)
    {
        int total = 0;

        for (int i = 0; i < deadEnemyActors.Count; i++)
        {
            EnemyBattleData enemyData = deadEnemyActors[i].EnemyBattleData;

            if (enemyData == null)
            {
                continue;
            }

            total += enemyData.GoldReward;
        }

        return total;
    }

    /// <summary>
    /// 사망한 적들의 드롭 테이블을 굴려서 아이템을 인벤토리에 지급하고, 같은 아이템끼리는 합산해서 반환.
    /// </summary>
    private List<DropResult> RollAndApplyDrops(List<BattleActor> deadEnemyActors)
    {
        Dictionary<ItemData, int> aggregated = new Dictionary<ItemData, int>();

        if (DropManager.Instance == null)
        {
            Debug.LogWarning("[BattleRewardManager] DropManager.Instance가 없습니다.");
            return new List<DropResult>();
        }

        for (int i = 0; i < deadEnemyActors.Count; i++)
        {
            EnemyBattleData enemyData = deadEnemyActors[i].EnemyBattleData;

            if (enemyData == null || enemyData.DropTable == null)
            {
                continue;
            }

            List<DropResult> drops = DropManager.Instance.RollDrop(enemyData.DropTable);

            for (int j = 0; j < drops.Count; j++)
            {
                DropResult drop = drops[j];

                if (drop.item == null)
                {
                    continue;
                }

                if (aggregated.ContainsKey(drop.item))
                {
                    aggregated[drop.item] += drop.amount;
                }
                else
                {
                    aggregated[drop.item] = drop.amount;
                }
            }
        }

        List<DropResult> results = new List<DropResult>();

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[BattleRewardManager] PlayerInventory.Instance가 없습니다.");
            return results;
        }

        foreach (KeyValuePair<ItemData, int> pair in aggregated)
        {
            PlayerInventory.Instance.AddItem(pair.Key, pair.Value);
            results.Add(new DropResult(pair.Key, pair.Value));
        }

        return results;
    }

    /// <summary>
    /// 파티 전원의 레벨/Exp를 지급 전/후로 스냅샷해서 캐릭터별 결과를 생성.
    /// 실제 Exp/Gold 지급도 이 메서드 안에서 수행.
    /// </summary>
    private List<CharacterRewardResult> ApplyExpAndGoldAndBuildResults(int exp, int gold)
    {
        List<CharacterRewardResult> results = new List<CharacterRewardResult>();

        if (PersistentCharacterManager.Instance == null)
        {
            return results;
        }

        List<PersistentCharacterUnit> activeParty = new List<PersistentCharacterUnit>();
        PersistentCharacterManager.Instance.GetActivePartyMembers(activeParty);

        List<(string name, int level)> before = new List<(string, int)>();

        for (int i = 0; i < activeParty.Count; i++)
        {
            PersistentCharacterUnit unit = activeParty[i];

            if (unit == null || unit.StatController == null)
            {
                before.Add((unit != null ? unit.CharacterName : "?", 0));
                continue;
            }

            before.Add((unit.CharacterName, unit.StatController.Level));
        }

        if (exp > 0)
        {
            PersistentCharacterManager.Instance.AddExpToActiveParty(exp);
        }

        if (gold > 0 && PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddGold(gold);
        }

        for (int i = 0; i < activeParty.Count; i++)
        {
            PersistentCharacterUnit unit = activeParty[i];

            if (unit == null || unit.StatController == null)
            {
                results.Add(new CharacterRewardResult
                {
                    CharacterName = before[i].name,
                    ExpGained = exp,
                    LevelBefore = before[i].level,
                    LevelAfter = before[i].level,
                    CurrentExp = 0,
                    RequiredExp = 0
                });
                continue;
            }

            results.Add(new CharacterRewardResult
            {
                CharacterName = before[i].name,
                ExpGained = exp,
                LevelBefore = before[i].level,
                LevelAfter = unit.StatController.Level,
                CurrentExp = unit.StatController.Exp,
                RequiredExp = unit.StatController.ExpToNextLevel
            });
        }

        return results;
    }
}