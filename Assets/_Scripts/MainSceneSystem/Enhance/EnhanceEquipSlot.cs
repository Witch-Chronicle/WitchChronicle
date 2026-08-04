using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EnhancePanel/EquipList/Carousel 쪽 Prefab_EnhanceEquipSlot_v1.
/// 아이콘/이름/강화 단계는 공통으로 항상 표시. Normal/Selected만 번갈아 활성화되어 선택 상태 표시.
/// </summary>
[RequireComponent(typeof(Button))]
public class EnhanceEquipSlot : MonoBehaviour
{
    [Header("공통 (항상 표시)")]
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _enhanceLv;

    [Header("선택 상태 표시 (Normal/Selected 번갈아 활성화)")]
    [SerializeField] private GameObject _normalObject;
    [SerializeField] private GameObject _selectedObject;

    [SerializeField] private Button _slotButton;

    public EquipmentInstance EquipmentInstance { get; private set; }

    private Action<EquipmentInstance> _onClickCallback;

    private void Awake()
    {
        if (_slotButton == null)
        {
            _slotButton = GetComponent<Button>();
        }

        _slotButton.onClick.AddListener(HandleClick);

        SetSelected(false);
    }

    public void Setup(EquipmentInstance equipmentInstance, Action<EquipmentInstance> onClick)
    {
        EquipmentInstance = equipmentInstance;
        _onClickCallback = onClick;

        if (_icon != null) _icon.sprite = equipmentInstance.baseData.icon;
        if (_name != null) _name.text = equipmentInstance.baseData.itemName;
        if (_enhanceLv != null) _enhanceLv.text = $"+{equipmentInstance.enhanceLevel}";

        SetSelected(false);
    }

    /// <summary>
    /// 선택 상태 토글. true면 Selected만 활성, false면 Normal만 활성.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (_normalObject != null) _normalObject.SetActive(!isSelected);
        if (_selectedObject != null) _selectedObject.SetActive(isSelected);
    }

    private void HandleClick()
    {
        _onClickCallback?.Invoke(EquipmentInstance);
    }
}