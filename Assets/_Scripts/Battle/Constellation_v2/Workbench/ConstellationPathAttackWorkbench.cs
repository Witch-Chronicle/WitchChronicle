using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 공격 전체 진입 흐름 독립 테스트
/// </summary>
public class ConstellationPathAttackWorkbench : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private ConstellationPathAttackController _attackController;
    [SerializeField] private SkillData _skillData;
    [SerializeField] private BattleCycleController _battleCycleController;

    [Header("Damage Test")]
    [SerializeField, Min(1)] private int _testUnitDamage = 13;

    private BattleUnit _testAttacker;

    /// <summary>
    /// 내부 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_battleManager == null)
        {
            _battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (_attackController == null)
        {
            _attackController = GetComponentInParent<ConstellationPathAttackController>();
        }

        if (_battleCycleController == null)
        {
            _battleCycleController = FindAnyObjectByType<BattleCycleController>();
        } 
    }

    /// <summary>
    /// 별자리 공격 전체 진입 흐름 테스트
    /// </summary>
    [ContextMenu("Play Attack Phase Test")]
    public void PlayAttackPhaseTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ConstellationPath] 플레이 모드에서 테스트 필요", this);
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        if (!TryFindTestUnits(out BattleUnit attacker, out BattleUnit target, out List<BattleUnit> targets))
        {
            Debug.LogWarning("[ConstellationPath] 테스트용 적 또는 플레이어를 찾지 못함", this);
            return;
        }

        _testAttacker = attacker;

        bool isStarted = _attackController.StartAttackPhase(
            attacker,
            target,
            targets,
            _skillData,
            HandleAttackPhaseCompleted);

        if (!isStarted)
        {
            Debug.LogWarning("[ConstellationPath] 공격 단계 테스트 시작 실패", this);
        }
    }

    /// <summary>
    /// 테스트 공격자와 플레이어 대상 목록 검색
    /// </summary>
    /// <param name="attacker">첫 번째 생존 적</param>
    /// <param name="target">대표 플레이어 대상</param>
    /// <param name="targets">생존 플레이어 전체</param>
    /// <returns>검색 성공 여부</returns>
    private bool TryFindTestUnits(
        out BattleUnit attacker,
        out BattleUnit target,
        out List<BattleUnit> targets)
    {
        attacker = null;
        target = null;
        targets = new List<BattleUnit>();

        if (_battleManager == null || _battleManager.SpawnedActors == null)
        {
            return false;
        }

        for (int i = 0; i < _battleManager.SpawnedActors.Count; i++)
        {
            BattleActor actor = _battleManager.SpawnedActors[i];

            if (actor == null || !actor.HasBattleUnit || actor.BattleUnit == null || !actor.BattleUnit.IsAlive)
            {
                continue;
            }

            if (actor.TeamType == BattleTeamType.Enemy && attacker == null)
            {
                attacker = actor.BattleUnit;
            }

            if (actor.TeamType == BattleTeamType.Player)
            {
                targets.Add(actor.BattleUnit);

                if (target == null)
                {
                    target = actor.BattleUnit;
                }
            }
        }

        return attacker != null && target != null && targets.Count > 0;
    }

    /// <summary>
    /// 테스트 참조 유효성 검사
    /// </summary>
    /// <returns>유효 여부</returns>
    private bool ValidateReferences()
    {
        if (_battleManager == null)
        {
            Debug.LogWarning("[ConstellationPath] BattleManager 참조 없음", this);
            return false;
        }

        if (_attackController == null)
        {
            Debug.LogWarning("[ConstellationPath] AttackController 참조 없음", this);
            return false;
        }

        if (_skillData == null)
        {
            Debug.LogWarning("[ConstellationPath] SkillData 참조 없음", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 별자리 공격 입력 결과 및 대상별 방어 횟수 출력
    /// </summary>
    /// <param name="result">별자리 결과</param>
    private void HandleAttackPhaseCompleted(ConstellationPathResult result)
    {
        Debug.Log(
            $"[ConstellationPath] 공격 단계 완료\n" +
            $"Nodes: {result.CompletedNodeCount}/{result.TotalNodeCount}\n" +
            $"Full Success: {result.IsSuccess}",
            this);

        LogShieldState();
        LogHitSchedule();
        LogDamageDistribution();
        ApplyHitScheduleDamage();
    }

    /// <summary>
    /// 플레이어별 남은 별자리 방어 횟수 출력
    /// </summary>
    private void LogShieldState()
    {
        if (_battleManager == null || _battleManager.SpawnedActors == null)
        {
            return;
        }

        for (int i = 0; i < _battleManager.SpawnedActors.Count; i++)
        {
            BattleActor actor = _battleManager.SpawnedActors[i];

            if (actor == null || !actor.HasBattleUnit || actor.BattleUnit == null)
            {
                continue;
            }

            if (actor.TeamType != BattleTeamType.Player)
            {
                continue;
            }

            int remainingCharge = _attackController.ShieldState.GetRemainingCharge(actor.BattleUnit);

            Debug.Log(
                $"[ConstellationPath] Shield | {actor.name} | Charge: {remainingCharge}",
                this);
        }
    }

    /// <summary>
    /// 생성된 별자리 공격 스케줄 출력
    /// </summary>
    private void LogHitSchedule()
    {
        IReadOnlyList<ConstellationPathHitEntry> schedule = _attackController.HitSchedule;

        Debug.Log($"[ConstellationPath] HitSchedule Count: {schedule.Count}", this);

        for (int i = 0; i < schedule.Count; i++)
        {
            ConstellationPathHitEntry entry = schedule[i];

            Debug.Log(
                $"[ConstellationPath] Hit {i + 1} | " +
                $"Round: {entry.RoundIndex} | " +
                $"Sequence: {entry.SequenceIndex}",
                this);
        }
    }

    /// <summary>
    /// 공격 스케줄 방어 및 데미지 분배 판정 테스트
    /// </summary>
    private void SimulateHitSchedule()
    {
        IReadOnlyList<ConstellationPathHitEntry> schedule = _attackController.HitSchedule;
        ConstellationPathAttackData attackData = _skillData.ConstellationPathAttackData;

        for (int i = 0; i < schedule.Count; i++)
        {
            if (!_attackController.ResolveAttackUnit(
                schedule[i],
                _testUnitDamage,
                attackData,
                out ConstellationPathHitResolution resolution))
            {
                continue;
            }

            Debug.Log(
                $"[ConstellationPath] Hit {i + 1} | " +
                $"Result: {(resolution.IsBlocked ? "BLOCK" : "HIT")} | " +
                $"Shield: {resolution.RemainingShieldCharge} | " +
                $"Damage Slices: {resolution.DamageSlices.Count}",
                this);
        }
    }

    /// <summary>
    /// 공격 단위 데미지 분배 결과 출력
    /// </summary>
    private void LogDamageDistribution()
    {
        if (_skillData == null || _skillData.ConstellationPathAttackData == null)
        {
            return;
        }

        List<ConstellationPathDamageSlice> slices = ConstellationPathDamageDistributor.Build(
            _testUnitDamage,
            _skillData.ConstellationPathAttackData);

        int totalDamage = 0;

        Debug.Log(
            $"[ConstellationPath] Damage Delivery: {_skillData.ConstellationPathAttackData.DamageDeliveryType} | " +
            $"Unit Damage: {_testUnitDamage} | Slice Count: {slices.Count}",
            this);

        for (int i = 0; i < slices.Count; i++)
        {
            ConstellationPathDamageSlice slice = slices[i];
            totalDamage += slice.Damage;

            Debug.Log(
                $"[ConstellationPath] Damage Slice {slice.TickIndex + 1}/{slice.TickCount} | Damage: {slice.Damage}",
                this);
        }

        Debug.Log($"[ConstellationPath] Damage Slice Total: {totalDamage}", this);
    }

    /// <summary>
    /// 생성된 공격 스케줄 실제 데미지 적용 테스트 시작
    /// </summary>
    private void ApplyHitScheduleDamage()
    {
        StartCoroutine(ApplyHitScheduleDamageRoutine());
    }

    /// <summary>
    /// 공격 패턴 기준 스케줄 데미지 적용
    /// </summary>
    private IEnumerator ApplyHitScheduleDamageRoutine()
    {
        if (_battleCycleController == null || _testAttacker == null || _skillData == null) yield break;

        ConstellationPathAttackData attackData = _skillData.ConstellationPathAttackData;

        if (attackData.AttackPattern == ConstellationPathAttackPattern.AllTargetsSimultaneous)
        {
            yield return ApplySimultaneousScheduleRoutine(attackData);
            yield break;
        }

        yield return ApplySequentialScheduleRoutine(attackData);
    }

    /// <summary>
    /// 순차 공격 스케줄 처리
    /// </summary>
    /// <param name="attackData">별자리 공격 데이터</param>
    private IEnumerator ApplySequentialScheduleRoutine(ConstellationPathAttackData attackData)
    {
        IReadOnlyList<ConstellationPathHitEntry> schedule = _attackController.HitSchedule;

        for (int i = 0; i < schedule.Count; i++)
        {
            yield return ApplyAttackUnitRoutine(i, schedule[i], attackData);

            if (i < schedule.Count - 1 && attackData.LaunchInterval > 0f)
            {
                yield return new WaitForSeconds(attackData.LaunchInterval);
            }
        }
    }

    /// <summary>
    /// 동일 라운드 대상 동시 공격 처리
    /// </summary>
    /// <param name="attackData">별자리 공격 데이터</param>
    private IEnumerator ApplySimultaneousScheduleRoutine(ConstellationPathAttackData attackData)
    {
        IReadOnlyList<ConstellationPathHitEntry> schedule = _attackController.HitSchedule;

        int index = 0;

        while (index < schedule.Count)
        {
            int roundIndex = schedule[index].RoundIndex;
            int runningCount = 0;

            while (index < schedule.Count && schedule[index].RoundIndex == roundIndex)
            {
                int hitIndex = index;
                ConstellationPathHitEntry hitEntry = schedule[index];

                runningCount++;

                StartCoroutine(ApplyAttackUnitRoutine(
                    hitIndex,
                    hitEntry,
                    attackData,
                    () => runningCount--));

                index++;
            }

            while (runningCount > 0)
            {
                yield return null;
            }

            if (index < schedule.Count && attackData.LaunchInterval > 0f)
            {
                yield return new WaitForSeconds(attackData.LaunchInterval);
            }
        }
    }

    /// <summary>
    /// 단일 공격 단위 처리
    /// </summary>
    /// <param name="hitIndex">공격 인덱스</param>
    /// <param name="hitEntry">공격 정보</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <param name="onComplete">완료 콜백</param>
    private IEnumerator ApplyAttackUnitRoutine(
        int hitIndex,
        ConstellationPathHitEntry hitEntry,
        ConstellationPathAttackData attackData,
        System.Action onComplete = null)
    {
        if (hitEntry.Target == null || !hitEntry.Target.IsAlive)
        {
            onComplete?.Invoke();
            yield break;
        }

        int unitDamage = _battleCycleController.CalculateConstellationUnitDamage(
            _testAttacker,
            hitEntry.Target,
            _skillData);

        if (!_attackController.ResolveAttackUnit(
            hitEntry,
            unitDamage,
            attackData,
            out ConstellationPathHitResolution resolution))
        {
            onComplete?.Invoke();
            yield break;
        }

        if (resolution.IsBlocked)
        {
            Debug.Log(
                $"[ConstellationPath] Hit {hitIndex + 1} BLOCK | " +
                $"Round: {hitEntry.RoundIndex} | " +
                $"Shield: {resolution.RemainingShieldCharge}",
                this);

            onComplete?.Invoke();
            yield break;
        }

        yield return ApplyDamageSlicesRoutine(hitIndex, resolution, attackData);

        onComplete?.Invoke();
    }

    /// <summary>
    /// 단일 공격 단위 내부 데미지 조각 순차 적용
    /// </summary>
    /// <param name="hitIndex">공격 단위 인덱스</param>
    /// <param name="resolution">공격 단위 판정 결과</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    private IEnumerator ApplyDamageSlicesRoutine(
        int hitIndex,
        ConstellationPathHitResolution resolution,
        ConstellationPathAttackData attackData)
    {
        int hpBefore = resolution.Target.CurrentHp;

        for (int i = 0; i < resolution.DamageSlices.Count; i++)
        {
            if (!resolution.Target.IsAlive) break;

            ConstellationPathDamageSlice slice = resolution.DamageSlices[i];

            _battleCycleController.ApplyConstellationDamageSlice(
                resolution.Target,
                slice.Damage,
                i == 0);

            Debug.Log(
                $"[ConstellationPath] Hit {hitIndex + 1} | " +
                $"Tick {slice.TickIndex + 1}/{slice.TickCount} | " +
                $"Damage: {slice.Damage} | " +
                $"HP: {resolution.Target.CurrentHp}",
                this);

            if (i < resolution.DamageSlices.Count - 1 &&
                attackData.DamageDeliveryType == ConstellationPathDamageDeliveryType.Tick &&
                attackData.TickInterval > 0f)
            {
                yield return new WaitForSeconds(attackData.TickInterval);
            }
        }

        Debug.Log(
            $"[ConstellationPath] Hit {hitIndex + 1} DAMAGE COMPLETE | " +
            $"HP: {hpBefore} → {resolution.Target.CurrentHp}",
            this);
    }
}