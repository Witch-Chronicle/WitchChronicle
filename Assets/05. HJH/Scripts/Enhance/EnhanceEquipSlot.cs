using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EnhancePanel/EquipList 쪽 Prefab_EnhanceEquipSlot에 붙는 스크립트.
/// 아이콘 / 이름 / 현재 강화 단계를 표시하고, 클릭하면 강화 미리보기 대상으로 선택됨.
/// </summary>
[RequireComponent(typeof(Button))]
public class EnhanceEquipSlot : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _enhanceLv;
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
    }

    public void Setup(EquipmentInstance equipmentInstance, Action<EquipmentInstance> onClick)
    {
        EquipmentInstance = equipmentInstance;
        _onClickCallback = onClick;

        if (_icon != null)
        {
            _icon.sprite = equipmentInstance.baseData.icon;
        }

        if (_name != null)
        {
            _name.text = equipmentInstance.baseData.itemName;
        }

        if (_enhanceLv != null)
        {
            _enhanceLv.text = $"+{equipmentInstance.enhanceLevel}";
        }
    }

    /// <summary>
    /// 선택된 슬롯 강조 표시용. Background 색만 바꾸는 간단한 버전.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (_background == null) return;

        _background.color = isSelected ? new Color(1f, 1f, 1f, 0.4f) : new Color(1f, 1f, 1f, 0f);
    }

    private void HandleClick()
    {
        _onClickCallback?.Invoke(EquipmentInstance);
    }
}