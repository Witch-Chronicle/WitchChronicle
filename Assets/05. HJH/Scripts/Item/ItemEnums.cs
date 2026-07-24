/// <summary>
/// 아이템 대분류
/// </summary>
public enum ItemType
{
    Consumable, // 소비 아이템
    Material,   // 재료 아이템
    Equipment,  // 장비 아이템
    KeyItem,    // 스토리/퀘스트 전용 아이템
    SeedItem    // 씨앗 아이템
}

/// <summary>
/// 소비 아이템 세부 종류
/// </summary>
public enum ConsumableType
{
    HealHP,     // HP 회복
    HealMP,     // MP 회복
    CureStatus, // 상태이상 치료
    Buff,       // 버프 효과
    Grimoire    // 마도서 (스킬/스킬조각 가챠)
}

/// <summary>
/// 재료 아이템 세부 종류 (필요 없으면 사용하지 않아도 됨)
/// </summary>
public enum MaterialType
{
    Herb, // 허브
    Crop, // 작물
    Fish, // 생성
    Ore,        // 광물
    Stone,      // 강화석
    MonsterDrop // 몬스터 드롭 아이템
}

/// <summary>
/// 무기 세부 종류. 프로젝트 상황에 맞게 수정해서 사용
/// </summary>
public enum WeaponType
{
    Staff,  // 지팡이
    Wand,   // 완드
    Sword   // 마검
}

/// <summary>
/// 아이템 등급
/// </summary>
public enum ItemGradeType
{
    Common,
    UnCommon,
    Rare,
    Unique,
    Legendary
}

/// <summary>
/// 무기의 전용 캐릭터 표기용. 실제로는 CharacterData(SO)를 참조하는 편이
/// 더 유연하지만, 우선 enum으로 단순화해둠. 필요 없으면 WeaponItemData에서
/// ownerCharacter 필드 자체를 삭제해도 무방.
/// </summary>
public enum OwnerCharacter
{
    None,   // 전용 캐릭터 없음 (공용 무기)
    Ariel,  // 아리엘
    Ria,    // 라이아
    Fay     // 페이
}