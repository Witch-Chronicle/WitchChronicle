using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 전체 게임 데이터 저장 / 로드 관리자
/// 어디서든 SaveManager.RequestSave(); 만 부르면 프레임 끝에 안전하게 자동 저장됩니다.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("저장 설정")]
    [Tooltip("자동 저장 주기 (초 단위)")]
    [SerializeField] private float _autoSaveInterval = 60f;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "save.json");

    // 이번 프레임에 세이브 요청이 있었는지 나타내는 플래그 (프레임 병합용)
    private bool _isSavePending;

    // 에셋 자동 조회를 위한 딕셔너리 캐시
    private readonly Dictionary<int, ItemData> _itemDatabase = new();
    private readonly Dictionary<string, SkillData> _skillDatabase = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildAssetDatabases();

        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting += OnApplicationQuit;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= OnApplicationQuit;
    }

    private void Start()
    {
        LoadAll();
        InvokeRepeating(nameof(AutoSaveTick), _autoSaveInterval, _autoSaveInterval);

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += RequestSave;
            PlayerInventory.Instance.OnGoldChanged += (gold) => RequestSave();
        }
    }

    /// <summary>
    /// 프로젝트 어디서든 저장이 필요할 때 부르는 단 하나의 통일된 세이브 요청 함수!
    /// </summary>
    public static void RequestSave()
    {
        if (Instance != null)
        {
            Instance._isSavePending = true;
        }
    }

    /// <summary>
    /// 한 프레임에 여러 번 RequestSave()가 호출되어도
    /// 프레임 맨 끝(LateUpdate)에서 파일을 딱 1번만 저장하여 렉을 방지합니다.
    /// </summary>
    private void LateUpdate()
    {
        if (_isSavePending)
        {
            _isSavePending = false;
            SaveAll();
        }
    }

    private void AutoSaveTick()
    {
        Debug.Log("[SaveManager] 주기적 자동 저장 실행...");
        RequestSave();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneId.Main.ToString() || scene.name == SceneId.Dungeon.ToString())
        {
            RequestSave();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("[SaveManager] 앱 일시정지/백그라운드 전환 감지 - 저장 실행");
            SaveAll();
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[SaveManager] 앱 종료 - 최종 저장 실행");
        SaveAll();
    }

    /// <summary>
    /// Resources 폴더 안의 모든 ItemData 및 SkillData 에셋을 수집하여 ID 매핑
    /// </summary>
    private void BuildAssetDatabases()
    {
        _itemDatabase.Clear();
        ItemData[] items = Resources.LoadAll<ItemData>("");
        foreach (ItemData item in items)
        {
            if (item != null && !_itemDatabase.ContainsKey(item.itemId))
            {
                _itemDatabase.Add(item.itemId, item);
            }
        }

        _skillDatabase.Clear();
        SkillData[] skills = Resources.LoadAll<SkillData>("");
        foreach (SkillData skill in skills)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.SkillId) && !_skillDatabase.ContainsKey(skill.SkillId))
            {
                _skillDatabase.Add(skill.SkillId, skill);
            }
        }

        Debug.Log($"<color=yellow>[SaveManager] Resources에서 찾은 아이템: {_itemDatabase.Count}개, 스킬: {_skillDatabase.Count}개</color>");
    }

    // =================================================================
    // SAVE ALL
    // =================================================================
    public void SaveAll()
    {
        try
        {
            SaveData data = new SaveData();
            data.Version = 1;

             // 농사 밭 세이브 데이터 저장
            if (WitchChronicle.IdleFarming.PlotManager.Instance != null)
            {
                data.FarmPlots = WitchChronicle.IdleFarming.PlotManager.Instance.GetFarmSaveData();
            }

            // 1. 재화 및 인벤토리 저장
            if (PlayerInventory.Instance != null)
            {
                data.Gold = PlayerInventory.Instance.Gold;

                // 💡 [핵심] 동일한 ItemId를 가진 아이템은 수량(Quantity)을 하나로 합쳐서 저장!
                Dictionary<int, int> itemTotals = new Dictionary<int, int>();

                foreach (var slot in PlayerInventory.Instance.InventorySlots)
                {
                    if (slot != null && slot.ItemData != null)
                    {
                        int id = slot.ItemData.itemId;
                        if (!itemTotals.ContainsKey(id))
                        {
                            itemTotals[id] = 0;
                        }
                        itemTotals[id] += slot.Quantity;
                    }
                }

                // 합쳐진 수량으로 세이브 데이터 생성
                foreach (var kvp in itemTotals)
                {
                    data.InventoryItems.Add(new ItemStackSaveData
                    {
                        ItemId = kvp.Key,
                        Quantity = kvp.Value
                    });
                }

                // 보유 장비 인스턴스 (장비는 개별 강화 수치가 있으므로 기존 유지)
                foreach (var eq in PlayerInventory.Instance.EquipmentInstances)
                {
                    if (eq != null && eq.baseData != null)
                    {
                        data.EquipmentInstances.Add(new EquipmentInstanceSaveData
                        {
                            ItemId = eq.baseData.itemId,
                            EnhanceLevel = eq.enhanceLevel,
                            EnhanceAttemptCount = eq.enhanceAttemptCount
                        });
                    }
                }
            }

            // 2. 캐릭터 및 파티 저장
            if (PersistentCharacterManager.Instance != null)
            {
                data.ActivePartyIds.AddRange(PersistentCharacterManager.Instance.ActivePartyCharacterIds);

                foreach (var unit in PersistentCharacterManager.Instance.AllCharacters)
                {
                    if (unit == null) continue;

                    CharacterSaveData charData = new CharacterSaveData();
                    charData.CharacterId = unit.CharacterId;
                    charData.IsRecruited = unit.IsRecruited;

                    // 스탯 & 레벨
                    if (unit.StatController != null && unit.StatController.Stats != null)
                    {
                        CharacterStats stats = unit.StatController.Stats;
                        charData.Level = stats.Level;
                        charData.Exp = stats.Exp;
                        charData.AvailableStatPoints = stats.AvailableStatPoints;

                        charData.AllocatedHp = stats.AllocatedHp;
                        charData.AllocatedMp = stats.AllocatedMp;
                        charData.AllocatedSpellPower = stats.AllocatedSpellPower;
                        charData.AllocatedIntelligence = stats.AllocatedIntelligence;
                        charData.AllocatedDefense = stats.AllocatedDefense;
                        charData.AllocatedSpeed = stats.AllocatedSpeed;
                        charData.AllocatedLuck = stats.AllocatedLuck;
                    }

                    // 체력 / 마나
                    if (unit.CharacterVitals != null)
                    {
                        charData.CurrentHp = unit.CharacterVitals.CurrentHp;
                        charData.CurrentMp = unit.CharacterVitals.CurrentMp;
                    }

                    // 장착 스킬
                    if (unit.PlayerSkillLoadout != null)
                    {
                        foreach (var skill in unit.PlayerSkillLoadout.EquippedSkills)
                        {
                            if (skill != null) charData.EquippedSkillIds.Add(skill.SkillId);
                        }
                    }

                    // 장착 장비
                    if (unit.CharacterEquipment != null)
                    {
                        foreach (EquipSlotType slotType in Enum.GetValues(typeof(EquipSlotType)))
                        {
                            EquipmentInstance eq = unit.CharacterEquipment.GetEquipped(slotType);
                            if (eq != null && eq.baseData != null)
                            {
                                charData.EquippedItems.Add(new EquippedSlotSaveData
                                {
                                    SlotType = slotType.ToString(),
                                    ItemId = eq.baseData.itemId,
                                    EnhanceLevel = eq.enhanceLevel,
                                    EnhanceAttemptCount = eq.enhanceAttemptCount
                                });
                            }
                        }
                    }

                    data.Characters.Add(charData);
                }
            }

            // 3. 습득 스킬 저장
            if (SkillInventory.Instance != null)
            {
                foreach (var skill in SkillInventory.Instance.LearnedSkills)
                {
                    if (skill != null) data.LearnedSkillIds.Add(skill.SkillId);
                }
            }

            // 4. 퀘스트 진행도 저장
            if (QuestManager.Instance != null)
            {
                var runningQuests = QuestManager.Instance.GetRunningQuests();
                foreach (var quest in runningQuests)
                {
                    if (quest == null || quest.Data == null) continue;

                    QuestProgressSaveData questSave = new QuestProgressSaveData
                    {
                        QuestId = quest.Data.id,
                        State = (int)quest.State
                    };

                    for (int i = 0; i < quest.Data.objectives.Count; i++)
                    {
                        int prog = quest.Progress.ContainsKey(i) ? quest.Progress[i] : 0;
                        questSave.ObjectiveProgress.Add(prog);
                    }

                    data.Quests.Add(questSave);
                }
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"<color=cyan>[SaveManager] 저장 성공!</color> : {SaveFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 저장 중 예외 발생 : {ex}");
        }
    }

    // =================================================================
    // LOAD ALL
    // =================================================================
    public void LoadAll()
    {
        try
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.Log("[SaveManager] 세이브 파일이 존재하지 않아 신규 진행합니다.");
                return;
            }

            string json = File.ReadAllText(SaveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null) return;

            if (WitchChronicle.IdleFarming.PlotManager.Instance != null)
            {
                WitchChronicle.IdleFarming.PlotManager.Instance.LoadFarmSaveData(data.FarmPlots);
            }

            // 1. 습득 스킬 복원
            if (SkillInventory.Instance != null && data.LearnedSkillIds != null)
            {
                foreach (string skillId in data.LearnedSkillIds)
                {
                    if (_skillDatabase.TryGetValue(skillId, out SkillData skill))
                    {
                        SkillInventory.Instance.TryLearnSkill(skill);
                    }
                }
            }

            // 2. 인벤토리 및 장비 복원
            if (PlayerInventory.Instance != null)
            {
                int currentGold = PlayerInventory.Instance.Gold;
                int goldDiff = data.Gold - currentGold;
                if (goldDiff > 0) PlayerInventory.Instance.AddGold(goldDiff);
                else if (goldDiff < 0) PlayerInventory.Instance.TrySpendGold(-goldDiff);

                if (data.InventoryItems != null)
                {
                    foreach (var itemSave in data.InventoryItems)
                    {
                        if (_itemDatabase.TryGetValue(itemSave.ItemId, out ItemData itemData))
                        {
                            PlayerInventory.Instance.AddItem(itemData, itemSave.Quantity);
                        }
                    }
                }

                if (data.EquipmentInstances != null)
                {
                    foreach (var eqSave in data.EquipmentInstances)
                    {
                        if (_itemDatabase.TryGetValue(eqSave.ItemId, out ItemData itemData) && itemData is EquipItemData equipItem)
                        {
                            PlayerInventory.Instance.AddEquipment(equipItem, 1);
                            var lastAdded = PlayerInventory.Instance.EquipmentInstances[PlayerInventory.Instance.EquipmentInstances.Count - 1];
                            if (lastAdded != null)
                            {
                                lastAdded.enhanceLevel = eqSave.EnhanceLevel;
                                lastAdded.enhanceAttemptCount = eqSave.EnhanceAttemptCount;

                                EnhanceController enhanceCtrl = FindFirstObjectByType<EnhanceController>();
                                if (enhanceCtrl != null)
                                {
                                    EnhanceTableData table = enhanceCtrl.GetTable(equipItem.itemGrade);
                                    lastAdded.RefreshStats(table);
                                }
                            }
                        }
                    }
                }
            }

            // 3. 캐릭터 스탯, 장비, 스킬, 파티 복원
            if (PersistentCharacterManager.Instance != null && data.Characters != null)
            {
                foreach (var charSave in data.Characters)
                {
                    if (PersistentCharacterManager.Instance.TryGetCharacter(charSave.CharacterId, out PersistentCharacterUnit unit))
                    {
                        unit.SetRecruited(charSave.IsRecruited);

                        if (unit.StatController != null && unit.StatController.Stats != null)
                        {
                            CharacterStats stats = unit.StatController.Stats;

                            // 💡 [핵심] 불러온 세이브 데이터의 레벨, 경험치, 남은 스탯 포인트를 캐릭터에 복원!
                            stats.SetLevelAndExp(charSave.Level, charSave.Exp, charSave.AvailableStatPoints);

                            stats.ResetAllocatedStats();

                            // 투자했던 스탯 포인트 복원
                            stats.TryUseStatPoint(StatType.MaxHP, charSave.AllocatedHp);
                            stats.TryUseStatPoint(StatType.MaxMP, charSave.AllocatedMp);
                            stats.TryUseStatPoint(StatType.SpellPower, charSave.AllocatedSpellPower);
                            stats.TryUseStatPoint(StatType.Intelligence, charSave.AllocatedIntelligence);
                            stats.TryUseStatPoint(StatType.Defense, charSave.AllocatedDefense);
                            stats.TryUseStatPoint(StatType.Speed, charSave.AllocatedSpeed);
                            stats.TryUseStatPoint(StatType.Luck, charSave.AllocatedLuck);
                        }

                        if (unit.CharacterVitals != null)
                        {
                            unit.CharacterVitals.SetCurrentVitals(charSave.CurrentHp, charSave.CurrentMp);
                        }

                        if (unit.PlayerSkillLoadout != null && charSave.EquippedSkillIds != null)
                        {
                            unit.PlayerSkillLoadout.ClearEquippedSkills();
                            foreach (string skillId in charSave.EquippedSkillIds)
                            {
                                if (_skillDatabase.TryGetValue(skillId, out SkillData skill))
                                {
                                    unit.PlayerSkillLoadout.TryEquipSkill(skill);
                                }
                            }
                        }

                        if (unit.CharacterEquipment != null && charSave.EquippedItems != null)
                        {
                            foreach (var eqSlotSave in charSave.EquippedItems)
                            {
                                if (Enum.TryParse(eqSlotSave.SlotType, out EquipSlotType slotType) &&
                                    _itemDatabase.TryGetValue(eqSlotSave.ItemId, out ItemData itemData) &&
                                    itemData is EquipItemData equipItemData)
                                {
                                    EquipmentInstance eqInstance = new EquipmentInstance(equipItemData, eqSlotSave.EnhanceLevel, null);
                                    unit.CharacterEquipment.Equip(eqInstance);
                                }
                            }
                        }
                    }
                }

                if (data.ActivePartyIds != null && data.ActivePartyIds.Count > 0)
                {
                    PersistentCharacterManager.Instance.SetActivePartyOrder(data.ActivePartyIds);
                }
            }
            // 4. 퀘스트 상태 복원
            if (QuestManager.Instance != null && data.Quests != null)
            {
                foreach (var questSave in data.Quests)
                {
                    QuestRuntime runtime = QuestManager.Instance.GetQuest(questSave.QuestId);
                    if (runtime == null)
                    {
                        QuestManager.Instance.StartQuest(questSave.QuestId);
                        runtime = QuestManager.Instance.GetQuest(questSave.QuestId);
                    }

                    if (runtime != null)
                    {
                        runtime.State = (QuestState)questSave.State;

                        for (int i = 0; i < questSave.ObjectiveProgress.Count; i++)
                        {
                            if (runtime.Progress.ContainsKey(i))
                            {
                                runtime.Progress[i] = questSave.ObjectiveProgress[i];
                            }
                        }
                    }
                }
            }

            Debug.Log("<color=green>[SaveManager] 로드 완료!</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 로드 중 예외 발생 : {ex}");
        }
    }
}