using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 사용 시 SkillData에 담긴 VFX 프리팹(시전/투사체/명중)을 재생한다.
/// 판정은 하지 않고 Binder가 Play를 호출하면 반응만 한다.
/// </summary>
public class SkillVfxPlayer : MonoBehaviour
{
    [Header("스폰 높이 (발밑 기준 위로)")]
    [SerializeField] private float _casterHeight = 1.0f;
    [SerializeField] private float _targetHeight = 1.0f;

    [Header("투사체")]
    [Tooltip("투사체가 대상까지 날아가는 시간(초)")]
    [SerializeField] private float _projectileTravelTime = 0.4f;

    [Header("명중")]
    [Tooltip("비투사체(광역/자기)에서 시전 후 명중 이펙트까지 지연")]
    [SerializeField] private float _hitDelay = 0.2f;
    [Tooltip("명중 후 연출 종료까지 보장할 시간")]
    [SerializeField] private float _postImpactDuration = 0.25f;

    [Header("정리")]
    [Tooltip("생성한 VFX를 자동 파괴하기까지 시간")]
    [SerializeField] private float _vfxLifetime = 3f;

    [Header("사운드")]
    [Tooltip("스킬 사운드(SkillData의 Cast/Hit Sfx) 볼륨")]
    [Range(0f, 1f)]
    [SerializeField] private float _sfxVolume = 1f;

    /// <summary>단일 대상 재생.</summary>
    public void Play(SkillData skill, Transform caster, Transform target)
    {
        Play(skill, caster, target, null, null);
    }

    /// <summary>단일 대상 재생 및 완료 통지.</summary>
    public void Play(
        SkillData skill,
        Transform caster,
        Transform target,
        System.Action onImpact,
        System.Action onComplete)
    {
        Play(
            skill,
            caster,
            target != null ? new[] { target } : System.Array.Empty<Transform>(),
            onImpact,
            onComplete);
    }

    /// <summary>다중 대상 재생.</summary>
    public void Play(SkillData skill, Transform caster, IReadOnlyList<Transform> targets)
    {
        Play(skill, caster, targets, null, null);
    }

    /// <summary>
    /// 다중 대상 재생 및 완료 통지
    /// </summary>
    /// <param name="skill">재생 스킬</param>
    /// <param name="caster">시전자 Transform</param>
    /// <param name="targets">대상 Transform 목록</param>
    /// <param name="onImpact">명중 완료 콜백</param>
    /// <param name="onComplete">연출 완료 콜백</param>
    public void Play(
        SkillData skill,
        Transform caster,
        IReadOnlyList<Transform> targets,
        System.Action onImpact,
        System.Action onComplete)
    {
        if (skill == null)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(
            PlayRoutine(
                skill,
                caster,
                targets,
                onImpact,
                onComplete));
    }

    /// <summary>
    /// 스킬 VFX 재생 순서 진행
    /// </summary>
    /// <param name="skill">재생 스킬</param>
    /// <param name="caster">시전자 Transform</param>
    /// <param name="targets">대상 Transform 목록</param>
    /// <param name="onImpact">명중 완료 콜백</param>
    /// <param name="onComplete">연출 완료 콜백</param>
    private IEnumerator PlayRoutine(
        SkillData skill,
        Transform caster,
        IReadOnlyList<Transform> targets,
        System.Action onImpact,
        System.Action onComplete)
    {
        Vector3 casterPos = caster != null
            ? CasterSpawnPos(skill, caster)
            : Vector3.zero;

        if (caster != null && skill.CastVfxPrefab != null)
        {
            SpawnAndForget(
                skill.CastVfxPrefab,
                casterPos,
                caster.rotation,
                skill.CastVfxScale);
        }

        // 시전 사운드 (VFX 유무와 무관하게 재생)
        PlaySfx(skill.CastSfx, casterPos);

        switch (skill.PresentationType)
        {
            case SkillPresentationType.SelfTarget:
                yield return SpawnDelayed(
                    ImpactPrefab(skill),
                    casterPos,
                    _hitDelay,
                    skill.HitVfxScale);

                // 명중 사운드는 임팩트 시점에 1회
                PlaySfx(skill.HitSfx, casterPos);

                onImpact?.Invoke();
                break;

            case SkillPresentationType.Projectile:
                yield return PlayTargetVfxRoutine(
                    skill,
                    casterPos,
                    targets,
                    true);

                // 명중 사운드는 대상이 여럿이어도 1회만 (겹쳐 울리는 것 방지)
                PlaySfx(skill.HitSfx, FirstTargetPos(skill, targets, casterPos));

                onImpact?.Invoke();
                break;

            case SkillPresentationType.Area:
                yield return PlayTargetVfxRoutine(
                    skill,
                    casterPos,
                    targets,
                    false);

                // 명중 사운드는 대상이 여럿이어도 1회만 (겹쳐 울리는 것 방지)
                PlaySfx(skill.HitSfx, FirstTargetPos(skill, targets, casterPos));

                onImpact?.Invoke();
                break;

            default:
                onImpact?.Invoke();
                break;
        }

        if (_postImpactDuration > 0f)
        {
            yield return new WaitForSeconds(_postImpactDuration);
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 대상별 VFX 재생 완료 대기
    /// </summary>
    /// <param name="skill">재생 스킬</param>
    /// <param name="casterPos">시전자 위치</param>
    /// <param name="targets">대상 목록</param>
    /// <param name="useProjectile">투사체 사용 여부</param>
    private IEnumerator PlayTargetVfxRoutine(
        SkillData skill,
        Vector3 casterPos,
        IReadOnlyList<Transform> targets,
        bool useProjectile)
    {
        int targetCount = 0;
        int completedCount = 0;

        for (int i = 0; targets != null && i < targets.Count; i++)
        {
            Transform target = targets[i];

            if (target == null)
            {
                continue;
            }

            targetCount++;

            if (useProjectile && skill.ProjectileVfxPrefab != null)
            {
                StartCoroutine(
                    ProjectileRoutine(
                        skill,
                        casterPos,
                        target,
                        () => completedCount++));
            }
            else
            {
                StartCoroutine(
                    SpawnDelayed(
                        ImpactPrefab(skill),
                        TargetSpawnPos(skill, target),
                        _hitDelay,
                        skill.HitVfxScale,
                        () => completedCount++));
            }
        }

        if (targetCount == 0)
        {
            yield break;
        }

        yield return new WaitUntil(
            () => completedCount >= targetCount);
    }

    /// <summary>명중/광역 재생에 쓸 프리팹: 명중 슬롯 우선, 없으면 투사체 슬롯을 정지 재생용으로 사용.</summary>
    private GameObject ImpactPrefab(SkillData skill)
    {
        return skill.HitVfxPrefab != null ? skill.HitVfxPrefab : skill.ProjectileVfxPrefab;
    }

    /// <summary>
    /// 시전 이펙트 스폰 위치. 스킬에 Cast Vfx Offset이 지정돼 있으면 그것,
    /// 아니면 전역 Caster Height를 쓴다.
    /// </summary>
    private Vector3 CasterSpawnPos(SkillData skill, Transform caster)
    {
        Vector3 offset = skill.CastVfxOffset != Vector3.zero
            ? skill.CastVfxOffset
            : Vector3.up * _casterHeight;

        return caster.position + offset;
    }

    /// <summary>
    /// 명중/광역 이펙트 스폰 위치. 스킬에 Hit Vfx Offset이 지정돼 있으면 그것,
    /// 아니면 전역 Target Height를 쓴다.
    /// </summary>
    private Vector3 TargetSpawnPos(SkillData skill, Transform target)
    {
        Vector3 offset = skill.HitVfxOffset != Vector3.zero
            ? skill.HitVfxOffset
            : Vector3.up * _targetHeight;

        return target.position + offset;
    }

    /// <summary>
    /// 투사체 이동 및 명중 처리
    /// </summary>
    /// <param name="skill">재생 스킬</param>
    /// <param name="start">시작 위치</param>
    /// <param name="target">대상 Transform</param>
    /// <param name="onImpact">명중 완료 콜백</param>
    private IEnumerator ProjectileRoutine(
        SkillData skill,
        Vector3 start,
        Transform target,
        System.Action onImpact)
    {
        Vector3 end = TargetSpawnPos(skill, target);
        Vector3 dir = end - start;
        Quaternion rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : Quaternion.identity;

        GameObject proj = Instantiate(skill.ProjectileVfxPrefab, start, rot);

        if (skill.HitVfxScale > 0f)
        {
            proj.transform.localScale *= skill.HitVfxScale;
        }

        float t = 0f;

        while (t < _projectileTravelTime)
        {
            t += Time.deltaTime;

            if (target != null)
            {
                end = TargetSpawnPos(skill, target);
            }

            if (proj != null)
            {
                proj.transform.position = Vector3.Lerp(
                    start,
                    end,
                    t / _projectileTravelTime);
            }

            yield return null;
        }

        if (proj != null)
        {
            Destroy(proj);
        }

        SpawnPrefab(
            skill.HitVfxPrefab,
            end,
            skill.HitVfxScale);

        onImpact?.Invoke();
    }

    /// <summary>
    /// 지연 후 VFX 생성
    /// </summary>
    /// <param name="prefab">생성 프리팹</param>
    /// <param name="pos">생성 위치</param>
    /// <param name="delay">생성 지연 시간</param>
    /// <param name="scale">크기 배율</param>
    /// <param name="onSpawned">생성 완료 콜백</param>
    private IEnumerator SpawnDelayed(
        GameObject prefab,
        Vector3 pos,
        float delay,
        float scale = 0f,
        System.Action onSpawned = null)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        SpawnPrefab(prefab, pos, scale);
        onSpawned?.Invoke();
    }

    /// <summary>명중 사운드를 재생할 대표 위치(첫 유효 대상). 없으면 fallback.</summary>
    private Vector3 FirstTargetPos(
        SkillData skill,
        IReadOnlyList<Transform> targets,
        Vector3 fallback)
    {
        for (int i = 0; targets != null && i < targets.Count; i++)
        {
            if (targets[i] != null)
            {
                return TargetSpawnPos(skill, targets[i]);
            }
        }

        return fallback;
    }

    /// <summary>지정 위치에서 클립을 1회 재생한다(클립이 없으면 무시).</summary>
    private void PlaySfx(AudioClip clip, Vector3 pos)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, pos, _sfxVolume);
    }

    private void SpawnPrefab(GameObject prefab, Vector3 pos, float scale = 0f)
    {
        if (prefab != null)
        {
            SpawnAndForget(prefab, pos, Quaternion.identity, scale);
        }
    }

    /// <summary>
    /// VFX 생성 후 일정 시간 뒤 파괴. scale이 0보다 크면 프리팹 원본 크기에 배율을 적용한다.
    /// </summary>
    private void SpawnAndForget(GameObject prefab, Vector3 pos, Quaternion rot, float scale = 0f)
    {
        GameObject vfx = Instantiate(prefab, pos, rot);

        if (scale > 0f)
        {
            vfx.transform.localScale *= scale;
        }

        Destroy(vfx, _vfxLifetime);
    }
}
