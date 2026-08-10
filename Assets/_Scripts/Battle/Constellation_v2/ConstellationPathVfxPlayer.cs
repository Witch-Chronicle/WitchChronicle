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
    private readonly Dictionary<BattleUnit, Transform> _targetAnchors = new Dictionary<BattleUnit, Transform>();

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
    /// 곡사 투사체 한 발 재생
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <param name="onImpact">대상 도착 콜백</param>
    /// <param name="onComplete">투사체 연출 완료 콜백</param>
    public void PlayArcProjectile(
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
            PlayArcProjectileRoutine(
                attacker,
                target,
                attackData,
                onImpact,
                onComplete));

        _runningRoutines.Add(routine);
    }

    /// <summary>
    /// 메테오 투사체 한 발 재생
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <param name="onImpact">대상 도착 콜백</param>
    /// <param name="onComplete">투사체 연출 완료 콜백</param>
    public void PlayMeteorProjectile(
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
            PlayMeteorProjectileRoutine(
                target,
                attackData,
                onImpact,
                onComplete));

        _runningRoutines.Add(routine);
    }

    /// <summary>
    /// 시간 기반 VFX 공격 한 번 재생
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <param name="onImpact">공격 판정 시점 콜백</param>
    /// <param name="onComplete">VFX 종료 콜백</param>
    public void PlayTimedVfx(
        BattleUnit attacker,
        BattleUnit target,
        ConstellationPathAttackData attackData,
        Action onImpact = null,
        Action onComplete = null)
    {
        if (!ValidatePlayRequest(attacker, target, attackData) || attackData.TimedVfxPrefab == null)
        {
            onImpact?.Invoke();
            onComplete?.Invoke();
            return;
        }

        Coroutine routine = StartCoroutine(
            PlayTimedVfxRoutine(
                attacker,
                target,
                attackData,
                onImpact,
                onComplete));

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
        TryGetTargetTransform(target, out Transform targetTransform);

        Vector3 startPosition = GetSpawnPosition(attackerTransform, attackData);
        Vector3 targetPosition = GetTargetPosition(targetTransform, attackData);

        GameObject projectile = SpawnProjectile(attackData, startPosition, targetPosition);

        float elapsedTime = 0f;
        float travelDuration = Mathf.Max(0.01f, attackData.TravelDuration);

        while (elapsedTime < travelDuration)
        {
            if (TryGetTargetTransform(
                target,
                out Transform resolvedTargetTransform))
            {
                targetTransform = resolvedTargetTransform;

                targetPosition = GetTargetPosition(
                    targetTransform,
                    attackData);
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

        onImpact?.Invoke();
        onComplete?.Invoke();
    }

    /// <summary>
    /// 곡사 투사체 이동 진행
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <param name="onImpact">대상 도착 콜백</param>
    /// <param name="onComplete">투사체 연출 완료 콜백</param>
    private IEnumerator PlayArcProjectileRoutine(
        BattleUnit attacker,
        BattleUnit target,
        ConstellationPathAttackData attackData,
        Action onImpact,
        Action onComplete)
    {
        TryGetActorTransform(attacker, out Transform attackerTransform);
        TryGetTargetTransform(target, out Transform targetTransform);

        Vector3 startPosition = GetSpawnPosition(
            attackerTransform,
            attackData);

        Vector3 targetPosition = GetTargetPosition(
            targetTransform,
            attackData);

        Vector3 controlPoint = CreateArcControlPoint(
            startPosition,
            attackData);

        GameObject projectile = SpawnProjectile(
            attackData,
            startPosition,
            targetPosition);

        float elapsedTime = 0f;
        float travelDuration = Mathf.Max(
            0.01f,
            attackData.TravelDuration);

        Vector3 previousPosition = startPosition;

        while (elapsedTime < travelDuration)
        {
            elapsedTime += Time.deltaTime;

            if (TryGetTargetTransform(
                target,
                out Transform resolvedTargetTransform))
            {
                targetTransform = resolvedTargetTransform;

                targetPosition = GetTargetPosition(
                    targetTransform,
                    attackData);
            }

            float progress = Mathf.Clamp01(
                elapsedTime / travelDuration);

            Vector3 currentPosition = CalculateQuadraticBezier(
                startPosition,
                controlPoint,
                targetPosition,
                progress);

            if (projectile != null)
            {
                projectile.transform.position =
                    currentPosition;

                Vector3 moveDirection =
                    currentPosition - previousPosition;

                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    projectile.transform.rotation =
                        Quaternion.LookRotation(
                            moveDirection.normalized);
                }
            }

            previousPosition = currentPosition;

            yield return null;
        }

        if (projectile != null)
        {
            projectile.transform.position =
                targetPosition;

            RemoveVfx(projectile);
        }

        onImpact?.Invoke();
        onComplete?.Invoke();
    }

    /// <summary>
    /// 메테오 투사체 낙하 진행
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 연출 데이터</param>
    /// <param name="onImpact">대상 도착 콜백</param>
    /// <param name="onComplete">투사체 연출 완료 콜백</param>
    private IEnumerator PlayMeteorProjectileRoutine(
        BattleUnit target,
        ConstellationPathAttackData attackData,
        Action onImpact,
        Action onComplete)
    {
        TryGetTargetTransform(target, out Transform targetTransform);

        if (targetTransform == null)
        {
            onImpact?.Invoke();
            onComplete?.Invoke();
            yield break;
        }

        Vector3 targetPosition = GetTargetPosition(
            targetTransform,
            attackData);

        Vector3 startPosition =
            targetPosition +
            Vector3.up * attackData.MeteorHeight;

        GameObject projectile = SpawnProjectile(
            attackData,
            startPosition,
            targetPosition);

        float elapsedTime = 0f;
        float travelDuration = Mathf.Max(
            0.01f,
            attackData.TravelDuration);

        while (elapsedTime < travelDuration)
        { 
            elapsedTime += Time.deltaTime;

            if (TryGetTargetTransform(
                target,
                out Transform resolvedTargetTransform))
            {
                targetTransform = resolvedTargetTransform;

                targetPosition = GetTargetPosition(
                    targetTransform,
                    attackData);
            }

            float progress = Mathf.Clamp01(
                elapsedTime / travelDuration);

            Vector3 currentPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress);

            if (projectile != null)
            {
                projectile.transform.position =
                    currentPosition;

                RotateToward(
                    projectile.transform,
                    targetPosition);
            }

            yield return null;
        }

        if (projectile != null)
        {
            projectile.transform.position =
                targetPosition;

            RemoveVfx(projectile);
        }

        onImpact?.Invoke();
        onComplete?.Invoke();
    }

    /// <summary>
    /// 시간 기반 VFX 공격 진행
    /// VFX 재생 시간에 맞춰 공격 판정 콜백 실행
    /// </summary>
    /// <param name="attacker">공격 유닛</param>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <param name="onImpact">공격 판정 시점 콜백</param>
    /// <param name="onComplete">VFX 종료 콜백</param>
    private IEnumerator PlayTimedVfxRoutine(
        BattleUnit attacker,
        BattleUnit target,
        ConstellationPathAttackData attackData,
        Action onImpact,
        Action onComplete)
    {
        TryGetActorTransform(attacker, out Transform attackerTransform);
        TryGetTargetTransform(target, out Transform targetTransform);

        Vector3 spawnPosition = GetTimedVfxPosition(
            attackerTransform,
            targetTransform,
            attackData);

        Quaternion spawnRotation = GetTimedVfxRotation(
            spawnPosition,
            targetTransform,
            attackData);

        GameObject timedVfx = Instantiate(
            attackData.TimedVfxPrefab,
            spawnPosition,
            spawnRotation);

        timedVfx.transform.localScale *= attackData.TimedVfxScale;
        _spawnedVfx.Add(timedVfx);

        float elapsedTime = 0f;
        float duration = Mathf.Max(0.01f, attackData.TimedVfxDuration);
        bool isImpactInvoked = false;

        if (attackData.TimedVfxImpactDelay <= 0f)
        {
            isImpactInvoked = true;
            onImpact?.Invoke();
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            if (TryGetTargetTransform(
                target,
                out Transform resolvedTargetTransform))
            {
                targetTransform = resolvedTargetTransform;
            }

            if (timedVfx != null &&
                attackData.TimedVfxFollowTarget &&
                targetTransform != null &&
                attackData.TimedVfxSpawnType != ConstellationPathTimedVfxSpawnType.Attacker)
            {
                timedVfx.transform.position = GetTimedVfxPosition(
                    attackerTransform,
                    targetTransform,
                    attackData);
            }

            if (timedVfx != null &&
                attackData.TimedVfxFaceTarget &&
                targetTransform != null)
            {
                Vector3 targetPosition = GetTargetPosition(
                    targetTransform,
                    attackData);

                Vector3 direction =
                    targetPosition - timedVfx.transform.position;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    timedVfx.transform.rotation =
                        Quaternion.LookRotation(direction.normalized);
                }
            }

            if (!isImpactInvoked &&
                elapsedTime >= attackData.TimedVfxImpactDelay)
            {
                isImpactInvoked = true;
                onImpact?.Invoke();
            }

            yield return null;
        }

        // 잘못된 데이터나 중간 상황에서도 대기 코루틴 정지 방지
        if (!isImpactInvoked)
        {
            onImpact?.Invoke();
        }

        if (timedVfx != null)
        {
            RemoveVfx(timedVfx);
        }

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

        PrepareProjectileForExternalMovement(projectile);

        projectile.transform.localScale *= attackData.ProjectileScale;

        _spawnedVfx.Add(projectile);

        return projectile;
    }

    /// <summary>
    /// 외부 이동 제어용 투사체 설정
    /// 에셋 자체 이동 및 물리 충돌 기능 비활성화
    /// </summary>
    /// <param name="projectile">생성된 투사체</param>
    private void PrepareProjectileForExternalMovement(
        GameObject projectile)
    {
        if (projectile == null) return;

        ProjectileMoveScript[] moveScripts =
            projectile.GetComponentsInChildren<ProjectileMoveScript>(
                true);

        for (int i = 0; i < moveScripts.Length; i++)
        {
            moveScripts[i].enabled = false;
        }

        Rigidbody[] rigidbodies =
            projectile.GetComponentsInChildren<Rigidbody>(
                true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rigidbody =
                rigidbodies[i];

            rigidbody.linearVelocity =
                Vector3.zero;

            rigidbody.angularVelocity =
                Vector3.zero;

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }

        Collider[] colliders =
            projectile.GetComponentsInChildren<Collider>(
                true);

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    /// <summary>
    /// 대상 명중 VFX 재생
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    public void PlayHitVfx(
        BattleUnit target,
        ConstellationPathAttackData attackData)
    {
        if (target == null || attackData == null) return;

        if (!TryGetActorTransform(target, out Transform targetTransform)) return;

        Vector3 position = GetTargetPosition(targetTransform, attackData);
        SpawnImpactVfx(attackData.HitVfxPrefab, position);
    }

    /// <summary>
    /// 방어막 충돌 VFX 재생
    /// </summary>
    /// <param name="target">방어 대상</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    public void PlayBlockVfx(
        BattleUnit target,
        ConstellationPathAttackData attackData)
    {
        if (target == null || attackData == null) return;

        if (!TryGetActorTransform(target, out Transform targetTransform)) return;

        Vector3 position = GetTargetPosition(targetTransform, attackData);
        SpawnImpactVfx(attackData.BlockVfxPrefab, position);
    }

    /// <summary>
    /// Tick 지속 VFX 생성
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <returns>생성된 Tick VFX</returns>
    public GameObject PlayTickVfx(
        BattleUnit target,
        ConstellationPathAttackData attackData)
    {
        if (target == null || attackData == null) return null;
        if (attackData.TickVfxPrefab == null) return null;
        if (!TryGetActorTransform(target, out Transform targetTransform)) return null;

        Vector3 position = GetTargetPosition(targetTransform, attackData);

        GameObject tickVfx = Instantiate(
            attackData.TickVfxPrefab,
            position,
            Quaternion.identity,
            targetTransform);

        _spawnedVfx.Add(tickVfx);

        return tickVfx;
    }

    /// <summary>
    /// Tick 지속 VFX 종료
    /// </summary>
    /// <param name="tickVfx">종료 대상 VFX</param>
    public void StopTickVfx(GameObject tickVfx)
    {
        RemoveVfx(tickVfx);
    }

    /// <summary>
    /// 충돌 VFX 생성
    /// </summary>
    /// <param name="prefab">생성 VFX 프리팹</param>
    /// <param name="position">생성 위치</param>
    private void SpawnImpactVfx(
        GameObject prefab,
        Vector3 position)
    {
        if (prefab == null) return;

        GameObject impactVfx = Instantiate(
            prefab,
            position,
            Quaternion.identity);

        _spawnedVfx.Add(impactVfx);
        StartCoroutine(
            RemoveAfterDelay(
                impactVfx,
                _vfxLifetime));
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
    /// 시간 기반 VFX 생성 위치 반환
    /// </summary>
    /// <param name="attackerTransform">공격자 Transform</param>
    /// <param name="targetTransform">대상 Transform</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <returns>VFX 생성 위치</returns>
    private Vector3 GetTimedVfxPosition(
        Transform attackerTransform,
        Transform targetTransform,
        ConstellationPathAttackData attackData)
    {
        switch (attackData.TimedVfxSpawnType)
        {
            case ConstellationPathTimedVfxSpawnType.Attacker:
                if (attackerTransform == null) return attackData.TimedVfxOffset;

                return attackerTransform.TransformPoint(
                    attackData.TimedVfxOffset);

            case ConstellationPathTimedVfxSpawnType.Target:
                if (targetTransform == null) return attackData.TimedVfxOffset;

                return targetTransform.TransformPoint(
                    attackData.TimedVfxOffset);

            case ConstellationPathTimedVfxSpawnType.AboveTarget:
                if (targetTransform == null)
                {
                    return Vector3.up * attackData.TimedVfxHeight +
                           attackData.TimedVfxOffset;
                }

                return targetTransform.TransformPoint(
                           attackData.TimedVfxOffset) +
                       Vector3.up * attackData.TimedVfxHeight;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 시간 기반 VFX 생성 회전 반환
    /// </summary>
    /// <param name="spawnPosition">VFX 생성 위치</param>
    /// <param name="targetTransform">대상 Transform</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <returns>VFX 생성 회전</returns>
    private Quaternion GetTimedVfxRotation(
        Vector3 spawnPosition,
        Transform targetTransform,
        ConstellationPathAttackData attackData)
    {
        if (!attackData.TimedVfxFaceTarget || targetTransform == null)
        {
            return Quaternion.identity;
        }

        Vector3 targetPosition = GetTargetPosition(
            targetTransform,
            attackData);

        Vector3 direction =
            targetPosition - spawnPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(
            direction.normalized);
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
    /// 곡사 투사체 랜덤 제어점 생성
    /// 적 위쪽 원뿔 범위에서 랜덤 방향을 선택해 곡사 경로 구성
    /// </summary>
    /// <param name="startPosition">투사체 시작 위치</param>
    /// <param name="attackData">별자리 공격 데이터</param>
    /// <returns>곡사 제어점</returns>
    private Vector3 CreateArcControlPoint(
        Vector3 startPosition,
        ConstellationPathAttackData attackData)
    {
        float halfAngle =
            attackData.ArcLaunchAngle * 0.5f;

        float maxAngleRadians =
            halfAngle * Mathf.Deg2Rad;

        float minimumVertical =
            Mathf.Cos(maxAngleRadians);

        float vertical =
            UnityEngine.Random.Range(
                minimumVertical,
                1f);

        float horizontal =
            Mathf.Sqrt(
                Mathf.Max(
                    0f,
                    1f - vertical * vertical));

        float azimuth =
            UnityEngine.Random.Range(
                0f,
                Mathf.PI * 2f);

        Vector3 randomDirection =
            new Vector3(
                Mathf.Cos(azimuth) * horizontal,
                vertical,
                Mathf.Sin(azimuth) * horizontal);

        float controlDistance =
            UnityEngine.Random.Range(
                attackData.ArcControlDistanceMin,
                attackData.ArcControlDistanceMax);

        return
            startPosition +
            randomDirection * controlDistance;
    }

    /// <summary>
    /// 2차 베지어 곡선 위치 계산
    /// </summary>
    /// <param name="startPosition">시작점</param>
    /// <param name="controlPoint">제어점</param>
    /// <param name="targetPosition">종착점</param>
    /// <param name="progress">진행도</param>
    /// <returns>곡선 위치</returns>
    private Vector3 CalculateQuadraticBezier(
        Vector3 startPosition,
        Vector3 controlPoint,
        Vector3 targetPosition,
        float progress)
    {
        float inverseProgress =
            1f - progress;

        return
            inverseProgress * inverseProgress * startPosition +
            2f * inverseProgress * progress * controlPoint +
            progress * progress * targetPosition;
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
    /// 공격 대상 Transform 반환
    /// 실제 Actor가 사라진 경우 마지막 위치 Anchor 반환
    /// </summary>
    /// <param name="unit">공격 대상 유닛</param>
    /// <param name="targetTransform">대상 또는 Anchor Transform</param>
    /// <returns>위치 반환 가능 여부</returns>
    private bool TryGetTargetTransform(
        BattleUnit unit,
        out Transform targetTransform)
    {
        targetTransform = null;

        if (unit == null)
        {
            return false;
        }

        if (TryGetActorTransform(unit, out Transform actorTransform) &&
            actorTransform != null)
        {
            if (!_targetAnchors.TryGetValue(
                    unit,
                    out Transform anchor) ||
                anchor == null)
            {
                GameObject anchorObject =
                    new GameObject(
                        $"ConstellationTargetAnchor_{unit.UnitName}");

                anchor = anchorObject.transform;

                _targetAnchors[unit] = anchor;
            }

            anchor.SetPositionAndRotation(
                actorTransform.position,
                actorTransform.rotation);

            anchor.localScale =
                actorTransform.lossyScale;

            targetTransform = actorTransform;

            return true;
        }

        if (_targetAnchors.TryGetValue(
                unit,
                out Transform cachedAnchor) &&
            cachedAnchor != null)
        {
            targetTransform = cachedAnchor;
            return true;
        }

        return false;
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

        if (!TryGetActorTransform(attacker, out _) ||
            !TryGetTargetTransform(target, out _))
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
    /// 별자리 공격 대상 Anchor 정리
    /// </summary>
    public void ClearTargetAnchors()
    {
        foreach (KeyValuePair<BattleUnit, Transform> pair in _targetAnchors)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }

        _targetAnchors.Clear();
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

        ClearTargetAnchors();
    }
}