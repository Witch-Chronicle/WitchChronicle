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

        if (table == null)
        {
            return results;
        }

        foreach (DropEntry entry in table.drops)
        {
            if (entry.item == null)
            {
                continue;
            }

            if (!IsDropSuccess(entry))
            {
                continue;
            }

            int amount = GetDropAmount(entry);

            results.Add(new DropResult(entry.item, amount));
        }

        return results;
    }
    
    private bool IsDropSuccess(DropEntry entry)
    {
        float roll = Random.Range(0f, 100f);

        return roll <= entry.chance;
    }

    private int GetDropAmount(DropEntry entry)
    {
        return Random.Range(entry.minAmount, entry.maxAmount + 1);
    }
}