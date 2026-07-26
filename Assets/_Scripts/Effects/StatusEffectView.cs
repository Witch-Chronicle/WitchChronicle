using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상태이상 지속 표시. 상태 종류별 루프 VFX를 유닛 몸에 붙여 켜고 끈다.
/// 판정은 하지 않고 외부(훅)가 ShowStatus/HideStatus/Sync/ClearAll을 호출하면 반응만 한다.
/// </summary>
public class StatusEffectView : MonoBehaviour
{
    [System.Serializable]
    public struct StatusVfxEntry
    {
        public StatusEffectType Type;
        public GameObject VfxPrefab;
    }

    [Tooltip("VFX를 붙일 위치. 몸을 따라가려면 척추/가슴 본을 넣는다. 비우면 이 오브젝트 기준")]
    [SerializeField] private Transform _attachPoint;

    [Tooltip("붙일 위치 오프셋(로컬)")]
    [SerializeField] private Vector3 _offset = Vector3.zero;

    [Tooltip("VFX 크기 배율(1=원본, 0.5=절반). 파티클 Scaling Mode가 Hierarchy/Local이어야 반영됨")]
    [SerializeField] private float _scale = 1.0f;

    [Tooltip("상태이상 종류별 루프 VFX 프리팹")]
    [SerializeField] private StatusVfxEntry[] _entries;

    private readonly Dictionary<StatusEffectType, GameObject> _active =
        new Dictionary<StatusEffectType, GameObject>();

    private readonly List<StatusEffectType> _syncRemove = new List<StatusEffectType>();

    private void Awake()
    {
        if (_attachPoint == null)
        {
            _attachPoint = transform;
        }
    }

    /// <summary>해당 상태이상의 루프 VFX를 켠다. 이미 켜져 있거나 매핑이 없으면 무시.</summary>
    public void ShowStatus(StatusEffectType type)
    {
        if (type == StatusEffectType.None || _active.ContainsKey(type))
        {
            return;
        }

        GameObject prefab = FindPrefab(type);

        if (prefab == null)
        {
            return;
        }

        GameObject vfx = Instantiate(prefab, _attachPoint);
        vfx.transform.localPosition = _offset;
        vfx.transform.localRotation = Quaternion.identity;

        if (Mathf.Approximately(_scale, 1.0f) == false)
        {
            vfx.transform.localScale *= _scale;
        }

        _active.Add(type, vfx);
    }

    /// <summary>해당 상태이상의 VFX를 끈다.</summary>
    public void HideStatus(StatusEffectType type)
    {
        if (_active.TryGetValue(type, out GameObject vfx) == false)
        {
            return;
        }

        if (vfx != null)
        {
            Destroy(vfx);
        }

        _active.Remove(type);
    }

    /// <summary>
    /// 현재 활성 상태 목록에 맞춰 동기화한다(폴링 훅용).
    /// 목록에 있는데 안 켜진 건 켜고, 켜져 있는데 목록에 없는 건 끈다.
    /// </summary>
    public void Sync(ICollection<StatusEffectType> current)
    {
        if (current == null)
        {
            ClearAll();
            return;
        }

        foreach (StatusEffectType type in current)
        {
            ShowStatus(type);
        }

        _syncRemove.Clear();

        foreach (KeyValuePair<StatusEffectType, GameObject> pair in _active)
        {
            if (current.Contains(pair.Key) == false)
            {
                _syncRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < _syncRemove.Count; i++)
        {
            HideStatus(_syncRemove[i]);
        }
    }

    /// <summary>모든 상태이상 VFX 제거(전투 종료/사망 시).</summary>
    public void ClearAll()
    {
        foreach (KeyValuePair<StatusEffectType, GameObject> pair in _active)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value);
            }
        }

        _active.Clear();
    }

    private GameObject FindPrefab(StatusEffectType type)
    {
        if (_entries == null)
        {
            return null;
        }

        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].Type == type)
            {
                return _entries[i].VfxPrefab;
            }
        }

        return null;
    }
}
