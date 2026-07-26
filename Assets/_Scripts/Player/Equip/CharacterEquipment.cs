using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 1명분의 장비 장착/해제를 담당하고, 장착 중인 장비들의 스탯을 합산해서 StatBlock으로 공개.
/// 캐릭터 4명이면 씬에 이 컴포넌트가 4개 존재하고, 각자 다른 _character 값을 가짐.
/// - 슬롯당 하나씩(EquipSlotType 8종) EquipmentInstance를 들고 있음
/// - "이 장비를 지금 누군가(4명 중 아무나) 장착 중인가"는 static으로 전역 공유 (같은 장비를 두 캐릭터가 동시에 낄 수 없으므로)
/// - 장착/해제될 때마다 같은 오브젝트의 CharacterStats에 StatModifier(Equipment 출처)를 등록/제거해서 실제 전투 스탯에 반영
/// - 장착 중인 장비가 판매 등으로 PlayerInventory에서 사라지면 자동으로 해제
/// * 슬롯 하나당 StatModifier의 SourceId로 "슬롯 이름"을 사용 -> 새 장비로 교체/해제 시 그 슬롯 것만 정확히 제거 가능
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class CharacterEquipment : MonoBehaviour
{
    [Header("이 컴포넌트가 어느 캐릭터의 장비인지")]
    [SerializeField] private CharacterType _character;

    [Header("스탯 적용 대상")]
    [Tooltip("같은 오브젝트에 붙은 CharacterStats. 비워두면 자동으로 찾음")]
    [SerializeField] private CharacterStats _characterStats;

    private readonly Dictionary<EquipSlotType, EquipmentInstance> _equipped = new Dictionary<EquipSlotType, EquipmentInstance>();

    private StatBlock _totalStats = new StatBlock();

    // 캐릭터 -> CharacterEquipment 정적 등록 (CharacterSelectionManager가 선택한 캐릭터로 바로 찾아갈 수 있도록)
    private static readonly Dictionary<CharacterType, CharacterEquipment> _registry = new Dictionary<CharacterType, CharacterEquipment>();

    // 지금 누군가(4명 중 아무나)가 장착 중인 EquipmentInstance 전역 집합. 중복 장착 방지 체크용.
    private static readonly HashSet<EquipmentInstance> _globallyEquipped = new HashSet<EquipmentInstance>();

    /// <summary>
    /// 아무 캐릭터의 장비 상태든 바뀌면 호출됨. 인벤토리 목록처럼 "누구 장비인지 상관없이" 갱신해야 하는 UI가 구독.
    /// </summary>
    public static event Action OnAnyEquipmentChanged;

    public CharacterType Character => _character;

    /// <summary>
    /// 이 캐릭터가 장착 중인 장비들의 스탯 총합 (UI 미리보기 계산용). 실제 전투 스탯 반영은 CharacterStats 쪽에서 이루어짐.
    /// </summary>
    public StatBlock TotalEquipmentStats => _totalStats;

    /// <summary>
    /// 이 캐릭터의 장착 상태가 바뀔 때마다 호출됨 (다른 캐릭터의 변화는 포함 안 됨).
    /// </summary>
    public event Action OnEquipmentChanged;

    private void Awake()
    {
        // 이미 살아있는 진짜 인스턴스가 등록되어 있다면, 이건 (테스트용) 중복 오브젝트이므로 등록하지 않음
        if (_registry.TryGetValue(_character, out var existing) && existing != null && existing != this)
        {
            if (_characterStats == null)
            {
                _characterStats = GetComponent<CharacterStats>();
            }
            return;
        }

        _registry[_character] = this;

        if (_characterStats == null)
        {
            _characterStats = GetComponent<CharacterStats>();
        }
    }

    private void OnEnable()
    {
        // Awake 이후 재활성화되는 경우(씬 전환 중 부모가 비활성화됐다 켜지는 경우 등)에도
        // 레지스트리에 다시 등록되도록 보정. OnDisable에서 지워졌던 게 여기서 복구됨.
        _registry[_character] = this;
    }

    private void OnDisable()
    {
        if (_registry.TryGetValue(_character, out var registered) && registered == this)
        {
            _registry.Remove(_character);
        }
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
        else
        {
            Debug.LogWarning($"[CharacterEquipment:{_character}] PlayerInventory.Instance가 null이라 구독 실패");
        }
    }


    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    /// <summary>
    /// 지정한 캐릭터의 CharacterEquipment를 찾아서 반환. 없으면 null.
    /// </summary>
    public static CharacterEquipment GetByCharacter(CharacterType character)
    {
        return _registry.TryGetValue(character, out var equipment) ? equipment : null;
    }

    /// <summary>
    /// 이 장비를 지금 누군가(4명 중 아무나)가 장착 중인지 여부. 인벤토리 목록 필터링용.
    /// </summary>
    public static bool IsEquippedByAnyone(EquipmentInstance instance)
    {
        return instance != null && _globallyEquipped.Contains(instance);
    }

    /// <summary>
    /// 장비 장착. instance.baseData.equipSlotType을 보고 슬롯을 자동으로 판단.
    /// 이 캐릭터의 해당 슬롯에 이미 다른 장비가 있으면 자동으로 해제 후 장착.
    /// * 이미 다른 캐릭터가 장착 중인 장비는 애초에 인벤토리 목록에 안 뜨므로 여기서 별도 방어 체크는 안 함.
    /// </summary>
    public void Equip(EquipmentInstance instance)
    {
        if (instance == null || instance.baseData == null) return;

        EquipSlotType slot = instance.baseData.equipSlotType;

        // 이미 그 슬롯에 같은 장비가 장착되어 있으면 아무것도 안 함
        if (_equipped.TryGetValue(slot, out var current) && current == instance)
        {
            return;
        }

        // 이 캐릭터가 그 슬롯에 다른 걸 끼고 있었다면, 전역 목록 + CharacterStats 모디파이어에서도 빼줌
        if (current != null)
        {
            _globallyEquipped.Remove(current);
            RemoveSlotModifiers(slot);
        }

        _equipped[slot] = instance;
        _globallyEquipped.Add(instance);

        ApplySlotModifiers(instance, slot);

        RecalculateTotalStats();
    }

    /// <summary>
    /// 지정한 슬롯의 장비를 해제.
    /// </summary>
    public void Unequip(EquipSlotType slot)
    {
        if (_equipped.TryGetValue(slot, out var removed))
        {
            _equipped.Remove(slot);
            _globallyEquipped.Remove(removed);
            RemoveSlotModifiers(slot);
            RecalculateTotalStats();
        }
    }

    /// <summary>
    /// 지정한 슬롯에 장착된 장비 반환. 없으면 null.
    /// </summary>
    public EquipmentInstance GetEquipped(EquipSlotType slot)
    {
        return _equipped.TryGetValue(slot, out var instance) ? instance : null;
    }

    /// <summary>
    /// 이 장비 개체가 "이 캐릭터"한테 장착되어 있는지 여부.
    /// </summary>
    public bool IsEquipped(EquipmentInstance instance)
    {
        return instance != null && _equipped.ContainsValue(instance);
    }

    /// <summary>
    /// PlayerInventory 내용이 바뀔 때마다 호출됨 (구매/판매/강화 등).
    /// - 이 캐릭터가 장착 중인 장비가 인벤토리에서 사라졌으면(판매 등) 자동으로 해제
    /// - 여전히 장착 중인 장비는 최신 cachedStats로 모디파이어를 다시 등록 (강화로 스탯이 바뀐 경우 반영)
    /// </summary>
    private void HandleInventoryChanged()
    {
        if (PlayerInventory.Instance == null) return;

        List<EquipSlotType> slotsToUnequip = null;
        List<EquipSlotType> slotsToRefresh = null;

        foreach (var kvp in _equipped)
        {
            bool stillOwned = false;

            foreach (var owned in PlayerInventory.Instance.EquipmentInstances)
            {
                if (owned == kvp.Value)
                {
                    stillOwned = true;
                    break;
                }
            }

            if (stillOwned)
            {
                // 여전히 보유 중 -> 강화 등으로 cachedStats가 바뀌었을 수 있으니 모디파이어 재적용 대상
                slotsToRefresh ??= new List<EquipSlotType>();
                slotsToRefresh.Add(kvp.Key);
            }
            else
            {
                slotsToUnequip ??= new List<EquipSlotType>();
                slotsToUnequip.Add(kvp.Key);
            }
        }

        if (slotsToUnequip == null && slotsToRefresh == null) return;

        if (slotsToUnequip != null)
        {
            foreach (var slot in slotsToUnequip)
            {
                _globallyEquipped.Remove(_equipped[slot]);
                RemoveSlotModifiers(slot);
                _equipped.Remove(slot);
            }
        }

        if (slotsToRefresh != null)
        {
            foreach (var slot in slotsToRefresh)
            {
                // 기존 모디파이어 지우고, 최신 cachedStats로 다시 등록 (강화 반영)
                RemoveSlotModifiers(slot);
                ApplySlotModifiers(_equipped[slot], slot);
            }
        }

        RecalculateTotalStats();
    }

    // ===================== CharacterStats 연동 (StatModifier) =====================

    /// <summary>
    /// 장착한 장비의 cachedStats를 StatModifier(Flat, Equipment 출처)로 변환해서 CharacterStats에 등록.
    /// SourceId는 슬롯 이름을 사용 -> 나중에 이 슬롯만 정확히 제거 가능.
    /// </summary>
    private void ApplySlotModifiers(EquipmentInstance instance, EquipSlotType slot)
    {
        if (_characterStats == null) return;

        _characterStats.AddModifiers(BuildModifiers(instance, slot));
    }

    /// <summary>
    /// 그 슬롯에서 나온 모디파이어를 전부 제거 (교체/해제 시 호출).
    /// </summary>
    private void RemoveSlotModifiers(EquipSlotType slot)
    {
        if (_characterStats == null) return;

        _characterStats.RemoveModifiersBySource(StatModifierSourceType.Equipment, slot.ToString());
    }

    /// <summary>
    /// 0이 아닌 스탯만 골라서 StatModifier 리스트로 변환.
    /// </summary>
    private List<StatModifier> BuildModifiers(EquipmentInstance instance, EquipSlotType slot)
    {
        var stats = instance.cachedStats;
        string sourceId = slot.ToString();
        var modifiers = new List<StatModifier>();

        AddModifierIfNonZero(modifiers, StatType.MaxHP, stats.hp, sourceId);
        AddModifierIfNonZero(modifiers, StatType.MaxMP, stats.mp, sourceId);
        AddModifierIfNonZero(modifiers, StatType.SpellPower, stats.spellPower, sourceId);
        AddModifierIfNonZero(modifiers, StatType.Intelligence, stats.intelligence, sourceId);
        AddModifierIfNonZero(modifiers, StatType.Defense, stats.defense, sourceId);
        AddModifierIfNonZero(modifiers, StatType.Speed, stats.speed, sourceId);
        AddModifierIfNonZero(modifiers, StatType.Luck, stats.luck, sourceId);

        return modifiers;
    }

    private void AddModifierIfNonZero(List<StatModifier> modifiers, StatType type, int value, string sourceId)
    {
        if (value == 0) return;

        modifiers.Add(new StatModifier(type, value, StatModifierType.Flat, StatModifierSourceType.Equipment, sourceId));
    }

    // ===================== 스탯 합산 (UI 미리보기용) =====================

    /// <summary>
    /// 장착 중인 장비 전체를 순회하며 스탯 합산. 우리 쪽 필드명(hp/mp/spellPower...) -> StatType 매핑이 여기서만 일어남.
    /// * 이건 UI 표시(Current/Change 미리보기)용 합산일 뿐, 실제 전투 스탯은 CharacterStats가 별도로 계산함.
    /// </summary>
    private void RecalculateTotalStats()
    {
        var newTotal = new StatBlock();

        foreach (var instance in _equipped.Values)
        {
            EquipStatCalculator.StatSet stats = instance.cachedStats;

            newTotal.Add(StatType.MaxHP, stats.hp);
            newTotal.Add(StatType.MaxMP, stats.mp);
            newTotal.Add(StatType.SpellPower, stats.spellPower);
            newTotal.Add(StatType.Intelligence, stats.intelligence);
            newTotal.Add(StatType.Defense, stats.defense);
            newTotal.Add(StatType.Speed, stats.speed);
            newTotal.Add(StatType.Luck, stats.luck);
        }

        _totalStats = newTotal;

        Debug.Log($"[CharacterEquipment:{_character}] 장착 스탯 갱신 - HP:{_totalStats.maxHP} MP:{_totalStats.maxMP} " +
            $"마력:{_totalStats.magicPower} 지능:{_totalStats.intelligence} 방어력:{_totalStats.defense} " +
            $"속도:{_totalStats.speed} 행운:{_totalStats.luck}");

        OnEquipmentChanged?.Invoke();
        OnAnyEquipmentChanged?.Invoke();
    }
}