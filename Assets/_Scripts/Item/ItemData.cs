using UnityEngine;

/// <summary>
/// 모든 아이템이 공통적으로 가지는 데이터.
/// 실제 개별 아이템 SO(ConsumableItemData 등)는 이 클래스를 상속받아 사용한다.
/// </summary>
public abstract class ItemData : ScriptableObject
{
    [Header("공통 정보")]
    public int itemId;                 // 아이템 고유 ID
    public string itemName;            // 아이템 이름

    [TextArea(2, 5)]
    public string description;         // 아이템 설명

    public ItemType itemType;          // 아이템 종류
    public ItemGradeType itemGrade;    // 아이템 등급
    public Sprite icon;                // 아이콘

    [Header("인벤토리/상점 카테고리")]
    public MainCategory mainCategory;
    public SubCategory subCategory;

    [Header("스택 / 거래")]
    public int maxStack = 1;           // 최대 중첩 개수

    [Tooltip("드롭 전용 획득 아이템은 0으로 설정")]
    public int buyPrice;               // 구매 가격
    public int sellPrice;              // 판매 가격
    public bool canSell = true;        // 판매 가능 여부
}