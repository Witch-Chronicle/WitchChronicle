using UnityEngine;

[System.Serializable]
public class DropResult
{
    public ItemData item;

    public int amount;

    public DropResult(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}