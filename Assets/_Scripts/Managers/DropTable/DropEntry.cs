using UnityEngine;

[System.Serializable]
public class DropEntry
{
    public ItemData item;

    [Range(0f, 100f)]
    public float chance;

    public int minAmount = 1;

    public int maxAmount = 1;
}