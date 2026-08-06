using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 인벤토리 슬롯 클릭 시 하단에서 슬라이드업 되는 아이템 상세 패널.
/// - 소비/재료/씨앗/퀘스트 아이템: Show(ItemData) 사용. 항상 Description만 표시.
/// - 장비: Show(EquipmentInstance) 사용. Description + EquipStatDetailRow(강화 반영된 cachedStats 기준) 함께 표시.
/// - SellBtn 클릭 시 BtnsWrap -> SellWrap으로 크로스페이드 전환, 수량 조절 후 실제 판매 처리.
///   장비는 개체 단위 판매라 수량이 항상 1로 고정됨.
/// * DOTween 필요 (Window > Package Manager로 임포트 후 사용)
/// </summary>
public class InventoryDetailController : MonoBehaviour
{
    private const int _minSellAmount = 1;

    [Header("Slide Panel")]
    [SerializeField] private RectTransform _panelRect;
    [SerializeField] private float _slideDuration = 0.3f;

    [Header("Info")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _itemTypeText;
    [SerializeField] private TextMeshProUGUI _itemGradeText;
    [SerializeField] private TextMeshProUGUI _requiredLevelText;

    [Header("Description (모든 아이템 공통 표시)")]
    [SerializeField] private GameObject _descriptionObject;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Tooltip("장착 중인 장비를 보고 있을 때만 활성화")]
    [SerializeField] private GameObject _equipStateText;

    [Header("Stats (장비 아이템용)")]
    [Tooltip("보이기/숨기기를 토글할 오브젝트")]
    [SerializeField] private GameObject _statsContainer;
    [Tooltip("EquipStatDetailRow들이 실제로 생성될 부모 (VerticalLayoutGroup이 붙은 오브젝트)")]
    [SerializeField] private Transform _statsRoot;
    [SerializeField] private EquipStatDetailRow _statRowPrefab;

    [Header("Close Btn")]
    [SerializeField] private Button _closeBtn;

    [Header("Action Btns - BtnsWrap")]
    [Tooltip("BtnsWrap 전체 (SellWrap과 서로 전환됨)")]
    [SerializeField] private GameObject _btnsWrap;
    [SerializeField] private CanvasGroup _btnsWrapCanvasGroup;
    [Tooltip("장비 아이템일 때만 표시")]
    [SerializeField] private Button _equipBtn;
    [Tooltip("장착 중인 장비를 보고 있을 때만 표시")]
    [SerializeField] private Button _unequipBtn;
    [Tooltip("ItemData.canSell이 true인 아이템만 표시")]
    [SerializeField] private Button _sellBtn;
    [Tooltip("MainCategory.Consume + SubCategory.Book(마도서)인 소비 아이템만 표시")]
    [SerializeField] private Button _useBtn;

    [Header("Skill Book Gacha")]
    [Tooltip("마도서 사용 결과를 재생할 Overlay Controller")]
    [SerializeField] private SkillGachaResultOverlayController _skillGachaOverlayController;

    [Header("Equip Stat Section")]
    [Tooltip("Equip/StatSection의 Current/Change 표시 담당")]
    [SerializeField] private EquipStatSectionController _equipStatSectionController;

    [Header("Sell Wrap")]
    [SerializeField] private GameObject _sellWrap;
    [SerializeField] private CanvasGroup _sellWrapCanvasGroup;
    [SerializeField] private Button _sellMinusBtn;
    [SerializeField] private Button _sellPlusBtn;
    [SerializeField] private Button _sellMaxBtn;
    [SerializeField] private TMP_InputField _sellAmountInput;
    [SerializeField] private TextMeshProUGUI _sellTotalPriceText;
    [SerializeField] private Button _sellConfirmBtn;
    [SerializeField] private Button _sellCancelBtn;

    [Header("Wrap 전환 애니메이션")]
    [SerializeField] private float _wrapFadeDuration = 0.2f;

    private readonly List<GameObject> _spawnedStatRows = new List<GameObject>();

    // 소비/재료/씨앗/퀘스트는 _currentItemData, 장비는 _currentEquipmentInstance만 채워짐 (동시에 둘 다 채워지지 않음)
    private ItemData _currentItemData;
    private EquipmentInstance _currentEquipmentInstance;

    private int _currentSellAmount;
    private int _currentMaxSellAmount = _minSellAmount;

    private float _shownY;
    private float _hiddenY;

    private void Awake()
    {
        if (_closeBtn != null) _closeBtn.onClick.AddListener(Hide);
        if (_equipBtn != null) _equipBtn.onClick.AddListener(OnClickEquip);
        if (_unequipBtn != null) _unequipBtn.onClick.AddListener(OnClickUnequip);
        if (_sellBtn != null) _sellBtn.onClick.AddListener(OnClickSellBtn);
        if (_useBtn != null) _useBtn.onClick.AddListener(OnClickUse);
        if (_sellCancelBtn != null) _sellCancelBtn.onClick.AddListener(OnClickSellCancel);
        if (_sellConfirmBtn != null) _sellConfirmBtn.onClick.AddListener(OnClickSellConfirm);
        if (_sellMinusBtn != null) _sellMinusBtn.onClick.AddListener(OnClickSellMinus);
        if (_sellPlusBtn != null) _sellPlusBtn.onClick.AddListener(OnClickSellPlus);
        if (_sellMaxBtn != null) _sellMaxBtn.onClick.AddListener(OnClickSellMax);
        if (_sellAmountInput != null) _sellAmountInput.onEndEdit.AddListener(OnSellAmountInputEndEdit);

        SetWrapStateImmediate(showSellWrap: false);

        if (_panelRect == null) return;

        _shownY = _panelRect.anchoredPosition.y;
        _hiddenY = _shownY - _panelRect.rect.height;

        _panelRect.anchoredPosition = new Vector2(_panelRect.anchoredPosition.x, _hiddenY);
    }


    /// <summary>
    /// 소비/재료/씨앗/퀘스트 아이템 정보를 채우고 패널을 슬라이드업으로 보여준다.
    /// </summary>
    public void Show(ItemData itemData)
    {
        if (itemData == null) return;

        _currentItemData = itemData;
        _currentEquipmentInstance = null;

        SetWrapStateImmediate(showSellWrap: false);

        SetInfoCommon(itemData);

        if (_requiredLevelText != null) _requiredLevelText.gameObject.SetActive(false);
        if (_statsContainer != null) _statsContainer.SetActive(false);
        if (_equipBtn != null) _equipBtn.gameObject.SetActive(false);
        if (_unequipBtn != null) _unequipBtn.gameObject.SetActive(false);
        if (_equipStateText != null) _equipStateText.SetActive(false);

        bool isBook = itemData.mainCategory == MainCategory.Consume && itemData.subCategory == SubCategory.Book;
        if (_useBtn != null) _useBtn.gameObject.SetActive(isBook);

        ClearStatRows();

        if (_descriptionObject != null) _descriptionObject.SetActive(true);
        if (_descriptionText != null) _descriptionText.text = itemData.description;

        SlideTo(_shownY);
    }

    /// <summary>
    /// 장비 개체 정보를 채우고 패널을 슬라이드업으로 보여준다. 스탯은 강화 반영된 cachedStats 기준.
    /// </summary>
    public void Show(EquipmentInstance equipmentInstance)
    {
        Debug.Log($"[InventoryDetailController] Show(EquipmentInstance) 호출됨. instance null? {equipmentInstance == null}");
        if (equipmentInstance == null || equipmentInstance.baseData == null) return;

        _currentEquipmentInstance = equipmentInstance;
        _currentItemData = null;

        ItemData itemData = equipmentInstance.baseData;

        SetWrapStateImmediate(showSellWrap: false);

        SetInfoCommon(itemData);

        if (_useBtn != null) _useBtn.gameObject.SetActive(false);

        if (_nameText != null && equipmentInstance.enhanceLevel > 0)
        {
            _nameText.text = $"{itemData.itemName} +{equipmentInstance.enhanceLevel}";
        }

        if (_requiredLevelText != null)
        {
            _requiredLevelText.gameObject.SetActive(true);
            _requiredLevelText.text = $"{equipmentInstance.baseData.requiredLevel}레벨 장비";
        }

        if (_descriptionObject != null) _descriptionObject.SetActive(true);
        if (_descriptionText != null) _descriptionText.text = itemData.description;
        if (_statsContainer != null) _statsContainer.SetActive(true);

        bool isEquipped = CharacterEquipment.IsEquippedByAnyone(equipmentInstance);

        if (_equipBtn != null) _equipBtn.gameObject.SetActive(!isEquipped);
        if (_unequipBtn != null) _unequipBtn.gameObject.SetActive(isEquipped);
        if (_equipStateText != null) _equipStateText.SetActive(isEquipped);

        if (isEquipped && _sellBtn != null)
        {
            _sellBtn.gameObject.SetActive(false);
        }

        BuildStatRows(equipmentInstance.cachedStats);

        SlideTo(_shownY);
    }

    /// <summary>
    /// Show(ItemData)/Show(EquipmentInstance) 공통으로 채우는 정보 (아이콘/이름/타입/등급/판매버튼)
    /// </summary>
    private void SetInfoCommon(ItemData itemData)
    {
        if (_iconImage != null) _iconImage.sprite = itemData.icon;
        if (_nameText != null) _nameText.text = itemData.itemName;
        if (_itemTypeText != null) _itemTypeText.text = itemData.itemType.ToDisplayString();
        if (_itemGradeText != null) _itemGradeText.text = itemData.itemGrade.ToDisplayString();

        if (_sellBtn != null) _sellBtn.gameObject.SetActive(itemData.canSell);
    }

    /// <summary>
    /// 패널을 아래로 슬라이드다운 시켜서 숨긴다.
    /// </summary>
    public void Hide()
    {
        SlideTo(_hiddenY);
    }

    /// <summary>
    /// 애니메이션 없이 즉시 숨김 상태로 초기화 (인벤토리 패널이 새로 열릴 때 사용)
    /// </summary>
    public void HideImmediate()
    {
        if (_panelRect == null) return;

        _panelRect.DOKill();
        _panelRect.anchoredPosition = new Vector2(_panelRect.anchoredPosition.x, _hiddenY);
    }

    private void SlideTo(float targetY)
    {
        if (_panelRect == null) return;

        _panelRect.DOKill();
        _panelRect.DOAnchorPosY(targetY, _slideDuration).SetEase(Ease.OutQuad);
    }

    // ===================== BtnsWrap <-> SellWrap 전환 =====================

    private void OnClickSellBtn()
    {
        if (PlayerInventory.Instance == null) return;

        if (_currentEquipmentInstance != null)
        {
            // 장비는 개체 단위 판매라 수량이 항상 1
            _currentMaxSellAmount = _minSellAmount;
            SetSellAmount(_minSellAmount);
            SetSellAmountControlsInteractable(false);
        }
        else if (_currentItemData != null)
        {
            _currentMaxSellAmount = Mathf.Max(_minSellAmount, PlayerInventory.Instance.GetTotalQuantity(_currentItemData));
            SetSellAmount(_minSellAmount);
            SetSellAmountControlsInteractable(true);
        }
        else
        {
            return;
        }

        CrossFadeToSellWrap();
    }

    private void OnClickSellCancel()
    {
        CrossFadeToBtnsWrap();
    }

    private void CrossFadeToSellWrap()
    {
        if (_btnsWrap != null) _btnsWrap.SetActive(true);
        if (_sellWrap != null) _sellWrap.SetActive(true);

        FadeCanvasGroup(_btnsWrapCanvasGroup, 0f, () => _btnsWrap?.SetActive(false));
        FadeCanvasGroup(_sellWrapCanvasGroup, 1f, null);
    }

    private void CrossFadeToBtnsWrap()
    {
        if (_btnsWrap != null) _btnsWrap.SetActive(true);
        if (_sellWrap != null) _sellWrap.SetActive(true);

        FadeCanvasGroup(_sellWrapCanvasGroup, 0f, () => _sellWrap?.SetActive(false));
        FadeCanvasGroup(_btnsWrapCanvasGroup, 1f, null);
    }

    /// <summary>
    /// 애니메이션 없이 즉시 BtnsWrap/SellWrap 상태를 세팅 (Show()로 새 아이템 열 때, 시작 시 사용)
    /// </summary>
    private void SetWrapStateImmediate(bool showSellWrap)
    {
        if (_btnsWrapCanvasGroup != null) _btnsWrapCanvasGroup.DOKill();
        if (_sellWrapCanvasGroup != null) _sellWrapCanvasGroup.DOKill();

        SetCanvasGroupInstant(_btnsWrapCanvasGroup, showSellWrap ? 0f : 1f);
        SetCanvasGroupInstant(_sellWrapCanvasGroup, showSellWrap ? 1f : 0f);

        if (_btnsWrap != null) _btnsWrap.SetActive(!showSellWrap);
        if (_sellWrap != null) _sellWrap.SetActive(showSellWrap);
    }

    private void SetCanvasGroupInstant(CanvasGroup canvasGroup, float alpha)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = alpha > 0.5f;
        canvasGroup.blocksRaycasts = alpha > 0.5f;
    }

    private void FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, System.Action onComplete)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        bool willBeVisible = targetAlpha > 0.5f;
        canvasGroup.interactable = willBeVisible;
        canvasGroup.blocksRaycasts = willBeVisible;

        canvasGroup.DOKill();
        canvasGroup.DOFade(targetAlpha, _wrapFadeDuration).OnComplete(() => onComplete?.Invoke());
    }

    // ===================== Sell 수량 조절 =====================

    private void OnClickSellMinus() => SetSellAmount(_currentSellAmount - 1);
    private void OnClickSellPlus() => SetSellAmount(_currentSellAmount + 1);
    private void OnClickSellMax() => SetSellAmount(_currentMaxSellAmount);

    /// <summary>
    /// SellAmountInput에 직접 숫자를 입력하고 포커스를 벗어났을 때(엔터 포함) 호출됨.
    /// </summary>
    private void OnSellAmountInputEndEdit(string inputText)
    {
        if (!int.TryParse(inputText, out int parsedAmount))
        {
            parsedAmount = _currentSellAmount;
        }

        SetSellAmount(parsedAmount);
    }

    /// <summary>
    /// 수량을 1~최대판매수량 범위로 clamp한 뒤 텍스트/총 판매가를 갱신.
    /// (장비는 항상 최대 1로 고정되어 있어서 사실상 값이 안 바뀜)
    /// </summary>
    private void SetSellAmount(int amount)
    {
        _currentSellAmount = Mathf.Clamp(amount, _minSellAmount, _currentMaxSellAmount);

        if (_sellAmountInput != null)
        {
            _sellAmountInput.SetTextWithoutNotify(_currentSellAmount.ToString());
        }

        if (_sellTotalPriceText != null)
        {
            int totalPrice = GetCurrentSellUnitPrice() * _currentSellAmount;
            _sellTotalPriceText.text = totalPrice.ToString();
        }
    }

    private int GetCurrentSellUnitPrice()
    {
        if (_currentEquipmentInstance != null) return _currentEquipmentInstance.baseData.sellPrice;
        if (_currentItemData != null) return _currentItemData.sellPrice;
        return 0;
    }

    private void SetSellAmountControlsInteractable(bool interactable)
    {
        if (_sellMinusBtn != null) _sellMinusBtn.interactable = interactable;
        if (_sellPlusBtn != null) _sellPlusBtn.interactable = interactable;
        if (_sellMaxBtn != null) _sellMaxBtn.interactable = interactable;
        if (_sellAmountInput != null) _sellAmountInput.interactable = interactable;
    }

    private void OnClickEquip()
    {
        if (_currentEquipmentInstance == null) return;
        if (CharacterSelectionManager.Instance == null) return;
        if (PersistentCharacterManager.Instance == null) return;

        CharacterType selected = CharacterSelectionManager.Instance.GetSelected();
        string characterId = selected.ToString();

        if (PersistentCharacterManager.Instance.TryGetCharacter(characterId, out PersistentCharacterUnit unit) == false)
        {
            Debug.LogWarning($"[InventoryDetailController] PersistentCharacterUnit를 찾을 수 없음: {selected}");
            return;
        }

        CharacterEquipment target = unit.CharacterEquipment;

        if (target == null)
        {
            Debug.LogWarning($"[InventoryDetailController] CharacterEquipment를 찾을 수 없음: {selected}");
            return;
        }

        target.Equip(_currentEquipmentInstance);
        Hide();
    }

    private void OnClickUnequip()
    {
        if (_currentEquipmentInstance == null) return;
        if (CharacterSelectionManager.Instance == null) return;
        if (PersistentCharacterManager.Instance == null) return;

        CharacterType selected = CharacterSelectionManager.Instance.GetSelected();
        string characterId = selected.ToString();

        if (PersistentCharacterManager.Instance.TryGetCharacter(characterId, out PersistentCharacterUnit unit) == false)
        {
            Debug.LogWarning($"[InventoryDetailController] PersistentCharacterUnit를 찾을 수 없음: {selected}");
            return;
        }

        CharacterEquipment target = unit.CharacterEquipment;

        if (target == null)
        {
            Debug.LogWarning($"[InventoryDetailController] CharacterEquipment를 찾을 수 없음: {selected}");
            return;
        }

        EquipSlotType slot = _currentEquipmentInstance.baseData.equipSlotType;
        target.Unequip(slot);
        Hide();
    }

    private void OnClickSellConfirm()
    {
        if (PlayerInventory.Instance == null) return;

        // 장비 판매 (개체 단위, 수량 항상 1)
        if (_currentEquipmentInstance != null)
        {
            bool equipSellSuccess = PlayerInventory.Instance.TrySellEquipment(_currentEquipmentInstance);
            if (equipSellSuccess)
            {
                // 개체 하나가 통째로 사라지므로 바로 패널 닫기
                Hide();
            }
            return;
        }

        // 소비/재료/씨앗/퀘스트 판매 (수량 기반)
        if (_currentItemData == null) return;

        bool success = PlayerInventory.Instance.TrySell(_currentItemData, _currentSellAmount);
        if (!success) return;

        // 다 팔아서 더 이상 보유하고 있지 않으면 패널 자체를 닫는다
        int remaining = PlayerInventory.Instance.GetTotalQuantity(_currentItemData);
        if (remaining <= 0)
        {
            Hide();
            return;
        }

        // 아직 남아있으면 BtnsWrap으로 복귀
        CrossFadeToBtnsWrap();
    }

    /// <summary>
    /// 선택된 마도서를 사용하고 결과를 가챠 Overlay로 전달한다.
    /// 실제 인벤토리 차감은 SkillBookUseService.Use() 내부에서 한 번만 처리한다.
    /// </summary>
    private void OnClickUse()
    {
        if (_currentItemData == null) return;
        if (PlayerInventory.Instance == null) return;

        // Category만 믿지 않고 실제 데이터 타입까지 확인한다.
        SkillBookItemData skillBook = _currentItemData as SkillBookItemData;

        if (skillBook == null)
        {
            Debug.LogWarning($"[InventoryDetailController] {_currentItemData.itemName}은 SkillBookItemData가 아닙니다.");
            return;
        }

        if (_skillGachaOverlayController == null)
        {
            Debug.LogError("[InventoryDetailController] SkillGachaOverlayController가 연결되지 않았습니다.");
            return;
        }

        if (_skillGachaOverlayController.IsOpen || _skillGachaOverlayController.IsPlaying)
        {
            return;
        }

        if (_useBtn != null)
        {
            _useBtn.interactable = false;
        }

        SkillBookResult result = SkillBookUseService.Use(skillBook);

        if (result.Success == false)
        {
            if (_useBtn != null) _useBtn.interactable = true;
            return;
        }

        int remaining = PlayerInventory.Instance.GetTotalQuantity(skillBook);

        if (remaining <= 0)
        {
            Hide();
        }

        bool started = _skillGachaOverlayController.Play(skillBook, result, () =>
        {
            if (_useBtn == null) return;

            bool stillSelected = _currentItemData == skillBook;
            bool stillOwned = PlayerInventory.Instance != null
                && PlayerInventory.Instance.GetTotalQuantity(skillBook) > 0;

            _useBtn.interactable = stillSelected && stillOwned;
        });

        if (started == false && _useBtn != null)
        {
            _useBtn.interactable = remaining > 0;
        }
    }

    // ===================== 스탯 표시 =====================

    /// <summary>
    /// 0이 아닌 스탯만 골라서 EquipStatDetailRow를 동적으로 생성. 강화가 반영된 cachedStats 기준.
    /// </summary>
    private void BuildStatRows(EquipStatCalculator.StatSet stats)
    {
        ClearStatRows();

        if (_statRowPrefab == null || _statsRoot == null) return;

        AddIntStatRow("체력", stats.hp);
        AddIntStatRow("마나", stats.mp);
        AddIntStatRow("공격력", stats.spellPower);
        AddIntStatRow("지능", stats.intelligence);
        AddIntStatRow("방어력", stats.defense);
        AddIntStatRow("속도", stats.speed);
        AddIntStatRow("행운", stats.luck);
    }

    private void AddIntStatRow(string label, int value)
    {
        if (value == 0) return;

        CreateStatRow(label, $"+{value}");
    }

    private void CreateStatRow(string label, string valueText)
    {
        EquipStatDetailRow row = Instantiate(_statRowPrefab, _statsRoot);
        row.Setup(label, valueText);
        _spawnedStatRows.Add(row.gameObject);
    }

    private void ClearStatRows()
    {
        foreach (var rowObj in _spawnedStatRows)
        {
            Destroy(rowObj);
        }
        _spawnedStatRows.Clear();
    }
}