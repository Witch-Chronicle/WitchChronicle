using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 전체 게임 데이터 저장 / 로드 / 리셋 관리자
/// - 어디서든 SaveManager.RequestSave(); 만 부르면 프레임 끝에 안전하게 자동 저장됩니다.
/// - 로드 중에는 자동 세이브가 덮어써지지 않도록 차단 처리되어 있습니다.
/// - 숫자 0 키를 누르면 세이브 파일(save.json)을 삭제하고 Boot 씬부터 완전히 깨끗하게 리셋합니다.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("저장 설정")]
    [Tooltip("자동 저장 주기 (초 단위)")]
    [SerializeField] private float _autoSaveInterval = 60f;

    [Header("디버그 / 리셋 키 설정")]
    [Tooltip("이 키를 누르면 세이브 파일이 삭제되고 첫 씬(Boot)으로 완벽 초기화됩니다 (기본: 숫자 0키)")]
    [SerializeField] private KeyCode _resetToNewGameKey = KeyCode.Alpha0;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "save.json");

    // 이번 프레임에 세이브 요청이 있었는지 나타내는 플래그 (프레임 병합 최적화용)
    private bool _isSavePending;

    // 💡 [핵심] 데이터를 로드하는 동안 세이브가 덮어씌워지는 것을 방지하는 차단 플래그
    private bool _isLoadingData;

    // 에셋 자동 조회를 위한 딕셔너리 캐시
    private readonly Dictionary<int, ItemData> _itemDatabase = new();
    private readonly Dictionary<string, SkillData> _skillDatabase = new();

    public SaveData CurrentSaveData { get; private set; }

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

        Application.quitting += OnApplicationQuit;
    }

    private void OnDestroy()
    {
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
            // 💡 로드 중이거나 데이터 복원 중일 때는 자동으로 세이브되는 것을 완전히 차단!
            if (Instance._isLoadingData) return;

            Instance._isSavePending = true;
        }
    }

    private void Update()
    {
        bool isResetPressed = false;

        // 숫자 0 키 감지 (신형 & 구형 Input 모두 지원)
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            isResetPressed = true;
        }
#endif

        if (Input.GetKeyDown(_resetToNewGameKey))
        {
            isResetPressed = true;
        }

        // 숫자 0 키를 누르면 세이브 파일 삭제 후 Boot 씬 완벽 재시작
        if (isResetPressed)
        {
            ResetByDeletingFile();
        }
    }

    /// <summary>
    /// 한 프레임에 여러 번 RequestSave()가 호출되어도
    /// 프레임 맨 끝(LateUpdate)에서 파일을 딱 1번만 저장하여 렉을 방지합니다.
    /// </summary>
    private void LateUpdate()
    {
        if (_isSavePending && !_isLoadingData)
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

    private void OnApplicationPause(bool pause)
    {
        if (pause && !_isLoadingData)
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
    // RESET BY DELETING FILE & RESTART (완벽 리셋)
    // =================================================================
    [ContextMenu("Reset By Deleting File (파일 삭제 후 Boot 리셋)")]
    public void ResetByDeletingFile()
    {
        _isSavePending = false;
        _isLoadingData = false;
        CancelInvoke(nameof(AutoSaveTick));

        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("<color=red>[SaveManager] save.json 세이브 파일 삭제 완료!</color>");
        }

        if (Application.isPlaying)
        {
            Time.timeScale = 1f;
            DestroyPersistentObjects();
            SceneManager.LoadScene(0);
        }
    }

    private void DestroyPersistentObjects()
    {
        PersistentRoot[] roots = FindObjectsByType<PersistentRoot>(FindObjectsSortMode.None);
        foreach (var root in roots)
        {
            if (root != null) Destroy(root.gameObject);
        }

        if (PlayerInventory.Instance != null) Destroy(PlayerInventory.Instance.gameObject);
        if (PersistentCharacterManager.Instance != null) Destroy(PersistentCharacterManager.Instance.gameObject);
        if (QuestManager.Instance != null) Destroy(QuestManager.Instance.gameObject);
        if (UIBackgroundBlurManager.Instance != null) Destroy(UIBackgroundBlurManager.Instance.gameObject);

        Destroy(gameObject);
    }

    // =================================================================
    // SAVE ALL
    // =================================================================
    public void SaveAll()
    {
        if (_isLoadingData) return;

        try
        {
            SaveData data = new SaveData();
            data.Version = 1;


             if (WitchChronicle.IdleFarming.PlotManager.Instance != null)
            {
                // 마을 씬에 PlotManager가 있을 때: 현재 밭 상태를 최신으로 저장
                data.FarmPlots = WitchChronicle.IdleFarming.PlotManager.Instance.GetFarmSaveData();
            }
            else if (CurrentSaveData != null && CurrentSaveData.FarmPlots != null && CurrentSaveData.FarmPlots.Count > 0)
            {
                // 던전/전투 씬이라 PlotManager가 없을 때: 마을에서 저장했던 밭 데이터를 삭제하지 않고 그대로 유지!
                data.FarmPlots = new List<WitchChronicle.IdleFarming.PlotSaveData>(CurrentSaveData.FarmPlots);
            }

            // 1. 재화 및 인벤토리 저장
            if (PlayerInventory.Instance != null)
            {
                data.Gold = PlayerInventory.Instance.Gold;

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

                foreach (var kvp in itemTotals)
                {
                    data.InventoryItems.Add(new ItemStackSaveData
                    {
                        ItemId = kvp.Key,
                        Quantity = kvp.Value
                    });
                }

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

                    if (unit.CharacterVitals != null)
                    {
                        charData.CurrentHp = unit.CharacterVitals.CurrentHp;
                        charData.CurrentMp = unit.CharacterVitals.CurrentMp;
                    }

                    if (unit.PlayerSkillLoadout != null)
                    {
                        foreach (var skill in unit.PlayerSkillLoadout.EquippedSkills)
                        {
                            if (skill != null) charData.EquippedSkillIds.Add(skill.SkillId);
                        }
                    }

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

            if (SoundManager.Instance != null)
            {
                data.MasterVolume = SoundManager.Instance.MasterVolume;
                data.BgmVolume = SoundManager.Instance.BgmVolume;
                data.SfxVolume = SoundManager.Instance.SfxVolume;

                data.IsMasterMuted = SoundManager.Instance.IsMasterMuted;
                data.IsBgmMuted = SoundManager.Instance.IsBgmMuted;
                data.IsSfxMuted = SoundManager.Instance.IsSfxMuted;
            }

            CurrentSaveData = data;

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
        // 💡 [핵심] 로드 작업 시작 시 자동 세이브 요청을 전면 차단합니다.
        _isLoadingData = true;

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

            CurrentSaveData = data;

            // 1. 농장 밭 데이터 복원
            if (WitchChronicle.IdleFarming.PlotManager.Instance != null)
            {
                WitchChronicle.IdleFarming.PlotManager.Instance.LoadFarmSaveData(data.FarmPlots);
            }

            // 2. 습득 스킬 복원
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

            // 3. 인벤토리 및 장비 복원
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
                    EnhanceController enhanceCtrl = FindFirstObjectByType<EnhanceController>();

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

                                if (enhanceCtrl != null)
                                {
                                    EnhanceTableData table = enhanceCtrl.GetTable(equipItem.itemGrade);
                                    lastAdded.RefreshStats(table);
                                }
                            }
                        }
                    }
                }

                PlayerInventory.Instance.RaiseInventoryChanged();
            }

            // 4. 캐릭터 스탯, 장비, 스킬, 파티 복원
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

                            stats.SetLevelAndExp(charSave.Level, charSave.Exp, charSave.AvailableStatPoints);

                            // [수정] 포인트 부족 검사에 걸리지 않고 투자 스탯을 바로 복원!
                            stats.SetAllocatedStats(
                                charSave.AvailableStatPoints,
                                charSave.AllocatedHp,
                                charSave.AllocatedMp,
                                charSave.AllocatedSpellPower,
                                charSave.AllocatedIntelligence,
                                charSave.AllocatedDefense,
                                charSave.AllocatedSpeed,
                                charSave.AllocatedLuck
                            );

                            stats.ResetAllocatedStats();

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

            // 5. 퀘스트 상태 복원
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

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetMasterVolume(data.MasterVolume);
                SoundManager.Instance.SetBgmVolume(data.BgmVolume);
                SoundManager.Instance.SetSfxVolume(data.SfxVolume);

                SoundManager.Instance.SetMasterMuted(data.IsMasterMuted);
                SoundManager.Instance.SetBgmMuted(data.IsBgmMuted);
                SoundManager.Instance.SetSfxMuted(data.IsSfxMuted);
            }

            Debug.Log("<color=green>[SaveManager] 로드 완료!</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 로드 중 예외 발생 : {ex}");
        }
        finally
        {
            // 💡 [핵심] 모든 데이터 복원이 안전하게 끝난 뒤 세이브 요청 차단을 풀어줍니다.
            _isLoadingData = false;
            _isSavePending = false;
        }
    }
}