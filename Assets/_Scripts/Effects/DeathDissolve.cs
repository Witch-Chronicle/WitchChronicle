using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사망 시 캐릭터를 디졸브(SG_Dissolve)로 사라지게 한다.
/// 캐릭터 메시가 여러 파트(머티리얼)로 나뉘어 있어도 전부 처리한다.
/// 원본 머티리얼의 메인 텍스처를 디졸브 머티리얼의 _BaseMap으로 복사해 모양을 유지한다.
/// 판정은 하지 않고 외부(Binder)가 Play()를 호출하면 반응만 한다.
/// </summary>
public class DeathDissolve : MonoBehaviour
{
    [Header("SG_Dissolve로 만든 머티리얼")]
    [SerializeField] private Material _dissolveMaterial;

    [Header("타이밍(초)")]
    [Tooltip("사망 모션 재생 시간. 이 시간 뒤 디졸브 시작")]
    [SerializeField] private float _startDelay = 1.0f;

    [Tooltip("디졸브에 걸리는 시간")]
    [SerializeField] private float _duration = 1.2f;

    [Header("상승 이펙트 (몸에서 피어오르는 파티클)")]
    [Tooltip("디졸브 시작 시 몸 위치에 스폰할 이펙트 프리팹")]
    [SerializeField] private GameObject _riseEffectPrefab;

    [Tooltip("이펙트를 스폰할 기준 위치. 쓰러지는 몸을 따라가려면 척추/골반 본을 넣는다. 비우면 루트 사용")]
    [SerializeField] private Transform _riseEffectAnchor;

    [Tooltip("스폰 높이(기준 위치에서 위로)")]
    [SerializeField] private float _riseEffectHeight = 1.0f;

    [Tooltip("이펙트 크기 배율(1=원본, 0.5=절반)")]
    [SerializeField] private float _riseEffectScale = 1.0f;

    [Tooltip("디졸브 완료 후 이펙트를 정리하기까지 여유 시간(잔여 파티클 재생용)")]
    [SerializeField] private float _effectLinger = 2.0f;

    [Header("완료 후")]
    [SerializeField] private bool _deactivateOnComplete = true;

    [Header("References")]
    [Tooltip("디졸브 후 비활성화할 캐릭터 모델 루트")]
    [SerializeField] private GameObject _visualRoot;

    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private bool _playing;

    /// <summary>디졸브 시작. 중복 호출은 무시한다.</summary>
    public void Play()
    {
        if (_playing)
        {
            return;
        }

        _playing = true;
        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        if (_startDelay > 0f)
        {
            yield return new WaitForSeconds(_startDelay);
        }

        GameObject riseEffect = SpawnRiseEffect();

        List<Material> instances = SwapToDissolveMaterials();

        if (instances.Count == 0)
        {
            CleanupRiseEffect(riseEffect);

            if (_deactivateOnComplete && _visualRoot != null)
            {
                _visualRoot.SetActive(false);
            }

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float amount = Mathf.Clamp01(elapsed / _duration);

            for (int i = 0; i < instances.Count; i++)
            {
                instances[i].SetFloat(DissolveAmountId, amount);
            }

            yield return null;
        }

        for (int i = 0; i < instances.Count; i++)
        {
            instances[i].SetFloat(DissolveAmountId, 1f);
        }

        CleanupRiseEffect(riseEffect);

        if (_deactivateOnComplete && _visualRoot != null)
        {
            _visualRoot.SetActive(false);
        }
    }

    /// <summary>디졸브 시작 위치(몸 위치 + 높이)에 상승 이펙트를 스폰한다. 없으면 null.</summary>
    private GameObject SpawnRiseEffect()
    {
        if (_riseEffectPrefab == null)
        {
            return null;
        }

        Vector3 basePos = _riseEffectAnchor != null ? _riseEffectAnchor.position : transform.position;
        Vector3 spawnPos = basePos + Vector3.up * _riseEffectHeight;

        // 캐릭터가 완료 시 비활성화되므로 부모를 두지 않고 월드에 독립 생성한다.
        GameObject effect = Instantiate(_riseEffectPrefab, spawnPos, Quaternion.identity);

        if (Mathf.Approximately(_riseEffectScale, 1.0f) == false)
        {
            effect.transform.localScale *= _riseEffectScale;
        }

        return effect;
    }

    /// <summary>상승 이펙트를 잔여 재생 여유 시간 뒤 제거한다.</summary>
    private void CleanupRiseEffect(GameObject riseEffect)
    {
        if (riseEffect != null)
        {
            Destroy(riseEffect, _effectLinger);
        }
    }

    /// <summary>
    /// 모든 렌더러의 머티리얼을 디졸브 머티리얼 인스턴스로 교체하고,
    /// 원본 메인 텍스처를 _BaseMap으로 넘긴다. 생성된 인스턴스 목록을 반환한다.
    /// </summary>
    private List<Material> SwapToDissolveMaterials()
    {
        List<Material> instances = new List<Material>();

        if (_dissolveMaterial == null)
        {
            Debug.LogError("[DeathDissolve] 디졸브 머티리얼이 비어있습니다: " + name);
            return instances;
        }

        Renderer[] renderers =
            _visualRoot != null
                ? _visualRoot.GetComponentsInChildren<Renderer>()
                : GetComponentsInChildren<Renderer>();

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer rend = renderers[r];
            Material[] originals = rend.sharedMaterials;
            Material[] swapped = new Material[originals.Length];

            for (int m = 0; m < originals.Length; m++)
            {
                Material inst = new Material(_dissolveMaterial);

                if (originals[m] != null && originals[m].mainTexture != null)
                {
                    inst.SetTexture(BaseMapId, originals[m].mainTexture);
                }

                inst.SetFloat(DissolveAmountId, 0f);
                swapped[m] = inst;
                instances.Add(inst);
            }

            rend.materials = swapped;
        }

        return instances;
    }
}
