using System.Collections.Generic;
using Battle.Rules;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 고정된 Up / Middle / Down 아이템 슬롯을 관리한다.
///
/// PlayerInventory가 보유한 PotionItemData를 불러오고,
/// 최대 3개를 화면에 표시한다.
///
/// 위/아래 입력에 따라 선택 위치와 표시 구간을 이동하며,
/// 선택한 포션은 현재 턴 유닛 자신에게 사용한다.
/// </summary>
public class ItemListController : MonoBehaviour
{
    private const int VisibleSlotCount = 3;

    /// <summary>
    /// 아이템 데이터와 현재 보유 수량을 함께 보관한다.
    /// </summary>
    private readonly struct BattleItemEntryData
    {
        public PotionItemData ItemData { get; }
        public int Amount { get; }

        public BattleItemEntryData(
            PotionItemData itemData,
            int amount)
        {
            ItemData = itemData;
            Amount = amount;
        }
    }

    [Header("Trigger")]
    [SerializeField]
    private Button _itemBtn;

    [Header("Panel")]
    [SerializeField]
    private RectTransform _rectTransform;

    [SerializeField, Min(0.01f)]
    private float _duration = 0.25f;

    [SerializeField]
    private Ease _openEase = Ease.OutQuad;

    [SerializeField]
    private Ease _closeEase = Ease.InQuad;

    [Header("Fixed Item Slots")]
    [Tooltip("첫 번째 표시 슬롯")]
    [SerializeField]
    private BattleItemListEntry _upPlace;

    [Tooltip("두 번째 표시 슬롯")]
    [SerializeField]
    private BattleItemListEntry _middlePlace;

    [Tooltip("세 번째 표시 슬롯")]
    [SerializeField]
    private BattleItemListEntry _downPlace;

    [Header("Action Bar")]
    [SerializeField]
    private BattleActionBarController _actionBar;

    [Header("Camera (씬 오브젝트라 인스펙터 연결 대신 런타임 자동 탐색)")]
    [SerializeField] private BattleCameraDirector _cameraDirector;

    /*
     * PlayerInventory에서 가져온
     * 포션 데이터와 보유 수량 목록.
     */
    private readonly List<BattleItemEntryData>
        _items =
            new List<BattleItemEntryData>();

    /*
     * Hierarchy에 미리 배치된
     * Up / Middle / Down 고정 슬롯.
     */
    private readonly List<BattleItemListEntry>
        _visibleSlots =
            new List<BattleItemListEntry>();

    /*
     * 전체 아이템 목록에서
     * 현재 선택된 실제 인덱스.
     */
    private int _selectedItemIndex = -1;

    /*
     * 현재 UpPlace가 표시하는
     * 전체 아이템 목록의 시작 인덱스.
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

    public int SelectedItemIndex =>
        _selectedItemIndex;

    public int WindowStartIndex =>
        _windowStartIndex;

    private void Awake()
    {
        InitializeVisibleSlots();

        if (_itemBtn != null)
        {
            _itemBtn.onClick.AddListener(Open);
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

    private void OnDisable()
    {
        _rectTransform?.DOKill();

        ClearListInput();

        IsOpen = false;
        _isClosing = false;
    }

    private void OnDestroy()
    {
        if (_itemBtn != null)
        {
            _itemBtn.onClick.RemoveListener(Open);
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
            _visiblePosX +
            _rectTransform.rect.width;

        _isInitialized = true;
    }

    /// <summary>
    /// BattleCameraDirector는 씬 오브젝트라 프리팹 인스펙터로 직접 연결할 수 없어서
    /// Camera.main 계층에서 런타임에 자동으로 찾음. 실패 시 씬 전체(비활성 포함)에서 검색.
    /// </summary>
    private void EnsureCameraDirector()
    {
        if (_cameraDirector != null) return;

        if (Camera.main != null)
        {
            _cameraDirector = Camera.main.GetComponentInParent<BattleCameraDirector>();
        }

        if (_cameraDirector == null)
        {
            _cameraDirector = FindFirstObjectByType<BattleCameraDirector>(FindObjectsInactive.Include);
        }
    }

    /// <summary>
    /// Item 버튼을 눌렀을 때 호출한다.
    /// </summary>
    public void Open()
    {
        if (IsOpen || _isClosing)
        {
            return;
        }

        OpenPanel();
    }

    /// <summary>
    /// 아이템 패널을 열고 현재 인벤토리의
    /// 포션 목록을 Up / Middle / Down에 표시한다.
    /// </summary>
    public void OpenPanel()
    {
        if (_rectTransform == null)
        {
            Debug.LogWarning(
                "[ItemListController] " +
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

        LoadInventoryItems();

        _rectTransform
            .DOAnchorPosX(
                _visiblePosX,
                _duration
            )
            .SetEase(_openEase)
            .SetUpdate(true);

        _actionBar?.Hide();

        EnsureCameraDirector();

        if (_cameraDirector != null &&
            BattleUIContext.Instance != null &&
            BattleUIContext.Instance.CurrentUnit != null)
        {
            _cameraDirector.PlayItemUseView(BattleUIContext.Instance.CurrentUnit);
        }

        if (BattleUIInputReader.Instance != null)
        {
            BattleUIInputReader.Instance
                .SetItemList(this);
        }
    }

    public void Reopen()
    {
        Open();
    }

    /// <summary>
    /// PlayerInventory에서 PotionItemData만 가져와
    /// 내부 아이템 목록에 보관한다.
    /// </summary>
    private void LoadInventoryItems()
    {
        _items.Clear();

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning(
                "[ItemListController] " +
                "PlayerInventory.Instance가 없습니다.",
                this
            );

            _selectedItemIndex = -1;
            _windowStartIndex = 0;

            ClearFixedSlots();
            return;
        }

        foreach (var inventorySlot
                 in PlayerInventory.Instance
                     .InventorySlots)
        {
            if (inventorySlot == null)
            {
                continue;
            }

            if (inventorySlot.ItemData
                is not PotionItemData potionData)
            {
                continue;
            }

            /*
             * 수량이 없는 슬롯은 전투 아이템 목록에
             * 표시하지 않는다.
             */
            if (inventorySlot.Quantity <= 0)
            {
                continue;
            }

            _items.Add(
                new BattleItemEntryData(
                    potionData,
                    inventorySlot.Quantity
                )
            );
        }

        if (_items.Count == 0)
        {
            _selectedItemIndex = -1;
            _windowStartIndex = 0;

            ClearFixedSlots();
            return;
        }

        _selectedItemIndex = 0;
        _windowStartIndex = 0;

        RefreshVisibleSlots();
    }

    /// <summary>
    /// 현재 windowStartIndex를 기준으로
    /// Up / Middle / Down 슬롯을 갱신한다.
    /// </summary>
    private void RefreshVisibleSlots()
    {
        for (int slotIndex = 0;
             slotIndex < VisibleSlotCount;
             slotIndex++)
        {
            BattleItemListEntry slot =
                GetSlot(slotIndex);

            if (slot == null)
            {
                continue;
            }

            int itemIndex =
                _windowStartIndex +
                slotIndex;

            if (itemIndex < 0 ||
                itemIndex >= _items.Count)
            {
                slot.Clear();
                continue;
            }

            BattleItemEntryData itemEntry =
                _items[itemIndex];

            bool canUse =
                itemEntry.ItemData != null &&
                itemEntry.Amount > 0;

            slot.Bind(
                itemEntry.ItemData,
                itemEntry.Amount,
                itemIndex,
                canUse,
                this
            );

            slot.SetSelectedImmediate(
                itemIndex ==
                _selectedItemIndex
            );
        }
    }

    private BattleItemListEntry GetSlot(
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
            _items.Count == 0)
        {
            return;
        }

        int nextIndex =
            _selectedItemIndex - 1;

        /*
         * 첫 번째 아이템에서 위로 이동하면
         * 마지막 아이템으로 순환한다.
         */
        if (nextIndex < 0)
        {
            _selectedItemIndex =
                _items.Count - 1;

            _windowStartIndex =
                Mathf.Max(
                    0,
                    _items.Count -
                    VisibleSlotCount
                );

            RefreshVisibleSlots();
            return;
        }

        _selectedItemIndex = nextIndex;

        /*
         * 현재 선택이 UpPlace보다 위쪽으로 이동하면
         * 표시 구간도 함께 이동시킨다.
         */
        if (_selectedItemIndex <
            _windowStartIndex)
        {
            _windowStartIndex =
                _selectedItemIndex;

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
            _items.Count == 0)
        {
            return;
        }

        int nextIndex =
            _selectedItemIndex + 1;

        /*
         * 마지막 아이템에서 아래로 이동하면
         * 첫 번째 아이템으로 순환한다.
         */
        if (nextIndex >= _items.Count)
        {
            _selectedItemIndex = 0;
            _windowStartIndex = 0;

            RefreshVisibleSlots();
            return;
        }

        _selectedItemIndex = nextIndex;

        int visibleEndIndex =
            _windowStartIndex +
            VisibleSlotCount - 1;

        /*
         * 현재 선택이 DownPlace 아래쪽으로 이동하면
         * 표시 구간을 한 칸 이동시킨다.
         */
        if (_selectedItemIndex >
            visibleEndIndex)
        {
            _windowStartIndex++;

            int maxWindowStart =
                Mathf.Max(
                    0,
                    _items.Count -
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
    /// 마우스가 아이템 슬롯에 올라왔을 때
    /// 해당 슬롯의 실제 아이템 인덱스를 선택한다.
    /// </summary>
    public void SelectItemByIndex(
        int itemIndex)
    {
        if (!IsInputActive)
        {
            return;
        }

        if (itemIndex < 0 ||
            itemIndex >= _items.Count)
        {
            return;
        }

        if (_selectedItemIndex ==
            itemIndex)
        {
            return;
        }

        _selectedItemIndex = itemIndex;

        RefreshSelectionVisualOnly();
    }

    /// <summary>
    /// 아이템 데이터는 유지한 채
    /// 현재 선택된 슬롯의 Reveal 상태만 갱신한다.
    /// </summary>
    private void RefreshSelectionVisualOnly()
    {
        for (int i = 0;
             i < _visibleSlots.Count;
             i++)
        {
            BattleItemListEntry slot =
                _visibleSlots[i];

            if (slot == null ||
                !slot.IsBound)
            {
                continue;
            }

            slot.SetSelected(
                slot.ItemIndex ==
                _selectedItemIndex
            );
        }
    }

    /// <summary>
    /// Enter 또는 마우스 클릭으로
    /// 현재 선택된 아이템을 사용한다.
    /// </summary>
    public void SubmitSelected()
    {
        if (!IsInputActive)
        {
            return;
        }

        if (_selectedItemIndex < 0 ||
            _selectedItemIndex >= _items.Count)
        {
            return;
        }

        BattleItemListEntry selectedSlot =
            FindVisibleSlotByItemIndex(
                _selectedItemIndex
            );

        if (selectedSlot == null)
        {
            return;
        }

        if (!selectedSlot.CanUse)
        {
            Debug.Log(
                "[ItemListController] " +
                "현재 사용할 수 없는 아이템입니다.",
                selectedSlot
            );

            return;
        }

        BattleItemEntryData selectedItem =
            _items[_selectedItemIndex];

        HandleItemSelected(
            selectedItem.ItemData
        );
    }

    private BattleItemListEntry
        FindVisibleSlotByItemIndex(
            int itemIndex)
    {
        for (int i = 0;
             i < _visibleSlots.Count;
             i++)
        {
            BattleItemListEntry slot =
                _visibleSlots[i];

            if (slot != null &&
                slot.IsBound &&
                slot.ItemIndex == itemIndex)
            {
                return slot;
            }
        }

        return null;
    }

    /// <summary>
    /// 선택된 포션을 현재 턴 유닛 자신에게 사용한다.
    /// 사용 성공 시 BattleActionRequest를 제출하여 턴을 소비한다.
    /// </summary>
    private void HandleItemSelected(
    PotionItemData potionData)
    {
        if (potionData == null)
        {
            return;
        }

        if (BattleUIContext.Instance == null)
        {
            Debug.LogWarning(
                "[ItemListController] " +
                "BattleUIContext.Instance가 없습니다.",
                this
            );

            return;
        }

        BattleUnit currentUnit =
            BattleUIContext.Instance.CurrentUnit;

        if (currentUnit == null)
        {
            Debug.LogWarning(
                "[ItemListController] " +
                "현재 턴 유닛이 없습니다.",
                this
            );

            return;
        }

        BattleItemResult result =
            BattleUIContext.Instance.UsePotion(
                currentUnit,
                potionData
            );

        if (!result.Success)
        {
            Debug.Log(
                $"[ItemListController] " +
                $"{potionData.itemName} 사용 실패",
                this
            );

            return;
        }

        Debug.Log(
            $"[ItemListController] " +
            $"{currentUnit.UnitName}: " +
            $"{potionData.itemName} 사용 성공",
            this
        );

        /*
         * 아이템 사용에 성공하면 리스트 입력을 제거하고
         * 패널을 닫은 뒤 전투 액션을 제출한다.
         */
        ClearListInput();
        CloseAfterUse();

        EnsureCameraDirector();

        if (_cameraDirector != null)
        {
            _cameraDirector.PlayPlayerBackView(currentUnit);
        }

        BattleActionRequest actionRequest =
            BattleActionRequest
                .CreateUsingItem(currentUnit);

        BattleUIContext.Instance
            .SubmitAction(actionRequest);
    }

    /// <summary>
    /// Esc 입력 시 호출한다.
    /// 아이템을 사용하지 않았으므로 ActionBar를 다시 표시한다.
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

        EnsureCameraDirector();

        if (_cameraDirector != null &&
            BattleUIContext.Instance != null &&
            BattleUIContext.Instance.CurrentUnit != null)
        {
            _cameraDirector.PlayPlayerBackView(BattleUIContext.Instance.CurrentUnit);
        }

        if (_rectTransform == null)
        {
            FinishCancel();
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
                ClearFixedSlots();

                gameObject.SetActive(false);

                _isClosing = false;
            });

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
    }

    /// <summary>
    /// 아이템 사용 성공 후 호출한다.
    /// 턴이 끝나므로 ActionBar는 다시 표시하지 않는다.
    /// </summary>
    private void CloseAfterUse()
    {
        IsOpen = false;
        _isClosing = true;

        if (_rectTransform == null)
        {
            ClearFixedSlots();

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
                ClearFixedSlots();

                gameObject.SetActive(false);

                _isClosing = false;
            });
    }

    private void ClearListInput()
    {
        if (BattleUIInputReader.Instance != null)
        {
            BattleUIInputReader.Instance
                .ClearItemList(this);
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