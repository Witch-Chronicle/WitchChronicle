using UnityEngine;

/// <summary>
/// 씨앗 아이템
/// SeedData를 가지고 있는 데이터
/// </summary>
[CreateAssetMenu(fileName = "NewSeedItem", menuName = "Witch Chronicle/Item/SeedItemData")]
public class SeedItemData : ItemData
{
    [Header("씨앗 아이템 데이터")]
    public SeedData seedData; // 재료 종류 (필요 없으면 삭제 가능)
}