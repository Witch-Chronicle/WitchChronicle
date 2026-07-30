using System.Collections.Generic;
using UnityEngine;

public enum Event_Type { Reward, Trap, Debuff }

/// <summary>이벤트가 파티에 주는 효과 종류.</summary>
public enum EventEffectKind
{
    None,           // 효과 없음(메시지만)
    HealHp,         // HP 회복 (Value = 고정 수치)
    HealHpPercent,  // HP 회복 (Value = 최대 HP 대비 %)
    HealMp,         // MP 회복 (Value = 고정 수치)
    HealMpPercent,  // MP 회복 (Value = 최대 MP 대비 %)
    DamageHp,       // HP 피해 (Value = 고정 수치)
    DamageHpPercent // HP 피해 (Value = 최대 HP 대비 %)
}

[CreateAssetMenu(fileName = "NewEventData", menuName = "Dungeon/Event Data")]
public class EventDataSO : ScriptableObject
{
    public string EventName;

    public string Description;
    public GameObject Prefab; // 각 이벤트에 해당하는 프리팹
    public Event_Type Type;

    [Header("Effect Settings")]
    [Tooltip("파티에 적용할 효과 종류. None이면 메시지만 표시")]
    public EventEffectKind EffectKind = EventEffectKind.None;
    public int Value; // 데미지량, 회복량, 혹은 아이템 ID 등
    public GameObject EffectPrefab; // 발동 시 보여줄 파티클 등

    [Header("Enemy Group")]

    public List<EnemyBattleData> mimic;
}