using UnityEngine;

/// <summary>
/// 별자리 공격 VFX 독립 테스트
/// 전투 중 첫 번째 적에서 첫 번째 플레이어로 투사체 재생
/// </summary>
public class ConstellationPathVfxWorkbench : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private ConstellationPathVfxPlayer _vfxPlayer;
    [SerializeField] private ConstellationPathAttackData _attackData;

    /// <summary>
    /// 내부 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_battleManager == null)
        {
            _battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (_vfxPlayer == null)
        {
            _vfxPlayer = GetComponentInParent<ConstellationPathVfxPlayer>();
        }
    }

    /// <summary>
    /// 직선 투사체 테스트 재생
    /// </summary>
    [ContextMenu("Play Test Projectile")]
    public void PlayTestProjectile()
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

        if (!TryFindTestUnits(out BattleUnit attacker, out BattleUnit target))
        {
            Debug.LogWarning("[ConstellationPath] 테스트용 적 또는 플레이어를 찾지 못함", this);
            return;
        }

        _vfxPlayer.PlayStraightProjectile(
            attacker,
            target,
            _attackData,
            HandleProjectileImpact,
            HandleProjectileCompleted);
    }

    /// <summary>
    /// 테스트용 공격자와 대상 검색
    /// </summary>
    /// <param name="attacker">첫 번째 생존 적</param>
    /// <param name="target">첫 번째 생존 플레이어</param>
    /// <returns>검색 성공 여부</returns>
    private bool TryFindTestUnits(out BattleUnit attacker, out BattleUnit target)
    {
        attacker = null;
        target = null;

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
            else if (actor.TeamType == BattleTeamType.Player && target == null)
            {
                target = actor.BattleUnit;
            }

            if (attacker != null && target != null)
            {
                return true;
            }
        }

        return false;
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

        if (_vfxPlayer == null)
        {
            Debug.LogWarning("[ConstellationPath] VfxPlayer 참조 없음", this);
            return false;
        }

        if (_attackData == null)
        {
            Debug.LogWarning("[ConstellationPath] AttackData 참조 없음", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 투사체 도착 로그 출력
    /// </summary>
    private void HandleProjectileImpact()
    {
        Debug.Log("[ConstellationPath] 테스트 투사체 도착", this);
    }

    /// <summary>
    /// 투사체 연출 완료 로그 출력
    /// </summary>
    private void HandleProjectileCompleted()
    {
        Debug.Log("[ConstellationPath] 테스트 투사체 완료", this);
    }
}