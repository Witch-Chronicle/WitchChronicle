using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DetailSection UI 컨트롤러.
/// - ShopItemSlot 클릭 시 아이템 정보 바인딩
/// - 수량 조절 (Minus/Plus/Max/직접입력) 및 수량에 따른 가격 계산
/// - PurchaseBtn 클릭 시 PlayerInventory를 통해 실제 구매 처리
/// </summary>
public class ShopDetailController : MonoBehaviour
{
    private const int _minPurchaseAmount = 1;

    // TODO: 재화/인벤토리 시스템 완성되면 실제 로직(소지금 기준 or 인벤토리 여유공간 기준)으로 교체
    private const int _tempMaxPurchaseAmount = 99;

    [Header("Info/BaseInfo")]
    [SerializeField] private GameObject _infoSection;   // Info 전체 (선택 전 비활성화)
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _itemTypeText;
    [SerializeField] private TextMeshProUGUI _itemGradeText;
    [SerializeField] private TextMeshProUGUI _requiredLevelText;

    [Header("Info/DetailInfo - Description")]
    [SerializeField] private GameObject _descriptionObject;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Header("Info/DetailInfo - EquipStat")]
    [Tooltip("보이기/숨기기를 토글할 오브젝트")]
    [SerializeField] private GameObject _equipStatContainer;
    [Tooltip("EquipStatDetailRow들이 실제로 생성될 부모 (VerticalLayoutGroup이 붙은 오브젝트)")]
    [SerializeField] private Transform _equipStatRoot;
    [SerializeField] private EquipStatDetailRow _statRowPrefab;

    private readonly List<GameObject> _spawnedStatRows = new List<GameObject>();

    [Header("Purchase/Amount")]
    [SerializeField] private Button _minusBtn;
    [SerializeField] private Button _plusBtn;
    [SerializeField] private Button _maxBtn;
    [SerializeField] private TMP_InputField _amountInput;

    [Header("Purchase/Price")]
    [SerializeField] private TextMeshProUGUI _priceText;

    [Header("Purchase/PurchaseBtn")]
    [SerializeField] private Button _purchaseBtn;

    [Header("Shop Controller")]
    [SerializeField] private ShopController _shopController;

    private ItemData _currentItemData;
    private int _currentAmount;

    private void Awake()
    {
        _minusBtn.onClick.AddListener(OnClickMinus);
        _plusBtn.onClick.AddListener(OnClickPlus);
        _maxBtn.onClick.AddListener(OnClickMax);
        _purchaseBtn.onClick.AddListener(OnClickPurchase);
        _amountInput.onEndEdit.AddListener(OnAmountInputEndEdit);

        // 아무것도 선택되지 않은 초기 상태
        HideDetail();
    }

    /// <summary>
    /// 아이템이 선택되기 전 초기 상태.
    /// Info는 비워두고, Purchase 관련 버튼들은 비활성화.
    /// </summary>
    public void HideDetail()
    {
        _currentItemData = null;

        if (_infoSection != null)
        {
            _infoSection.SetActive(false);
        }

        SetPurchaseInteractable(false);

        if (_equipStatContainer != null) _equipStatContainer.SetActive(false);
        ClearStatRows();

        if (_amountInput != null) _amountInput.SetTextWithoutNotify("0");
        if (_priceText != null) _priceText.text = "0";
    }

    /// <summary>
    /// 아이템 슬롯 클릭 시 호출. 아이템 정보를 채우고 수량을 1로 초기화.
    /// </summary>
    public void ShowItemDetail(ItemData itemData)
    {
        _currentItemData = itemData;
        _currentAmount = _minPurchaseAmount;

        if (_infoSection != null)
        {
            _infoSection.SetActive(true);
        }

        if (_iconImage != null) _iconImage.sprite = itemData.icon;
        if (_nameText != null) _nameText.text = itemData.itemName;
        if (_itemTypeText != null) _itemTypeText.text = itemData.itemType.ToDisplayString();
        if (_itemGradeText != null) _itemGradeText.text = itemData.itemGrade.ToDisplayString();

        // Description은 아이템 종류 상관없이 항상 표시
        if (_descriptionObject != null) _descriptionObject.SetActive(true);
        if (_descriptionText != null) _descriptionText.text = itemData.description;

        if (itemData is EquipItemData equipItemData)
        {
            // 장비 아이템일 때만 EquipStat 추가로 표시
            if (_equipStatContainer != null) _equipStatContainer.SetActive(true);

            BuildStatRows(equipItemData);
        }
        else
        {
            if (_equipStatContainer != null) _equipStatContainer.SetActive(false);
            ClearStatRows();
        }

        if (_requiredLevelText != null)
        {
            // 장비가 아니면 착용 레벨이 없으므로 표시하지 않음
            if (itemData is EquipItemData equipItemForLevel)
            {
                _requiredLevelText.gameObject.SetActive(true);
                _requiredLevelText.text = $"Lv.{equipItemForLevel.requiredLevel}";
            }
            else
            {
                _requiredLevelText.gameObject.SetActive(false);
            }
        }

        SetPurchaseInteractable(true);
        UpdateAmountAndPrice();
    }

    private void OnClickMinus()
    {
        if (_currentItemData == null) return;

        SetAmount(_currentAmount - 1);
    }

    private void OnClickPlus()
    {
        if (_currentItemData == null) return;

        SetAmount(_currentAmount + 1);
    }

    private void OnClickMax()
    {
        if (_currentItemData == null) return;

        SetAmount(_tempMaxPurchaseAmount);
    }

    /// <summary>
    /// AmountTxt(TMP_InputField)에 직접 숫자를 입력하고 포커스를 벗어났을 때(엔터 포함) 호출됨.
    /// 1~99 범위로 clamp 처리.
    /// </summary>
    private void OnAmountInputEndEdit(string inputText)
    {
        if (_currentItemData == null) return;

        if (!int.TryParse(inputText, out int parsedAmount))
        {
            // 숫자가 아니거나 빈 값이면 기존 수량으로 되돌림
            parsedAmount = _currentAmount;
        }

        SetAmount(parsedAmount);
    }

    /// <summary>
    /// 수량을 1~99 범위로 clamp한 뒤 텍스트/가격을 갱신.
    /// </summary>
    private void SetAmount(int amount)
    {
        _currentAmount = Mathf.Clamp(amount, _minPurchaseAmount, _tempMaxPurchaseAmount);
        UpdateAmountAndPrice();
    }

    private void OnClickPurchase()
    {
        if (_currentItemData == null || _shopController == null) return;

        bool success = _shopController.TryPurchase(_currentItemData, _currentAmount);

        if (success)
        {
            // 구매 후 수량은 다시 1로 초기화
            SetAmount(_minPurchaseAmount);
        }
    }

    private void UpdateAmountAndPrice()
    {
        if (_amountInput != null)
        {
            // SetTextWithoutNotify: 값 갱신 시 onEndEdit/onValueChanged가 다시 호출되는 걸 방지
            _amountInput.SetTextWithoutNotify(_currentAmount.ToString());
        }

        if (_priceText != null)
        {
            int totalPrice = _currentItemData.buyPrice * _currentAmount;
            _priceText.text = totalPrice.ToString();
        }
    }

    private void SetPurchaseInteractable(bool interactable)
    {
        _minusBtn.interactable = interactable;
        _plusBtn.interactable = interactable;
        _maxBtn.interactable = interactable;
        _purchaseBtn.interactable = interactable;
        _amountInput.interactable = interactable;
    }

    /// <summary>
    /// 0이 아닌 스탯만 골라서 EquipStatDetailRow를 동적으로 생성.
    /// </summary>
    private void BuildStatRows(EquipItemData equipItemData)
    {
        ClearStatRows();

        if (_statRowPrefab == null || _equipStatRoot == null) return;

        AddIntStatRow("체력", equipItemData.hpBonus);
        AddIntStatRow("마나", equipItemData.mpBonus);
        AddIntStatRow("공격력", equipItemData.spellPowerBonus);
        AddIntStatRow("지능", equipItemData.intelligenceBonus);
        AddIntStatRow("방어력", equipItemData.defenseBonus);
        AddIntStatRow("속도", equipItemData.speedBonus);
        AddIntStatRow("행운", equipItemData.luckBonus);
    }

    private void AddIntStatRow(string label, int value)
    {
        if (value == 0) return;

        CreateStatRow(label, $"+{value}");
    }

    private void CreateStatRow(string label, string valueText)
    {
        EquipStatDetailRow row = Instantiate(_statRowPrefab, _equipStatRoot);
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