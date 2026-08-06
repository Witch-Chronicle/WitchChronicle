using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SkillEquipPanel의 장착 슬롯 한 칸(고정 6개).
/// 비어있는 슬롯은 클릭이 먹히지 않는다(선택 불가).
/// </summary>
public class SkillEquipEquippedSlot : MonoBehaviour
{
    [SerializeField] private GameObject _base;
    [SerializeField] private GameObject _selected;
    [SerializeField] private Image _skillIcon;
    [SerializeField] private GameObject _emptyIcon;
    [SerializeField] private TMP_Text _slotNumberTxt;
    [SerializeField] private Button _button;

    private int _slotIndex = -1;
    private bool _hasSkill;
    private Action<int> _onClicked;

    public int SlotIndex => _slotIndex;

    private void Awake()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    public void Bind(int slotIndex, SkillData skill, bool isSelected, Action<int> onClicked)
    {
        _slotIndex = slotIndex;
        _onClicked = onClicked;
        _hasSkill = skill != null;

        gameObject.SetActive(true);

        if (_skillIcon != null)
        {
            _skillIcon.sprite = _hasSkill ? skill.SkillIcon : null;
            _skillIcon.enabled = _hasSkill && _skillIcon.sprite != null;
        }

        if (_emptyIcon != null)
        {
            _emptyIcon.SetActive(_hasSkill == false);
        }

        if (_slotNumberTxt != null)
        {
            _slotNumberTxt.text = (slotIndex + 1).ToString();
        }

        // 빈 슬롯은 클릭 자체를 막는다(선택 불가)
        if (_button != null)
        {
            _button.interactable = _hasSkill;
        }

        SetSelected(isSelected && _hasSkill);
    }

    public void SetSelected(bool isSelected)
    {
        if (_base != null) _base.SetActive(!isSelected);
        if (_selected != null) _selected.SetActive(isSelected);
    }

    public void Clear()
    {
        _slotIndex = -1;
        _hasSkill = false;
        gameObject.SetActive(false);
    }

    private void HandleClick()
    {
        if (_slotIndex >= 0 && _hasSkill)
        {
            _onClicked?.Invoke(_slotIndex);
        }
    }
}