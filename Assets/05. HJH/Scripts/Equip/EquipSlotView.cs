using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EquipLeftSlots/EquipRightSlots 아래 슬롯 하나(WeaponSlot, RobeSlot 등)에 붙는 스크립트.
/// 장착된 장비가 있으면 아이콘 + 강화단계(1강 이상일 때만)를 보여주고,
/// 없으면 SampleTxt(빈 슬롯 안내 텍스트)를 보여준다.
/// 클릭하면 장착된 장비 정보를 콜백으로 넘겨줌 (ItemDetailPanel에 표시하는 용도).
/// </summary>
public class EquipSlotView : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject _sampleTextObject; // SampleTxt (빈 슬롯일 때)
    [SerializeField] private GameObject _iconObject;       // Icon (장착 시)
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _enhanceLevelText; // EnhanceLevelTxt
    [SerializeField] private Button _slotButton;

    public EquipmentInstance EquippedInstance { get; private set; }

    private Action<EquipmentInstance> _onClickCallback;

    private void Awake()
    {
        if (_slotButton != null)
        {
            _slotButton.onClick.AddListener(HandleClick);
        }
    }

    /// <summary>
    /// 장착된 장비 정보로 슬롯을 채운다. null이면 빈 슬롯 상태로 표시.
    /// </summary>
    public void Setup(EquipmentInstance equippedInstance, Action<EquipmentInstance> onClick)
    {
        EquippedInstance = equippedInstance;
        _onClickCallback = onClick;

        bool isEquipped = equippedInstance != null && equippedInstance.baseData != null;

        if (_sampleTextObject != null) _sampleTextObject.SetActive(!isEquipped);
        if (_iconObject != null) _iconObject.SetActive(isEquipped);

        if (!isEquipped) return;

        if (_iconImage != null)
        {
            _iconImage.sprite = equippedInstance.baseData.icon;
        }

        if (_enhanceLevelText != null)
        {
            // 0강이면 표시 안 함, 1강 이상일 때만 "+n"
            if (equippedInstance.enhanceLevel > 0)
            {
                _enhanceLevelText.gameObject.SetActive(true);
                _enhanceLevelText.text = $"+{equippedInstance.enhanceLevel}";
            }
            else
            {
                _enhanceLevelText.gameObject.SetActive(false);
            }
        }
    }

    private void HandleClick()
    {
        Debug.Log($"[EquipSlotView] 클릭됨. EquippedInstance: {(EquippedInstance == null ? "null" : EquippedInstance.baseData.itemName)}");

        if (EquippedInstance == null) return;
        _onClickCallback?.Invoke(EquippedInstance);
    }
}