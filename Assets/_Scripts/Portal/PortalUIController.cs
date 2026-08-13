using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PortalPanel_v1의 던전 선택, 상세 정보 표시,
/// 입장 조건 확인 및 던전 입장을 관리합니다.
/// </summary>
public class PortalUIController : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField] private PortalNPC _portalNPC;

    [Header("Quest")]
    [Tooltip("DungeonData.CanEnter 검사에 사용할 QuestManager")]
    [SerializeField] private QuestManager _questManager;

    [Header("Dungeon Buttons")]
    [SerializeField] private DungeonButton[] _dungeonButtons;

    [Tooltip("패널 활성화 시 자동으로 선택할 ClassicDungeonBtn")]
    [SerializeField] private DungeonButton _classicDungeonButton;

    [Header("Detail - Dungeon")]
    [SerializeField] private Image _mapIcon;
    [SerializeField] private TMP_Text _mapTxt;
    [SerializeField] private TMP_Text _descriptionTxt;

    [Header("Detail - Monster")]
    [SerializeField] private MapMonsterSlotPool _monsterSlotPool;
    [SerializeField] private ScrollRect _monsterScrollRect;

    [Header("Enter Button")]
    [SerializeField] private Button _enterBtn;
    [SerializeField] private GameObject _enterTxt;
    [SerializeField] private TMP_Text _cannotTxt;

    [Header("Close Button")]
    [SerializeField] private Button _closeBtn;

    private DungeonButton _selectedButton;
    private DungeonData _selectedDungeon;
    private bool _isBound;

    private void Awake()
    {
        ResolveReferences();
        BindEvents();
        ClearDetail();
    }

    private void OnEnable()
    {
        // 패널이 열릴 때마다 Classic Dungeon을 자동 선택
        SelectClassicDungeon();
    }

    private void OnDisable()
    {
        if (_monsterSlotPool != null)
        {
            _monsterSlotPool.ReleaseAll();
        }
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void ResolveReferences()
    {
        if (_questManager == null)
        {
            _questManager = QuestManager.Instance;
        }
    }

    private void BindEvents()
    {
        if (_isBound)
        {
            return;
        }

        _isBound = true;

        if (_dungeonButtons != null)
        {
            foreach (DungeonButton dungeonButton in _dungeonButtons)
            {
                if (dungeonButton == null)
                {
                    continue;
                }

                dungeonButton.OnDungeonSelected += HandleDungeonSelected;
            }
        }

        if (_enterBtn != null)
        {
            _enterBtn.onClick.AddListener(HandleEnterClicked);
        }

        if (_closeBtn != null)
        {
            _closeBtn.onClick.AddListener(HandleCloseClicked);
        }
    }

    private void UnbindEvents()
    {
        if (!_isBound)
        {
            return;
        }

        _isBound = false;

        if (_dungeonButtons != null)
        {
            foreach (DungeonButton dungeonButton in _dungeonButtons)
            {
                if (dungeonButton == null)
                {
                    continue;
                }

                dungeonButton.OnDungeonSelected -= HandleDungeonSelected;
            }
        }

        if (_enterBtn != null)
        {
            _enterBtn.onClick.RemoveListener(HandleEnterClicked);
        }

        if (_closeBtn != null)
        {
            _closeBtn.onClick.RemoveListener(HandleCloseClicked);
        }
    }

    /// <summary>
    /// PortalPanel이 활성화될 때 ClassicDungeonBtn을 자동 선택합니다.
    /// </summary>
    private void SelectClassicDungeon()
    {
        if (_classicDungeonButton == null)
        {
            Debug.LogWarning(
                "[PortalUIController] ClassicDungeonBtn이 할당되지 않았습니다.",
                this
            );

            ClearDetail();
            return;
        }

        if (_classicDungeonButton.DungeonData == null)
        {
            Debug.LogWarning(
                "[PortalUIController] ClassicDungeonBtn에 DungeonData가 없습니다.",
                _classicDungeonButton
            );

            ClearDetail();
            return;
        }

        HandleDungeonSelected(
            _classicDungeonButton,
            _classicDungeonButton.DungeonData
        );
    }

    /// <summary>
    /// 던전 버튼을 클릭했을 때 선택 상태와 상세 정보를 갱신합니다.
    /// </summary>
    private void HandleDungeonSelected(
        DungeonButton selectedButton,
        DungeonData dungeonData
    )
    {
        if (selectedButton == null || dungeonData == null)
        {
            Debug.LogWarning(
                "[PortalUIController] 선택된 버튼 또는 DungeonData가 null입니다.",
                this
            );

            return;
        }

        _selectedButton = selectedButton;
        _selectedDungeon = dungeonData;

        UpdateButtonSelection(selectedButton);
        UpdateDungeonDetail(dungeonData);
        UpdateMonsterList(dungeonData);
        UpdateEnterState(dungeonData);

        Debug.Log(
            $"[PortalUIController] 던전 선택: {dungeonData.DungeonName}",
            this
        );
    }

    /// <summary>
    /// 선택된 버튼만 Select를 활성화하고 1.2배로 확대합니다.
    /// </summary>
    private void UpdateButtonSelection(DungeonButton selectedButton)
    {
        if (_dungeonButtons == null)
        {
            return;
        }

        foreach (DungeonButton dungeonButton in _dungeonButtons)
        {
            if (dungeonButton == null)
            {
                continue;
            }

            bool isSelected = dungeonButton == selectedButton;
            dungeonButton.SetSelected(isSelected, true);
        }
    }

    /// <summary>
    /// 선택된 DungeonData의 기본 정보를 Detail UI에 표시합니다.
    /// </summary>
    private void UpdateDungeonDetail(DungeonData dungeonData)
    {
        if (_mapIcon != null)
        {
            Sprite detailIcon = GetDungeonDetailIcon(dungeonData);

            _mapIcon.sprite = detailIcon;
            _mapIcon.enabled = detailIcon != null;
        }

        if (_mapTxt != null)
        {
            _mapTxt.text = dungeonData.DungeonName;
        }

        if (_descriptionTxt != null)
        {
            _descriptionTxt.text = dungeonData.Description;
        }
    }

    /// <summary>
    /// DungeonData.EnemyPool을 기준으로 몬스터 슬롯을 생성합니다.
    /// </summary>
    private void UpdateMonsterList(DungeonData dungeonData)
    {
        if (_monsterSlotPool == null)
        {
            Debug.LogWarning(
                "[PortalUIController] MapMonsterSlotPool이 할당되지 않았습니다.",
                this
            );

            return;
        }

        _monsterSlotPool.ReleaseAll();

        List<EnemyBattleData> enemyPool = dungeonData.EnemyPool;

        if (enemyPool != null)
        {
            foreach (EnemyBattleData enemyData in enemyPool)
            {
                if (enemyData == null)
                {
                    continue;
                }

                _monsterSlotPool.Get(enemyData);
            }
        }

        // 던전 변경 시 스크롤 위치를 처음으로 복구
        if (_monsterScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _monsterScrollRect.horizontalNormalizedPosition = 0f;
            _monsterScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// 입장 가능 여부에 따라 EnterTxt와 CannotTxt를 변경합니다.
    /// </summary>
    private void UpdateEnterState(DungeonData dungeonData)
    {
        bool canEnter = dungeonData.CanEnter(_questManager);

        if (_enterTxt != null)
        {
            _enterTxt.SetActive(canEnter);
        }

        if (_cannotTxt != null)
        {
            _cannotTxt.gameObject.SetActive(!canEnter);

            if (!canEnter)
            {
                _cannotTxt.text = GetLockedReasonText(dungeonData);
            }
            else
            {
                _cannotTxt.text = string.Empty;
            }
        }

        /*
         * interactable을 false로 만들면 Button의 Disabled Color가 적용될 수 있으므로
         * 현재는 버튼을 활성 상태로 유지하고 HandleEnterClicked에서 입장을 차단합니다.
         */
        if (_enterBtn != null)
        {
            _enterBtn.interactable = true;
        }
    }

    /// <summary>
    /// EnterBtn을 클릭하면 선택된 던전으로 입장합니다.
    /// </summary>
    private void HandleEnterClicked()
    {
        if (_selectedDungeon == null)
        {
            Debug.LogWarning("[PortalUIController] 선택된 던전이 없습니다.", this);
            return;
        }

        bool canEnter = _selectedDungeon.CanEnter(_questManager);

        if (!canEnter)
        {
            string lockedReason = GetLockedReasonText(_selectedDungeon);

            if (ShowMessageManager.Instance != null)
            {
                ShowMessageManager.Instance.ShowMessage(lockedReason);
            }

            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[PortalUIController] SceneTransitionManager.Instance가 null입니다.", this);
            return;
        }

        DungeonSelection.CurrentDungeonData = _selectedDungeon;

        if (ShowMessageManager.Instance != null)
        {
            ShowMessageManager.Instance.ShowMessage($"{_selectedDungeon.DungeonName}에 입장합니다.");
        }

        SceneTransitionManager.Instance.LoadSceneWithLoading(SceneId.Dungeon, waitForReadySignal: true);
    }

    private void HandleCloseClicked()
    {
        if (_portalNPC == null)
        {
            Debug.LogWarning(
                "[PortalUIController] PortalNPC가 할당되지 않았습니다.",
                this
            );

            return;
        }

        _portalNPC.TogglePortal();
    }

    /// <summary>
    /// DungeonIcon을 우선 사용하고, 없으면 Presentation의 Icon을 사용합니다.
    /// </summary>
    private Sprite GetDungeonDetailIcon(DungeonData dungeonData)
    {
        if (dungeonData == null)
        {
            return null;
        }

        if (dungeonData.DungeonIcon != null)
        {
            return dungeonData.DungeonIcon;
        }

        return dungeonData.Icon;
    }

    private string GetLockedReasonText(DungeonData dungeonData)
    {
        if (dungeonData == null || dungeonData.RequiredQuest == null)
        {
            return "현재 입장할 수 없습니다.";
        }

        return $"[{dungeonData.RequiredQuest.title}] 퀘스트 진행 후 입장 가능";
    }

    private void ClearDetail()
    {
        _selectedButton = null;
        _selectedDungeon = null;

        if (_dungeonButtons != null)
        {
            foreach (DungeonButton dungeonButton in _dungeonButtons)
            {
                if (dungeonButton != null)
                {
                    dungeonButton.SetSelected(false, false);
                }
            }
        }

        if (_mapIcon != null)
        {
            _mapIcon.sprite = null;
            _mapIcon.enabled = false;
        }

        if (_mapTxt != null)
        {
            _mapTxt.text = string.Empty;
        }

        if (_descriptionTxt != null)
        {
            _descriptionTxt.text = string.Empty;
        }

        if (_monsterSlotPool != null)
        {
            _monsterSlotPool.ReleaseAll();
        }

        if (_enterTxt != null)
        {
            _enterTxt.SetActive(false);
        }

        if (_cannotTxt != null)
        {
            _cannotTxt.text = string.Empty;
            _cannotTxt.gameObject.SetActive(false);
        }
    }
}