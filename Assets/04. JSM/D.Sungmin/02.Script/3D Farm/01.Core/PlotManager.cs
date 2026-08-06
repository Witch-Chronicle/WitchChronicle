using System.Collections.Generic;
using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    public class PlotManager : MonoBehaviour
    {
        public static PlotManager Instance { get; private set; }

        [Header("설정")]
        [SerializeField] private PlotConfig _config;
        [SerializeField] private List<SeedData> _allSeeds = new List<SeedData>();
        [SerializeField] private int _maxOfflineCycles = 5;

        [Header("UI Panels")]
        [SerializeField] private PlotUnlockPanel _unlockPanel;
        [SerializeField] private PlotSeedSelectPanel _seedSelectPanel;
        [SerializeField] private PlotHarvestPanel _harvestPanel;

        [Header("Floating UI (Screen Space)")]
        [SerializeField] private PlotFloatingUI _floatingUIPrefab;
        [SerializeField] private RectTransform _hudCanvasRoot;

        [Header("팝업 중 잠글 설정")]
        [Tooltip("플레이어 태그 (Player Controller가 붙은 오브젝트)")]
        [SerializeField] private string _playerTag = "Player";
        [Tooltip("잠글 스크립트 이름 목록 (Ariel에 붙은 컴포넌트 이름들)")]
        [SerializeField] private List<string> _lockScriptNames = new List<string>
        {
            "PlayerController",
            "FieldTargetingController",
            "FieldAttackController"
        };
        [Tooltip("CharacterController도 잠글지")]
        [SerializeField] private bool _lockCharacterController = true;
        [Tooltip("Animator Speed 파라미터명 (없거나 다르면 조정)")]
        [SerializeField] private string _animatorSpeedParam = "Speed";

        private readonly List<PlotSlot> _slots = new List<PlotSlot>();
        private readonly List<PlotFloatingUI> _floatingUIs = new List<PlotFloatingUI>();
        private int _openPanelCount = 0;

        // 런타임에 찾은 플레이어 참조 (캐싱)
        private GameObject _cachedPlayer;
        private List<Behaviour> _cachedLockComponents = new List<Behaviour>();
        private CharacterController _cachedCharacterController;
        private Animator _cachedAnimator;

        public PlotConfig Config => _config;
        public int AllSeedsCount => _allSeeds.Count;
        public int MaxOfflineCycles => _maxOfflineCycles;
        public PlotUnlockPanel UnlockPanel => _unlockPanel;
        public PlotSeedSelectPanel SeedSelectPanel => _seedSelectPanel;
        public PlotHarvestPanel HarvestPanel => _harvestPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            AutoRegisterPlots();
        }

        // ====== 슬롯 등록 ======

        private void AutoRegisterPlots()
        {
            _slots.Clear();
            _floatingUIs.Clear();

            var found = FindObjectsOfType<PlotSlot>();
            System.Array.Sort(found, (a, b) => a.PlotIndex.CompareTo(b.PlotIndex));
            _slots.AddRange(found);

            foreach (var slot in _slots)
            {
                slot.OnHarvested += HandleSlotHarvested;
                SpawnFloatingUI(slot);
            }

            Debug.Log($"[PlotManager] {_slots.Count}개 슬롯 등록됨");
        }

        private void SpawnFloatingUI(PlotSlot slot)
        {
            if (_floatingUIPrefab == null || _hudCanvasRoot == null)
            {
                Debug.LogWarning("[PlotManager] FloatingUI 프리팹 또는 HUD Canvas 미설정");
                return;
            }

            var ui = Instantiate(_floatingUIPrefab, _hudCanvasRoot);
            ui.name = $"PlotFloatingUI_{slot.PlotIndex:D2}";
            slot.SetFloatingUI(ui);
            _floatingUIs.Add(ui);
        }

        private void HandleSlotHarvested(PlotSlot slot, SeedData seed, int amount)
        {
            if (seed == null || seed.harvestItem == null || amount <= 0) return;

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddItem(seed.harvestItem, amount);
                Debug.Log($"[PlotManager] 수확: {seed.harvestName} x{amount}");
            }
            else
            {
                Debug.LogWarning("[PlotManager] PlayerInventory.Instance 없음, 수확 지급 실패");
            }
        }

        // ====== 초기화 ======

        private void InitializeFresh()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var saveData = new PlotSaveData(_slots[i].PlotIndex);
                saveData.state = _slots[i].PlotIndex < _config.initialUnlockedCount
                    ? PlotState.Empty : PlotState.Locked;
                saveData.SetCycleStartTime(System.DateTime.Now);
                _slots[i].Initialize(saveData, null);
            }
        }

        // ====== 조회 ======

        public SeedData GetSeedAt(int index)
        {
            if (index < 0 || index >= _allSeeds.Count) return null;
            return _allSeeds[index];
        }

        public SeedData FindSeedByName(string seedName)
        {
            if (string.IsNullOrEmpty(seedName)) return null;
            foreach (var s in _allSeeds)
                if (s != null && s.name == seedName) return s;
            return null;
        }

        public int GetUnlockCost(int plotIndex)
        {
            int priceIndex = plotIndex - _config.initialUnlockedCount;
            if (priceIndex < 0) return 0;
            if (_config.unlockPrices == null || priceIndex >= _config.unlockPrices.Length)
                return 999999;
            return _config.unlockPrices[priceIndex];
        }

        // ====== 플레이어 존 진입/이탈 ======

        public void SetAllFloatingUIsPlayerNear(bool near)
        {
            for (int i = 0; i < _floatingUIs.Count; i++)
            {
                if (_floatingUIs[i] != null)
                    _floatingUIs[i].SetPlayerNear(near);
            }
        }

        public void SetFloatingUINearBySlot(PlotSlot slot, bool near)
        {
            if (slot == null) return;

            int index = _slots.IndexOf(slot);
            if (index < 0 || index >= _floatingUIs.Count) return;

            if (_floatingUIs[index] != null)
                _floatingUIs[index].SetPlayerNear(near);
        }

        public void SetFloatingUINearByIndex(int plotIndex, bool near)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && _slots[i].PlotIndex == plotIndex)
                {
                    if (i < _floatingUIs.Count && _floatingUIs[i] != null)
                        _floatingUIs[i].SetPlayerNear(near);
                    return;
                }
            }
        }

        // ====== 팝업 열림/닫힘: 커서 + 입력 잠금 ======

        public void NotifyPanelOpened()
        {
            _openPanelCount++;
            if (_openPanelCount == 1)
            {
                EnsurePlayerCached();
                LockGameplayInput(true);
            }
        }

        public void NotifyPanelClosed()
        {
            _openPanelCount = Mathf.Max(0, _openPanelCount - 1);
            if (_openPanelCount == 0)
            {
                LockGameplayInput(false);
            }
        }

        /// <summary>
        /// 플레이어가 런타임에 생성되므로 팝업 열릴 때마다 캐시 유효성 확인 후 없으면 다시 찾음
        /// </summary>
        private void EnsurePlayerCached()
        {
            if (_cachedPlayer != null && _cachedLockComponents.Count > 0) return;

            _cachedPlayer = GameObject.FindGameObjectWithTag(_playerTag);
            if (_cachedPlayer == null)
            {
                Debug.LogWarning($"[PlotManager] '{_playerTag}' 태그를 가진 플레이어를 찾을 수 없음");
                return;
            }

            _cachedLockComponents.Clear();

            // 스크립트 이름으로 컴포넌트 찾기
            var allComponents = _cachedPlayer.GetComponents<Behaviour>();
            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                string compName = comp.GetType().Name;
                if (_lockScriptNames.Contains(compName))
                {
                    _cachedLockComponents.Add(comp);
                    Debug.Log($"[PlotManager] 잠금 대상 등록: {compName}");
                }
            }

            // CharacterController
            if (_lockCharacterController)
                _cachedCharacterController = _cachedPlayer.GetComponent<CharacterController>();

            // Animator (자식 오브젝트에 있을 수도 있음)
            _cachedAnimator = _cachedPlayer.GetComponentInChildren<Animator>();
        }

        private void LockGameplayInput(bool locked)
        {
            // 커서 처리
            if (locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // 플레이어 스크립트들 잠금
            foreach (var comp in _cachedLockComponents)
            {
                if (comp != null)
                    comp.enabled = !locked;
            }

            // CharacterController 잠금
            if (_cachedCharacterController != null)
                _cachedCharacterController.enabled = !locked;

            // 팝업 열릴 때 이동 애니메이션 리셋
            if (locked && _cachedAnimator != null && !string.IsNullOrEmpty(_animatorSpeedParam))
            {
                if (HasParameter(_cachedAnimator, _animatorSpeedParam))
                    _cachedAnimator.SetFloat(_animatorSpeedParam, 0f);
            }
        }

        private bool HasParameter(Animator animator, string paramName)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
        }

        // ====== 주기적 갱신 ======

        private void Update()
        {
            foreach (var slot in _slots)
                slot.UpdateCycle();
        }

        public List<PlotSaveData> GetFarmSaveData()
        {
            List<PlotSaveData> saveDataList = new List<PlotSaveData>();
            foreach (var slot in _slots)
            {
                if (slot != null)
                    saveDataList.Add(slot.ToSaveData());
            }
            return saveDataList;
        }

        public void LoadFarmSaveData(List<PlotSaveData> savedPlots)
        {
            // 저장된 데이터가 없으면 신규 시작 (기본 밭 해제)
            if (savedPlots == null || savedPlots.Count == 0)
            {
                InitializeFresh();
                return;
            }

            Dictionary<int, PlotSaveData> dataMap = new Dictionary<int, PlotSaveData>();
            foreach (var p in savedPlots)
            {
                if (p != null) dataMap[p.plotIndex] = p;
            }

            foreach (var slot in _slots)
            {
                if (slot == null) continue;

                if (dataMap.TryGetValue(slot.PlotIndex, out var saveData))
                {
                    SeedData seed = FindSeedByName(saveData.plantedSeedItemId);
                    // 💡 Initialize() 안에서 UpdateCycle()이 불려 오프라인 지난 시간만큼 작물이 자랍니다!
                    slot.Initialize(saveData, seed);
                }
                else
                {
                    var fresh = new PlotSaveData(slot.PlotIndex);
                    fresh.state = slot.PlotIndex < _config.initialUnlockedCount ? PlotState.Empty : PlotState.Locked;
                    fresh.SetCycleStartTime(System.DateTime.Now);
                    slot.Initialize(fresh, null);
                }
            }

            Debug.Log($"[PlotManager] 농사 데이터 및 오프라인 시간 복원 완료 (총 {_slots.Count}개 밭)");
        }
    }
}