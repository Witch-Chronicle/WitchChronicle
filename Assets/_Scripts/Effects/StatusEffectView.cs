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

        [Tooltip("이 상태이상 VFX만의 크기 배율(1=원본). 0이면 아래 전역 Scale을 사용")]
        public float Scale;

        [Tooltip("이 상태이상 VFX만의 위치 오프셋(로컬). 전역 Offset에 더해진다. 0이면 전역만 적용")]
        public Vector3 Offset;

        [Tooltip("이 상태이상이 걸리는 순간 1회 재생할 소리")]
        public AudioClip ApplySfx;
    }

    [Tooltip("VFX를 붙일 위치. 몸을 따라가려면 척추/가슴 본을 넣는다. 비우면 이 오브젝트 기준")]
    [SerializeField] private Transform _attachPoint;

    [Tooltip("전역 위치 오프셋(로컬). 모든 VFX의 기준 위치. 각 entry의 Offset이 여기에 더해진다")]
    [SerializeField] private Vector3 _offset = Vector3.zero;

    [Tooltip("전역 VFX 크기 배율(1=원본). 각 entry의 Scale이 0일 때 이 값이 쓰인다. 파티클 Scaling Mode가 Hierarchy/Local이어야 반영됨")]
    [SerializeField] private float _scale = 1.0f;

    [Tooltip("상태이상 종류별 루프 VFX 프리팹")]
    [SerializeField] private StatusVfxEntry[] _entries;

    [Tooltip("상태이상 적용음을 낼 AudioSource. 비우면 실행 시 자동으로 만든다")]
    [SerializeField] private AudioSource _audioSource;

    [Range(0f, 1f)]
    [SerializeField] private float _sfxVolume = 0.3f;

    private readonly Dictionary<StatusEffectType, GameObject> _active =
        new Dictionary<StatusEffectType, GameObject>();

    private readonly List<StatusEffectType> _syncRemove = new List<StatusEffectType>();

    private void Awake()
    {
        if (_attachPoint == null)
        {
            _attachPoint = transform;
        }

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    /// <summary>해당 상태이상의 루프 VFX를 켠다. 이미 켜져 있거나 매핑이 없으면 무시.</summary>
    public void ShowStatus(StatusEffectType type)
    {
        if (type == StatusEffectType.None || _active.ContainsKey(type))
        {
            return;
        }

        int index = FindEntryIndex(type);

        if (index < 0 || _entries[index].VfxPrefab == null)
        {
            return;
        }

        GameObject vfx = Instantiate(_entries[index].VfxPrefab, _attachPoint);
        // 전역 Offset을 기준으로 entry별 Offset을 더해 위치 미세조정
        vfx.transform.localPosition = _offset + _entries[index].Offset;
        vfx.transform.localRotation = Quaternion.identity;

        // entry별 Scale이 있으면 그것, 없으면(0) 전역 _scale 사용
        float scale = _entries[index].Scale > 0f ? _entries[index].Scale : _scale;

        if (Mathf.Approximately(scale, 1.0f) == false)
        {
            vfx.transform.localScale *= scale;
        }

        _active.Add(type, vfx);

        // 걸리는 순간 1회만 재생 (이미 걸려 있으면 위에서 return되므로 중복되지 않는다)
        if (_audioSource != null && _entries[index].ApplySfx != null)
        {
            _audioSource.PlayOneShot(_entries[index].ApplySfx, _sfxVolume);
        }
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

    private int FindEntryIndex(StatusEffectType type)
    {
        if (_entries == null)
        {
            return -1;
        }

        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].Type == type)
            {
                return i;
            }
        }

        return -1;
    }
}
