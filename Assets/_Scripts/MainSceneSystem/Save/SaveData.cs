using System;
using System.Collections.Generic;

/// <summary>
/// 전체 저장 파일 구조 (JSON 루트)
/// </summary>
[Serializable]
public class SaveData
{
    public int Version = 1;

    public List<WitchChronicle.IdleFarming.PlotSaveData> FarmPlots = new();

    // 플레이어 재화 데이터
    public int Gold;

    // 파티 및 캐릭터 데이터
    public List<string> ActivePartyIds = new();
    public List<CharacterSaveData> Characters = new();

    // 인벤토리 & 장비 인스턴스 데이터
    public List<ItemStackSaveData> InventoryItems = new();
    public List<EquipmentInstanceSaveData> EquipmentInstances = new();

    // 습득 스킬 목록 (스킬 ID)
    public List<string> LearnedSkillIds = new();

    // 퀘스트 진행 상태
    public List<QuestProgressSaveData> Quests = new();
}

/// <summary>
/// 개별 캐릭터 저장 데이터
/// </summary>
[Serializable]
public class CharacterSaveData
{
    public string CharacterId;
    public bool IsRecruited;

    public int Level;
    public int Exp;
    public int AvailableStatPoints;

    // 투자 스탯
    public int AllocatedHp;
    public int AllocatedMp;
    public int AllocatedSpellPower;
    public int AllocatedIntelligence;
    public int AllocatedDefense;
    public int AllocatedSpeed;
    public int AllocatedLuck;

    // 현재 자원
    public int CurrentHp;
    public int CurrentMp;

    // 장착 스킬 ID 목록
    public List<string> EquippedSkillIds = new();

    // 슬롯별 장착 장비 정보
    public List<EquippedSlotSaveData> EquippedItems = new();
}

/// <summary>
/// 장착 장비 슬롯 저장 데이터
/// </summary>
[Serializable]
public class EquippedSlotSaveData
{
    public string SlotType;         // EquipSlotType (Weapon, Robe 등)
    public int ItemId;              // 아이템 ID
    public int EnhanceLevel;        // 강화 단계
    public int EnhanceAttemptCount; // 강화 시도 횟수
}

/// <summary>
/// 일반 소모품/재료 아이템 저장 데이터
/// </summary>
[Serializable]
public class ItemStackSaveData
{
    public int ItemId;
    public int Quantity;
}

/// <summary>
/// 인벤토리 보유 장비 인스턴스 저장 데이터
/// </summary>
[Serializable]
public class EquipmentInstanceSaveData
{
    public int ItemId;
    public int EnhanceLevel;
    public int EnhanceAttemptCount;
}

/// <summary>
/// 퀘스트 진행도 저장 데이터
/// </summary>
[Serializable]
public class QuestProgressSaveData
{
    public string QuestId;
    public int State;                           // QuestState (Running, Completed, Rewarded)
    public List<int> ObjectiveProgress = new(); // 목표별 진행 수치
}