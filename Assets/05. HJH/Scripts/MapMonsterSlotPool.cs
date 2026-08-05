using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 상세 정보의 몬스터 슬롯을 재사용하기 위한 로컬 오브젝트 풀입니다.
/// </summary>
public class MapMonsterSlotPool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private MapMonsterSlotUI _prefab;
    [SerializeField] private Transform _content;

    [Tooltip("패널 최초 초기화 시 미리 생성할 슬롯 수")]
    [Min(0)]
    [SerializeField] private int _initialPoolSize = 8;

    private readonly Queue<MapMonsterSlotUI> _inactiveSlots = new();
    private readonly List<MapMonsterSlotUI> _activeSlots = new();

    private bool _initialized;

    public int ActiveCount => _activeSlots.Count;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 초기 슬롯을 미리 생성합니다.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        if (_prefab == null)
        {
            Debug.LogError(
                "[MapMonsterSlotPool] Prefab_MapMonster_v1이 할당되지 않았습니다.",
                this
            );

            return;
        }

        if (_content == null)
        {
            Debug.LogError(
                "[MapMonsterSlotPool] Content Transform이 할당되지 않았습니다.",
                this
            );

            return;
        }

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateSlot();
        }
    }

    /// <summary>
    /// 풀에서 몬스터 슬롯을 가져옵니다.
    /// </summary>
    public MapMonsterSlotUI Get(EnemyBattleData enemyData)
    {
        Initialize();

        if (_prefab == null || _content == null)
        {
            return null;
        }

        MapMonsterSlotUI slot;

        if (_inactiveSlots.Count > 0)
        {
            slot = _inactiveSlots.Dequeue();
        }
        else
        {
            slot = CreateSlot();
        }

        if (slot == null)
        {
            return null;
        }

        slot.transform.SetParent(_content, false);
        slot.transform.SetAsLastSibling();
        slot.gameObject.SetActive(true);
        slot.Bind(enemyData);

        _activeSlots.Add(slot);

        return slot;
    }

    /// <summary>
    /// 현재 표시 중인 슬롯을 모두 풀로 반환합니다.
    /// </summary>
    public void ReleaseAll()
    {
        for (int i = _activeSlots.Count - 1; i >= 0; i--)
        {
            MapMonsterSlotUI slot = _activeSlots[i];

            if (slot == null)
            {
                continue;
            }

            slot.Clear();
            slot.gameObject.SetActive(false);

            _inactiveSlots.Enqueue(slot);
        }

        _activeSlots.Clear();
    }

    private MapMonsterSlotUI CreateSlot()
    {
        if (_prefab == null || _content == null)
        {
            return null;
        }

        MapMonsterSlotUI slot = Instantiate(_prefab, _content);

        slot.Clear();
        slot.gameObject.SetActive(false);

        _inactiveSlots.Enqueue(slot);

        return slot;
    }
}