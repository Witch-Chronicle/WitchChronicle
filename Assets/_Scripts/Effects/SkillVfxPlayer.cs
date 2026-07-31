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
        Play(skill, caster, target != null ? new[] { target } : System.Array.Empty<Transform>());
    }

    /// <summary>
    /// 다중 대상 재생. 광역(Area) 스킬은 모든 대상에게 이펙트가 뜬다.
    /// caster/targets는 유닛의 Transform(루트).
    /// </summary>
    public void Play(SkillData skill, Transform caster, IReadOnlyList<Transform> targets)
    {
        if (skill == null)
        {
            return;
        }

        Vector3 casterPos = caster != null
            ? CasterSpawnPos(skill, caster)
            : Vector3.zero;

        // 시전 이펙트 (시전자에게 1회)
        if (caster != null && skill.CastVfxPrefab != null)
        {
            SpawnAndForget(skill.CastVfxPrefab, casterPos, caster.rotation, skill.CastVfxScale);
        }

        // 시전 사운드 (VFX 유무와 무관하게 재생)
        PlaySfx(skill.CastSfx, casterPos);

        switch (skill.PresentationType)
        {
            case SkillPresentationType.SelfTarget:
                // 자기/힐형: 시전자 위치에 1회
                StartCoroutine(SpawnDelayed(ImpactPrefab(skill), casterPos, _hitDelay, skill.HitVfxScale, skill.HitSfx));
                break;

            case SkillPresentationType.Projectile:
                // 투사체형: 각 대상으로 투사체가 날아간 뒤 명중 (광역이면 대상마다)
                for (int i = 0; targets != null && i < targets.Count; i++)
                {
                    Transform t = targets[i];
                    if (t == null) continue;

                    // 명중음은 대상이 여럿이어도 1회만 (겹쳐 울리는 것 방지)
                    AudioClip projSfx = i == 0 ? skill.HitSfx : null;

                    if (skill.ProjectileVfxPrefab != null)
                    {
                        StartCoroutine(ProjectileRoutine(skill, casterPos, t, projSfx));
                    }
                    else
                    {
                        StartCoroutine(SpawnDelayed(ImpactPrefab(skill), TargetSpawnPos(skill, t), _hitDelay, skill.HitVfxScale, projSfx));
                    }
                }
                break;

            case SkillPresentationType.Area:
                // 광역형: 모든 대상 위치에 정지 재생(날아가지 않음)
                for (int i = 0; targets != null && i < targets.Count; i++)
                {
                    Transform t = targets[i];
                    if (t == null) continue;

                    // 명중음은 대상이 여럿이어도 1회만 (겹쳐 울리는 것 방지)
                    StartCoroutine(SpawnDelayed(ImpactPrefab(skill), TargetSpawnPos(skill, t), _hitDelay, skill.HitVfxScale, i == 0 ? skill.HitSfx : null));
                }
                break;
        }
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

    private IEnumerator ProjectileRoutine(SkillData skill, Vector3 start, Transform target, AudioClip hitSfx = null)
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
                proj.transform.position = Vector3.Lerp(start, end, t / _projectileTravelTime);
            }

            yield return null;
        }

        if (proj != null)
        {
            Destroy(proj);
        }

        PlaySfx(hitSfx, end);
        SpawnPrefab(skill.HitVfxPrefab, end, skill.HitVfxScale);
    }

    private IEnumerator SpawnDelayed(GameObject prefab, Vector3 pos, float delay, float scale = 0f, AudioClip sfx = null)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        PlaySfx(sfx, pos);
        SpawnPrefab(prefab, pos, scale);
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
