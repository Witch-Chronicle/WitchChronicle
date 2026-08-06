using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 공격 VFX 재생
/// 투사체 생성, 이동, 충돌 시점 전달
/// </summary>
public class ConstellationPathVfxPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager _battleManager;

    [Header("Cleanup")]
    [Tooltip("남아 있는 VFX 자동 제거 시간")]
    [SerializeField, Min(0.1f)] private float _vfxLifetime = 5f;

    private readonly List<GameObject> _spawnedVfx = new List<GameObject>();
    private readonly List<Coroutine> _runningRoutines = new List<Coroutine>();

    /// <summary>
    /// 내부 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_battleManager == null)
        {
            _battleManager = FindFirstObjectByType<BattleManager>();
        }
    }

    /// <summary>
    /// 비활성화 시 진행 중인 VFX 정리
    /// </summary>
    private void OnDisable()
    {
        StopAllVfx();
    }

    /// <summary>
    /// 직선 투사체 한 발 재생
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <param name="onImpact">대상 도착 콜백</param>
    /// <param name="onComplete">투사체 연출 완료 콜백</param>
    public void PlayStraightProjectile(
        BattleUnit attacker,
        BattleUnit target,
        ConstellationPathAttackData attackData,
        Action onImpact = null,
        Action onComplete = null)
    {
        if (!ValidatePlayRequest(attacker, target, attackData))
        {
            onImpact?.Invoke();
            onComplete?.Invoke();
            return;
        }

        Coroutine routine = StartCoroutine(
            PlayStraightProjectileRoutine(attacker, target, attackData, onImpact, onComplete));

        _runningRoutines.Add(routine);
    }

    /// <summary>
    /// 직선 투사체 이동 진행
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <param name="onImpact">대상 도착 콜백</param>
    /// <param name="onComplete">투사체 연출 완료 콜백</param>
    private IEnumerator PlayStraightProjectileRoutine(
        BattleUnit attacker,
        BattleUnit target,
        ConstellationPathAttackData attackData,
        Action onImpact,
        Action onComplete)
    {
        TryGetActorTransform(attacker, out Transform attackerTransform);
        TryGetActorTransform(target, out Transform targetTransform);

        Vector3 startPosition = GetSpawnPosition(attackerTransform, attackData);
        Vector3 targetPosition = GetTargetPosition(targetTransform, attackData);

        GameObject projectile = SpawnProjectile(attackData, startPosition, targetPosition);

        float elapsedTime = 0f;
        float travelDuration = Mathf.Max(0.01f, attackData.TravelDuration);

        while (elapsedTime < travelDuration)
        {
            if (targetTransform == null)
            {
                break;
            }

            elapsedTime += Time.deltaTime;
            targetPosition = GetTargetPosition(targetTransform, attackData);

            float progress = Mathf.Clamp01(elapsedTime / travelDuration);

            if (projectile != null)
            {
                projectile.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
                RotateToward(projectile.transform, targetPosition);
            }

            yield return null;
        }

        if (projectile != null)
        {
            projectile.transform.position = targetPosition;
            RemoveVfx(projectile);
        }

        SpawnHitVfx(attackData, targetPosition);

        onImpact?.Invoke();
        onComplete?.Invoke();
    }

    /// <summary>
    /// 투사체 생성
    /// </summary>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <param name="startPosition">생성 위치</param>
    /// <param name="targetPosition">초기 대상 위치</param>
    /// <returns>생성 투사체</returns>
    private GameObject SpawnProjectile(
        ConstellationPathAttackData attackData,
        Vector3 startPosition,
        Vector3 targetPosition)
    {
        if (attackData.ProjectileVfxPrefab == null)
        {
            return null;
        }

        Vector3 direction = targetPosition - startPosition;
        Quaternion rotation = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized)
            : Quaternion.identity;

        GameObject projectile = Instantiate(
            attackData.ProjectileVfxPrefab,
            startPosition,
            rotation);

        projectile.transform.localScale *= attackData.ProjectileScale;

        _spawnedVfx.Add(projectile);

        return projectile;
    }

    /// <summary>
    /// 명중 VFX 생성
    /// </summary>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <param name="position">생성 위치</param>
    private void SpawnHitVfx(ConstellationPathAttackData attackData, Vector3 position)
    {
        if (attackData.HitVfxPrefab == null)
        {
            return;
        }

        GameObject hitVfx = Instantiate(
            attackData.HitVfxPrefab,
            position,
            Quaternion.identity);

        _spawnedVfx.Add(hitVfx);
        StartCoroutine(RemoveAfterDelay(hitVfx, _vfxLifetime));
    }

    /// <summary>
    /// 공격자 기준 투사체 생성 위치 반환
    /// </summary>
    /// <param name="attackerTransform">공격자 Transform</param>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <returns>생성 위치</returns>
    private Vector3 GetSpawnPosition(
        Transform attackerTransform,
        ConstellationPathAttackData attackData)
    {
        if (attackerTransform == null)
        {
            return attackData.SpawnOffset;
        }

        return attackerTransform.TransformPoint(attackData.SpawnOffset);
    }

    /// <summary>
    /// 대상 기준 충돌 위치 반환
    /// </summary>
    /// <param name="targetTransform">대상 Transform</param>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <returns>충돌 위치</returns>
    private Vector3 GetTargetPosition(
        Transform targetTransform,
        ConstellationPathAttackData attackData)
    {
        if (targetTransform == null)
        {
            return attackData.TargetOffset;
        }

        return targetTransform.TransformPoint(attackData.TargetOffset);
    }

    /// <summary>
    /// 투사체 진행 방향 회전
    /// </summary>
    /// <param name="projectileTransform">투사체 Transform</param>
    /// <param name="targetPosition">대상 위치</param>
    private void RotateToward(Transform projectileTransform, Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - projectileTransform.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        projectileTransform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    /// <summary>
    /// 유닛 기준 액터 Transform 검색
    /// </summary>
    /// <param name="unit">검색 유닛</param>
    /// <param name="actorTransform">검색 결과</param>
    /// <returns>검색 성공 여부</returns>
    private bool TryGetActorTransform(BattleUnit unit, out Transform actorTransform)
    {
        actorTransform = null;

        if (unit == null || _battleManager == null)
        {
            return false;
        }

        if (!_battleManager.TryGetActor(unit, out BattleActor actor) || actor == null)
        {
            return false;
        }

        actorTransform = actor.transform;
        return true;
    }

    /// <summary>
    /// 투사체 재생 요청 유효성 검사
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <returns>유효 여부</returns>
    private bool ValidatePlayRequest(
        BattleUnit attacker,
        BattleUnit target,
        ConstellationPathAttackData attackData)
    {
        if (attacker == null || target == null || attackData == null)
        {
            return false;
        }

        if (!TryGetActorTransform(attacker, out _) || !TryGetActorTransform(target, out _))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 지정 시간 후 VFX 제거
    /// </summary>
    /// <param name="vfx">제거 대상</param>
    /// <param name="delay">대기 시간</param>
    private IEnumerator RemoveAfterDelay(GameObject vfx, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        RemoveVfx(vfx);
    }

    /// <summary>
    /// 단일 VFX 제거
    /// </summary>
    /// <param name="vfx">제거 대상</param>
    private void RemoveVfx(GameObject vfx)
    {
        if (vfx == null)
        {
            return;
        }

        _spawnedVfx.Remove(vfx);
        Destroy(vfx);
    }

    /// <summary>
    /// 진행 중인 모든 별자리 VFX 중단
    /// </summary>
    public void StopAllVfx()
    {
        for (int i = 0; i < _runningRoutines.Count; i++)
        {
            if (_runningRoutines[i] != null)
            {
                StopCoroutine(_runningRoutines[i]);
            }
        }

        _runningRoutines.Clear();

        for (int i = _spawnedVfx.Count - 1; i >= 0; i--)
        {
            if (_spawnedVfx[i] != null)
            {
                Destroy(_spawnedVfx[i]);
            }
        }

        _spawnedVfx.Clear();
    }
}