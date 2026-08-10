public enum QuestObjectiveType // 퀘스트 목표 정의
{
    None,

    TalkNPC,

    KillMonster,

    CollectItem,

    ClearDungeon,

    RecruitNPC,

    // ===== 사이드 퀘스트용 신규 목표 =====
    PlantSeed,      // 씨앗 심기
    HarvestCrop,    // 농작물 수확
    CatchFish,      // 낚시 성공
    CookFood,       // 요리 제작
    BrewPotion,     // 포션 제조
    SellItem,       // 아이템 판매
    SellEquipment,  // 장비 판매
    UseShop,        // 상점에서 구매 1회 이상
    EnhanceItem,    // 강화 시도 1회 이상
    ResetStat,      // 스탯 초기화
    gatcha,         // 가챠 1회 이상
    
}