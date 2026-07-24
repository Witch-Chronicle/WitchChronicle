using System.Collections;
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

    /// <summary>스킬의 VFX를 재생한다. caster/target은 유닛의 Transform(루트).</summary>
    public void Play(SkillData skill, Transform caster, Transform target)
    {
        if (skill == null)
        {
            return;
        }

        Vector3 casterPos = caster != null
            ? caster.position + Vector3.up * _casterHeight
            : Vector3.zero;

        Vector3 targetPos = target != null
            ? target.position + Vector3.up * _targetHeight
            : casterPos;

        // 시전 이펙트 (있으면 시전자에게)
        if (caster != null && skill.CastVfxPrefab != null)
        {
            SpawnAndForget(skill.CastVfxPrefab, casterPos, caster.rotation);
        }

        switch (skill.PresentationType)
        {
            case SkillPresentationType.Projectile:
                // 투사체형: 투사체가 대상으로 날아간 뒤 명중
                if (skill.ProjectileVfxPrefab != null && target != null)
                {
                    StartCoroutine(ProjectileRoutine(skill, casterPos, target));
                }
                else
                {
                    StartCoroutine(SpawnDelayed(ImpactPrefab(skill), targetPos, _hitDelay));
                }
                break;

            case SkillPresentationType.Area:
                // 광역형: 대상 위치에 정지 재생(날아가지 않음)
                StartCoroutine(SpawnDelayed(ImpactPrefab(skill), targetPos, _hitDelay));
                break;

            case SkillPresentationType.SelfTarget:
                // 자기/힐형: 시전자 위치에 재생
                StartCoroutine(SpawnDelayed(ImpactPrefab(skill), casterPos, _hitDelay));
                break;
        }
    }

    /// <summary>명중/광역 재생에 쓸 프리팹: 명중 슬롯 우선, 없으면 투사체 슬롯을 정지 재생용으로 사용.</summary>
    private GameObject ImpactPrefab(SkillData skill)
    {
        return skill.HitVfxPrefab != null ? skill.HitVfxPrefab : skill.ProjectileVfxPrefab;
    }

    private IEnumerator ProjectileRoutine(SkillData skill, Vector3 start, Transform target)
    {
        Vector3 end = target.position + Vector3.up * _targetHeight;
        Vector3 dir = end - start;
        Quaternion rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : Quaternion.identity;

        GameObject proj = Instantiate(skill.ProjectileVfxPrefab, start, rot);

        float t = 0f;
        while (t < _projectileTravelTime)
        {
            t += Time.deltaTime;

            if (target != null)
            {
                end = target.position + Vector3.up * _targetHeight;
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

        SpawnPrefab(skill.HitVfxPrefab, end);
    }

    private IEnumerator SpawnDelayed(GameObject prefab, Vector3 pos, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        SpawnPrefab(prefab, pos);
    }

    private void SpawnPrefab(GameObject prefab, Vector3 pos)
    {
        if (prefab != null)
        {
            SpawnAndForget(prefab, pos, Quaternion.identity);
        }
    }

    private void SpawnAndForget(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject vfx = Instantiate(prefab, pos, rot);
        Destroy(vfx, _vfxLifetime);
    }
}
