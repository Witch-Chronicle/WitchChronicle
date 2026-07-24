using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "EventRoomTable", menuName = "Dungeon/Event Table")]
public class EventRoomTableSO : ScriptableObject
{
    [Serializable]
    public class EventEntry
    {
        public EventDataSO EventData; // 기존에 넣던 SO
        [Range(0, 100)]
        public int Weight;            // 여기에 가중치를 입력할 칸이 생깁니다!
    }

    // 1. 리스트 타입을 EventEntry로 변경
    public List<EventEntry> EventEntries = new List<EventEntry>();

    // 가중치 기반 랜덤 선택 메서드
    public EventDataSO GetRandomEvent()
    {
        if (EventEntries == null || EventEntries.Count == 0) return null;

        int totalWeight = 0;
        foreach (var entry in EventEntries) totalWeight += entry.Weight;

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var entry in EventEntries)
        {
            // 2. 누적 가중치 계산 (entry.Weight 사용)
            currentWeight += entry.Weight;

            if (randomValue < currentWeight) 
            {
                // 3. 실제 데이터(EventDataSO) 반환
                return entry.EventData;
            }
        }

        // 4. 마지막 요소 반환 (오차 대비)
        return EventEntries[EventEntries.Count - 1].EventData;
    }
}