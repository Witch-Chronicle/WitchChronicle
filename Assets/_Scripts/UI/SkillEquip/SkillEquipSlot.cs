using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 장착 슬롯 한 칸. 비어 있으면 "비어 있음", 차 있으면 스킬 아이콘·이름 표시.
/// 클릭하면 슬롯 인덱스를 알린다.
/// </summary>
public class SkillEquipSlot : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Button _button;

    [Header("상태 표시")]
    [SerializeField] private GameObject _selectedMark;
    [SerializeField] private GameObject _emptyMark;

    private int _slotIndex = -1;
    private Action<int> _onClicked;

    /// <summary>이 칸의 슬롯 번호.</summary>
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

    /// <summary>슬롯 내용 채우기.</summary>
    /// <param name="slotIndex">슬롯 번호</param>
    /// <param name="skill">장착된 스킬 (없으면 null)</param>
    /// <param name="onClicked">클릭 콜백</param>
    public void Bind(int slotIndex, SkillData skill, Action<int> onClicked)
    {
        _slotIndex = slotIndex;
        _onClicked = onClicked;

        gameObject.SetActive(true);

        bool hasSkill = skill != null;

        if (_iconImage != null)
        {
            _iconImage.sprite = hasSkill ? skill.SkillIcon : null;
            _iconImage.enabled = hasSkill && _iconImage.sprite != null;
        }

        if (_nameText != null)
        {
            _nameText.text = hasSkill ? skill.SkillName : "- 비어 있음 -";
        }

        if (_emptyMark != null)
        {
            _emptyMark.SetActive(hasSkill == false);
        }

        SetSelected(false);
    }

    /// <summary>선택 표시 갱신.</summary>
    public void SetSelected(bool isSelected)
    {
        if (_selectedMark != null)
        {
            _selectedMark.SetActive(isSelected);
        }
    }

    /// <summary>이 칸 숨기기(슬롯 수가 줄었을 때).</summary>
    public void Clear()
    {
        _slotIndex = -1;
        gameObject.SetActive(false);
    }

    private void HandleClick()
    {
        if (_slotIndex >= 0)
        {
            _onClicked?.Invoke(_slotIndex);
        }
    }
}
