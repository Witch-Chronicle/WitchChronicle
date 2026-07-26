using UnityEngine;

public enum SeedCategory { Jagmul, Yakcho, Rare }


[CreateAssetMenu(fileName = "NewSeed", menuName = "WitchChronicle/Seed Data")]
public class SeedData : ScriptableObject
{
    public string seedName;          // 감자 씨앗
    public string harvestName;       // 감자
    public SeedCategory category;    // Crop / Herb / Rare
    public float growthTime;         // 성장시간 (초) - 5분=300
    public Sprite seedSprite;        // 씨앗 이미지
    public Sprite sproutSprite;      // 새싹 이미지
    public Sprite harvestSprite;     // 다 자란 이미지

    [Header("수확물")]
    public ItemData harvestItem;     // 수확 시 인벤토리에 추가할 아이템
    public int harvestAmount = 1;    // 수확 개수
}