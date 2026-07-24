using UnityEngine;

public enum Event_Type { Reward, Trap, Debuff }

[CreateAssetMenu(fileName = "NewEventData", menuName = "Dungeon/Event Data")]
public class EventDataSO : ScriptableObject
{
    public string EventName;

    public string Description;
    public GameObject Prefab; // 각 이벤트에 해당하는 프리팹
    public Event_Type Type;

    [Header("Effect Settings")]
    public int Value; // 데미지량, 회복량, 혹은 아이템 ID 등
    public GameObject EffectPrefab; // 발동 시 보여줄 파티클 등
}