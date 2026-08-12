using System.Collections.Generic;
using UnityEngine;

public class DropManager : MonoBehaviour
{
    public static DropManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 드롭 확률 정하기
    /// </summary>
    /// <param name="table">드롭 테이블</param>
    /// <returns></returns>
    public List<DropResult> RollDrop(DropTable table)
    {
        List<DropResult> results = new List<DropResult>();

        if (table == null || table.drops == null || table.drops.Count == 0)
        {
            return results;
        }

        // 1. 유효한 항목들의 전체 가중치(Total Weight) 합산
        float totalWeight = 0f;

        foreach (DropEntry entry in table.drops)
        {
            if (entry.item != null && entry.chance > 0)
            {
                totalWeight += entry.chance; // chance 필드를 가중치(Weight)로 활용
            }
        }

        // 가중치 합이 0 이하인 경우 드롭 없음
        if (totalWeight <= 0f)
        {
            return results;
        }

        // 2. 0 ~ totalWeight 사이의 무작위 값 추출
        float roll = Random.Range(0f, totalWeight);
        float accumulatedWeight = 0f;

        // 3. 가중치 누적(Accumulated Weight)을 비교하여 아이템 결정
        foreach (DropEntry entry in table.drops)
        {
            if (entry.item == null || entry.chance <= 0)
            {
                continue;
            }

            accumulatedWeight += entry.chance;

            // 롤 값이 현재 누적 가중치 이하이면 해당 아이템 당첨
            if (roll <= accumulatedWeight)
            {
                results.Add(new DropResult(entry.item, 1));
                break; // 1개 당첨 후 종료
            }
        }

        return results;
    }

}