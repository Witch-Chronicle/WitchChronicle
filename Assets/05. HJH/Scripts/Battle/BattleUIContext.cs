using System;
using System.Collections.Generic;
using System.Linq;
using Battle.Rules;
using UnityEngine;

/// <summary>
/// BattleCycleController/BattleManager를 직접 아는 유일한 창구.
/// UI 쪽 컨트롤러들은 이 스크립트(BattleUIContext.Instance)만 참조하면 되고,
/// BattleCycleController/BattleManager를 직접 몰라도 됨.
/// </summary>
public class BattleUIContext : MonoBehaviour
{
    public static BattleUIContext Instance { get; private set; }

    [SerializeField] private BattleCycleController _battleCycleController;
    [SerializeField] private BattleManager _battleManager;

    [Header("Status Effect (아이콘 조회용, StatusEffectData.Icon 사용)")]
    [SerializeField] private Battle.Rules.StatusEffectDatabase _statusEffectDatabase;

    public BattleUnit CurrentUnit { get; private set; }
    public IReadOnlyList<BattleUnit> PartyUnits { get; private set; } = new List<BattleUnit>();

    public event Action<BattleUnit> OnTurnStarted;
    public event Action<BattleUnit> OnTurnEnded;
    public event Action OnBattleStarted;
    public event Action<BattleTeamType> OnBattleEnded;

    /// <summary>
    /// 상태이상이 유닛에게 부여/해제될 때 중계. UI(BattleCharacterStatusView, EnemyTargetOverlay 등)는
    /// BattleCycleController를 몰라도 이걸로 구독 가능.
    /// </summary>
    public event Action<BattleUnit, StatusEffectType> OnStatusApplied;
    public event Action<BattleUnit, StatusEffectType> OnStatusRemoved;

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
        if (_battleCycleController == null) return;

        _battleCycleController.OnBattleStarted += HandleBattleStarted;
        _battleCycleController.OnTurnStarted += HandleTurnStarted;
        _battleCycleController.OnTurnEnded += HandleTurnEnded;
        _battleCycleController.OnBattleEnded += HandleBattleEnded;
        _battleCycleController.OnStatusApplied += HandleStatusApplied;
        _battleCycleController.OnStatusRemoved += HandleStatusRemoved;
    }

    private void OnDisable()
    {
        if (_battleCycleController == null) return;

        _battleCycleController.OnBattleStarted -= HandleBattleStarted;
        _battleCycleController.OnTurnStarted -= HandleTurnStarted;
        _battleCycleController.OnTurnEnded -= HandleTurnEnded;
        _battleCycleController.OnBattleEnded -= HandleBattleEnded;
        _battleCycleController.OnStatusApplied -= HandleStatusApplied;
        _battleCycleController.OnStatusRemoved -= HandleStatusRemoved;
    }

    private void HandleBattleStarted()
    {
        if (_battleManager != null)
        {
            PartyUnits = _battleManager.ActiveBattleUnits
                .Where(unit => unit != null && unit.TeamType == BattleTeamType.Player)
                .ToList();
        }

        OnBattleStarted?.Invoke();
    }

    private void HandleTurnStarted(BattleUnit unit, int actionCount)
    {
        CurrentUnit = unit;
        OnTurnStarted?.Invoke(unit);
    }

    private void HandleTurnEnded(BattleUnit unit)
    {
        OnTurnEnded?.Invoke(unit);
    }

    private void HandleBattleEnded(BattleTeamType winner)
    {
        Debug.Log($"[BattleUIContext] HandleBattleEnded 호출됨: {winner}");   // 임시

        CurrentUnit = null;
        OnBattleEnded?.Invoke(winner);
    }

    // BattleUIContext에 창구 추가
    public void ForceEndBattle(BattleTeamType winner)
    {
        if (_battleCycleController == null) return;
        _battleCycleController.ForceEndBattle(winner);
    }

    /// <summary>
    /// 공격/스킬 등 행동 요청을 전투 시스템에 제출.
    /// </summary>
    public void SubmitAction(BattleActionRequest request)
    {
        if (_battleCycleController == null || request == null) return;

        _battleCycleController.SubmitAction(request);
    }

    /// <summary>
    /// 침묵 등으로 이 유닛이 스킬을 사용할 수 있는지 여부(스킬 버튼 잠금 판단용).
    /// </summary>
    public bool CanUseSkill(BattleUnit unit)
    {
        if (_battleCycleController == null || unit == null) return true;

        return _battleCycleController.CanUseSkill(unit);
    }

    /// <summary>
    /// 포션 사용(HP/MP 회복·상태이상 해제). 배틀의 실제 실행부로 위임한다.
    /// </summary>
    public BattleItemResult UsePotion(BattleUnit user, PotionItemData potion)
    {
        if (_battleCycleController == null) return default;

        return _battleCycleController.UsePotion(user, potion);
    }

    /// <summary>
    /// actor 기준 생존한 상대 목록 조회 (기본 공격용).
    /// </summary>
    public void GetAliveOpponents(BattleUnit actor, List<BattleUnit> targets)
    {
        if (_battleCycleController == null) return;

        _battleCycleController.GetAliveOpponents(actor, targets);
    }

    /// <summary>
    /// 이 스킬이 대상 선택 UI를 열어야 하는지 여부. false면 대상 없이 바로 제출해야 함.
    /// </summary>
    public bool DoesSkillRequireTargetSelection(SkillData skillData)
    {
        if (_battleCycleController == null || skillData == null) return true;

        return _battleCycleController.DoesSkillRequireTargetSelection(skillData);
    }

    /// <summary>
    /// 스킬의 TargetType에 맞는 선택 가능 대상 목록 조회 (아군 힐 스킬이면 아군, 공격 스킬이면 적 등).
    /// </summary>
    public void GetSelectableSkillTargets(BattleUnit actor, SkillData skillData, List<BattleUnit> targets)
    {
        if (_battleCycleController == null) return;

        _battleCycleController.GetSelectableSkillTargets(actor, skillData, targets);
    }

    /// <summary>
    /// 이번 라운드 전체 턴 순서(아군+적 섞여있음)를 복사해서 반환.
    /// </summary>
    public void GetCurrentTurnOrder(List<BattleUnit> result, bool includeDead = true)
    {
        if (_battleCycleController == null) return;

        _battleCycleController.GetCurrentTurnOrder(result, includeDead);
    }

    /// <summary>
    /// 이번 라운드 턴 순서 리스트 상에서 지금 행동 중인 유닛의 인덱스.
    /// </summary>
    public int GetCurrentTurnOrderIndex()
    {
        return _battleCycleController != null ? _battleCycleController.GetCurrentTurnOrderIndex() : -1;
    }

    /// <summary>
    /// BattleUnit에 대응하는 BattleActor 검색 (아웃라인 표시 등에서 사용).
    /// </summary>
    public bool TryGetActor(BattleUnit unit, out BattleActor actor)
    {
        actor = null;

        if (_battleManager == null || unit == null)
        {
            return false;
        }

        return _battleManager.TryGetActor(unit, out actor);
    }

    private void HandleStatusApplied(BattleUnit unit, StatusEffectType type)
    {
        OnStatusApplied?.Invoke(unit, type);
    }

    private void HandleStatusRemoved(BattleUnit unit, StatusEffectType type)
    {
        OnStatusRemoved?.Invoke(unit, type);
    }

    /// <summary>
    /// 상태이상 종류에 해당하는 아이콘 스프라이트 조회. 데이터베이스 미설정/데이터 없으면 null.
    /// </summary>
    public Sprite GetStatusIcon(StatusEffectType type)
    {
        if (_statusEffectDatabase == null) return null;

        Battle.Rules.StatusEffectData data = _statusEffectDatabase.GetData(type);

        return data != null ? data.Icon : null;
    }
}