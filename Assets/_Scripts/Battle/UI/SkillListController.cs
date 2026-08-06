using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 고정된 Up / Middle / Down 스킬 슬롯을 관리한다.
///
/// 캐릭터의 전체 스킬 목록 중 최대 3개를 화면에 표시하고,
/// 위/아래 입력에 따라 표시 구간을 이동시킨다.
/// </summary>
public class SkillListController : MonoBehaviour
{
    private const int VisibleSlotCount = 3;

    [Header("Trigger")]
    [SerializeField] private Button _skillBtn;

    [Header("Panel")]
    [SerializeField] private RectTransform _rectTransform;

    [SerializeField, Min(0.01f)]
    private float _duration = 0.25f;

    [SerializeField]
    private Ease _openEase =
        Ease.OutQuad;

    [SerializeField]
    private Ease _closeEase =
        Ease.InQuad;

    [Header("Fixed Skill Slots")]
    [Tooltip("첫 번째 표시 슬롯")]
    [SerializeField] private BattleSkillListEntry _upPlace;

    [Tooltip("두 번째 표시 슬롯")]
    [SerializeField] private BattleSkillListEntry _middlePlace;

    [Tooltip("세 번째 표시 슬롯")]
    [SerializeField] private BattleSkillListEntry _downPlace;

    [Header("Action Bar")]
    [SerializeField]
    private BattleActionBarController _actionBar;

    [Header("Target Cycler")]
    [SerializeField]
    private BattleTargetCycler _targetCycler;

    [Header("Camera")]
    [SerializeField]
    private BattleCameraDirector _cameraDirector;

    [Header("Presentation")]
    [SerializeField]
    private SkillPresentationPalette _skillPresentationPalette;

    private readonly List<SkillData> _skills =
        new List<SkillData>();

    private readonly List<BattleSkillListEntry>
        _visibleSlots =
            new List<BattleSkillListEntry>();

    /*
     * 전체 스킬 목록에서 현재 선택된 실제 인덱스.
     */
    private int _selectedSkillIndex = -1;

    /*
     * 현재 UpPlace가 표시하는 전체 스킬 인덱스.
     */
    private int _windowStartIndex;

    private float _visiblePosX;
    private float _hiddenPosX;

    private bool _isInitialized;
    private bool _isClosing;

    public bool IsOpen { get; private set; }

    public bool IsInputActive =>
        IsOpen &&
        !_isClosing &&
        gameObject.activeInHierarchy;

    public int SelectedSkillIndex =>
        _selectedSkillIndex;

    public int WindowStartIndex =>
        _windowStartIndex;

    private void Awake()
    {
        InitializeVisibleSlots();

        if (_skillBtn != null)
        {
            _skillBtn.onClick.AddListener(Open);
        }


    }

    private void Start()
    {
        EnsureInitialized();

        IsOpen = false;
        _isClosing = false;

        ClearFixedSlots();
        SetPosXImmediate(_hiddenPosX);

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        EnsureCameraDirector();
        EnsureTargetCycler();
    }

    private void OnDisable()
    {
        _rectTransform?.DOKill();

        if (BattleUIInputReader.Instance != null)
        {
            BattleUIInputReader.Instance
                .ClearSkillList(this);
        }

        IsOpen = false;
        _isClosing = false;
    }

    private void OnDestroy()
    {
        if (_skillBtn != null)
        {
            _skillBtn.onClick.RemoveListener(Open);
        }


        _rectTransform?.DOKill();
    }

    private void InitializeVisibleSlots()
    {
        _visibleSlots.Clear();

        _visibleSlots.Add(_upPlace);
        _visibleSlots.Add(_middlePlace);
        _visibleSlots.Add(_downPlace);
    }

    private void EnsureInitialized()
    {
        if (_isInitialized ||
            _rectTransform == null)
        {
            return;
        }

        _visiblePosX =
            _rectTransform.anchoredPosition.x;

        _hiddenPosX =
            _visiblePosX + _rectTransform.rect.width;

        _isInitialized = true;
    }

    private void EnsureCameraDirector()
    {
        if (_cameraDirector != null)
        {
            return;
        }

        if (Camera.main != null)
        {
            _cameraDirector =
                Camera.main.GetComponentInParent
                    <BattleCameraDirector>();
        }

        if (_cameraDirector == null)
        {
            _cameraDirector =
                FindFirstObjectByType
                    <BattleCameraDirector>(
                        FindObjectsInactive.Include
                    );
        }
    }

    private void EnsureTargetCycler()
    {
        if (_targetCycler != null)
        {
            return;
        }

        _targetCycler =
            BattleTargetCycler.Instance;

        if (_targetCycler == null)
        {
            _targetCycler =
                FindFirstObjectByType
                    <BattleTargetCycler>(
                        FindObjectsInactive.Include
                    );
        }
    }

    public void Open()
    {
        if (IsOpen || _isClosing)
        {
            return;
        }

        BattleUnit currentUnit =
            BattleUIContext.Instance != null
                ? BattleUIContext.Instance.CurrentUnit
                : null;

        EnsureCameraDirector();

        if (_cameraDirector == null ||
            currentUnit == null)
        {
            OpenPanel();
            return;
        }

        _cameraDirector.PlaySkillLowAngle(
            currentUnit,
            OpenPanel
        );
    }

    public void OpenPanel()
    {
        if (_rectTransform == null)
        {
            Debug.LogWarning(
                "[SkillListController] " +
                "RectTransform이 연결되지 않았습니다.",
                this
            );

            return;
        }

        EnsureInitialized();

        _rectTransform.DOKill();

        IsOpen = true;
        _isClosing = false;

        gameObject.SetActive(true);
        SetPosXImmediate(_hiddenPosX);

        LoadCurrentUnitSkills();

        _rectTransform
            .DOAnchorPosX(
                _visiblePosX,
                _duration
            )
            .SetEase(_openEase)
            .SetUpdate(true);

        _actionBar?.Hide();

        if (BattleUIInputReader.Instance != null)
        {
            BattleUIInputReader.Instance
                .SetSkillList(this);
        }
    }

    public void Reopen()
    {
        Open();
    }

    /// <summary>
    /// 현재 턴 캐릭터의 스킬을 내부 목록에 복사하고
    /// 0번부터 Up / Middle / Down에 표시한다.
    /// </summary>
    private void LoadCurrentUnitSkills()
    {
        _skills.Clear();

        BattleUnit currentUnit =
            BattleUIContext.Instance != null
                ? BattleUIContext.Instance.CurrentUnit
                : null;

        if (currentUnit == null)
        {
            ClearFixedSlots();
            return;
        }

        foreach (SkillData skillData
                 in currentUnit.SkillList)
        {
            if (skillData != null)
            {
                _skills.Add(skillData);
            }
        }

        if (_skills.Count == 0)
        {
            _selectedSkillIndex = -1;
            _windowStartIndex = 0;

            ClearFixedSlots();
            return;
        }

        _selectedSkillIndex = 0;
        _windowStartIndex = 0;

        RefreshVisibleSlots();
    }

    /// <summary>
    /// 현재 windowStartIndex를 기준으로
    /// Up / Middle / Down 슬롯을 갱신한다.
    /// </summary>
    private void RefreshVisibleSlots()
    {
        BattleUnit currentUnit =
            BattleUIContext.Instance != null
                ? BattleUIContext.Instance.CurrentUnit
                : null;

        for (int slotIndex = 0;
             slotIndex < VisibleSlotCount;
             slotIndex++)
        {
            BattleSkillListEntry slot =
                GetSlot(slotIndex);

            if (slot == null)
            {
                continue;
            }

            int skillIndex =
                _windowStartIndex + slotIndex;

            /*
             * 3개 이하라면:
             * Up → Middle → Down 순으로 필요한 만큼만 활성화된다.
             *
             * 목록 자체는 화면 안에서 순환시켜 중복 표시하지 않는다.
             */
            if (skillIndex < 0 ||
                skillIndex >= _skills.Count)
            {
                slot.Clear();
                continue;
            }

            SkillData skillData =
                _skills[skillIndex];

            bool canUse =
                currentUnit == null ||
                currentUnit.CanUseSkill(skillData);

            slot.Bind(
                skillData,
                skillIndex,
                canUse,
                this,
                _skillPresentationPalette
            );

            slot.SetSelectedImmediate(
                skillIndex == _selectedSkillIndex
            );
        }
    }

    private BattleSkillListEntry GetSlot(
        int slotIndex)
    {
        if (slotIndex < 0 ||
            slotIndex >= _visibleSlots.Count)
        {
            return null;
        }

        return _visibleSlots[slotIndex];
    }

    /// <summary>
    /// 위 방향키 입력.
    /// </summary>
    public void MoveSelectionUp()
    {
        if (!IsInputActive ||
            _skills.Count == 0)
        {
            return;
        }

        int previousIndex =
            _selectedSkillIndex;

        int nextIndex =
            previousIndex - 1;

        /*
         * 첫 번째 스킬에서 위로 이동하면
         * 마지막 스킬로 순환한다.
         */
        if (nextIndex < 0)
        {
            nextIndex = _skills.Count - 1;

            _selectedSkillIndex = nextIndex;

            /*
             * 예: 스킬 4개
             *
             * Up     = Skill[1]
             * Middle = Skill[2]
             * Down   = Skill[3]
             */
            _windowStartIndex =
                Mathf.Max(
                    0,
                    _skills.Count - VisibleSlotCount
                );

            RefreshVisibleSlots();
            return;
        }

        _selectedSkillIndex = nextIndex;

        /*
         * 현재 UpPlace에서 위로 이동했다면
         * 표시 구간을 한 칸 아래 인덱스 방향으로 옮긴다.
         */
        if (_selectedSkillIndex <
            _windowStartIndex)
        {
            _windowStartIndex =
                _selectedSkillIndex;

            RefreshVisibleSlots();
            return;
        }

        RefreshSelectionVisualOnly();
    }

    /// <summary>
    /// 아래 방향키 입력.
    /// </summary>
    public void MoveSelectionDown()
    {
        if (!IsInputActive ||
            _skills.Count == 0)
        {
            return;
        }

        int nextIndex =
            _selectedSkillIndex + 1;

        /*
         * 마지막 스킬에서 아래로 이동하면
         * 첫 번째 스킬로 순환한다.
         */
        if (nextIndex >= _skills.Count)
        {
            _selectedSkillIndex = 0;
            _windowStartIndex = 0;

            RefreshVisibleSlots();
            return;
        }

        _selectedSkillIndex = nextIndex;

        int visibleEndIndex =
            _windowStartIndex +
            VisibleSlotCount - 1;

        /*
         * 현재 DownPlace에서 아래로 이동한 경우
         * 표시 구간을 한 칸씩 올린다.
         */
        if (_selectedSkillIndex >
            visibleEndIndex)
        {
            _windowStartIndex++;

            int maxWindowStart =
                Mathf.Max(
                    0,
                    _skills.Count -
                    VisibleSlotCount
                );

            _windowStartIndex =
                Mathf.Min(
                    _windowStartIndex,
                    maxWindowStart
                );

            RefreshVisibleSlots();
            return;
        }

        RefreshSelectionVisualOnly();
    }

    /// <summary>
    /// 마우스가 FrameImg에 올라왔을 때 호출된다.
    /// 슬롯에 연결된 실제 스킬 인덱스를 선택한다.
    /// </summary>
    public void SelectSkillByIndex(
        int skillIndex)
    {
        if (!IsInputActive)
        {
            return;
        }

        if (skillIndex < 0 ||
            skillIndex >= _skills.Count)
        {
            return;
        }

        if (_selectedSkillIndex ==
            skillIndex)
        {
            return;
        }

        _selectedSkillIndex = skillIndex;
        RefreshSelectionVisualOnly();
    }

    /// <summary>
    /// 리스트의 데이터는 그대로 유지하고
    /// 현재 선택된 슬롯의 Reveal 효과만 변경한다.
    /// </summary>
    private void RefreshSelectionVisualOnly()
    {
        for (int i = 0;
             i < _visibleSlots.Count;
             i++)
        {
            BattleSkillListEntry slot =
                _visibleSlots[i];

            if (slot == null ||
                !slot.IsBound)
            {
                continue;
            }

            slot.SetSelected(
                slot.SkillIndex ==
                _selectedSkillIndex
            );
        }
    }

    /// <summary>
    /// Enter 또는 마우스 클릭으로 현재 스킬을 확정한다.
    /// </summary>
    public void SubmitSelected()
    {
        if (!IsInputActive)
        {
            return;
        }

        if (_selectedSkillIndex < 0 ||
            _selectedSkillIndex >= _skills.Count)
        {
            return;
        }

        BattleSkillListEntry selectedSlot =
            FindVisibleSlotBySkillIndex(
                _selectedSkillIndex
            );

        if (selectedSlot == null)
        {
            return;
        }

        if (!selectedSlot.CanUse)
        {
            Debug.Log(
                "[SkillListController] " +
                "현재 사용할 수 없는 스킬입니다.",
                selectedSlot
            );

            return;
        }

        SkillData selectedSkill =
            _skills[_selectedSkillIndex];

        HandleSkillSelected(selectedSkill);
    }

    private BattleSkillListEntry
        FindVisibleSlotBySkillIndex(
            int skillIndex)
    {
        for (int i = 0;
             i < _visibleSlots.Count;
             i++)
        {
            BattleSkillListEntry slot =
                _visibleSlots[i];

            if (slot != null &&
                slot.IsBound &&
                slot.SkillIndex == skillIndex)
            {
                return slot;
            }
        }

        return null;
    }

    private void HandleSkillSelected(
        SkillData skillData)
    {
        if (skillData == null)
        {
            return;
        }

        EnsureTargetCycler();

        if (_targetCycler == null)
        {
            Debug.LogWarning(
                "[SkillListController] " +
                "BattleTargetCycler를 찾지 못했습니다.",
                this
            );

            return;
        }

        ClearListInput();
        SlideOutForTargeting();

        _targetCycler.EnterSkillMode(skillData);
    }

    /// <summary>
    /// 스킬 리스트를 취소하고 기본 커맨드 UI로 돌아간다.
    /// Esc 입력 시 BattleUIInputReader에서 호출한다.
    /// </summary>
    public void Cancel()
    {
        if (!IsOpen || _isClosing)
        {
            return;
        }

        IsOpen = false;
        _isClosing = true;

        ClearListInput();

        if (_rectTransform == null)
        {
            FinishCancel();
            return;
        }

        _rectTransform.DOKill();

        _rectTransform
            .DOAnchorPosX(_hiddenPosX, _duration)
            .SetEase(_closeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                ClearFixedSlots();
                gameObject.SetActive(false);
                _isClosing = false;
            });

        BattleUnit currentUnit =
            BattleUIContext.Instance != null
                ? BattleUIContext.Instance.CurrentUnit
                : null;

        EnsureCameraDirector();

        if (_cameraDirector != null &&
            currentUnit != null)
        {
            _cameraDirector.PlayPlayerBackView(
                currentUnit,
                ShowActionBar
            );

            return;
        }

        ShowActionBar();
    }

    private void FinishCancel()
    {
        IsOpen = false;
        _isClosing = false;

        ClearListInput();
        ClearFixedSlots();

        gameObject.SetActive(false);

        ShowActionBar();
    }

    private void ShowActionBar()
    {
        _actionBar?.Show();

        /*
         * BattleUIInputReader가 ActionBar 입력 대상을 별도로
         * 비활성화하고 있었다면 여기서 다시 활성화한다.
         *
         * 예:
         * _actionBar?.ActivateInput();
         *
         * 현재 SetSkillList/ClearSkillList만으로 입력 우선권을
         * 제어한다면 추가 코드는 필요 없다.
         */
    }

    // private void FinishNormalClose()
    // {
    //     IsOpen = false;
    //     _isClosing = false;

    //     ClearListInput();
    //     ClearFixedSlots();

    //     gameObject.SetActive(false);
    //     _actionBar?.Show();
    // }

    private void SlideOutForTargeting()
    {
        IsOpen = false;
        _isClosing = true;

        if (_rectTransform == null)
        {
            gameObject.SetActive(false);
            _isClosing = false;
            return;
        }

        _rectTransform.DOKill();

        _rectTransform
            .DOAnchorPosX(
                _hiddenPosX,
                _duration
            )
            .SetEase(_closeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                _isClosing = false;
            });
    }

    private void ClearListInput()
    {
        if (BattleUIInputReader.Instance != null)
        {
            BattleUIInputReader.Instance
                .ClearSkillList(this);
        }
    }

    private void ClearFixedSlots()
    {
        for (int i = 0;
             i < _visibleSlots.Count;
             i++)
        {
            _visibleSlots[i]?.Clear();
        }
    }

    private void SetPosXImmediate(float posX)
    {
        if (_rectTransform == null)
        {
            return;
        }

        Vector2 position =
            _rectTransform.anchoredPosition;

        position.x = posX;

        _rectTransform.anchoredPosition =
            position;
    }
}