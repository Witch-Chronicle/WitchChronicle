using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SkillEquipPanel의 보유 스킬 목록 한 줄. ObjectPool로 재사용(Destroy 없이 SetActive 토글).
/// </summary>
public class SkillEquipOwnedSlot : MonoBehaviour
{
    [SerializeField] private GameObject _base;
    [SerializeField] private GameObject _selected;
    [SerializeField] private Image _skillIcon;
    [SerializeField] private TMP_Text _skillNameTxt;
    [SerializeField] private TMP_Text _skillTierTxt;
    [SerializeField] private GameObject _equippedStatus;
    [SerializeField] private TMP_Text _equippedStatusTxt;
    [SerializeField] private Button _button;

    private SkillData _skill;
    private Action<SkillData> _onClicked;

    public SkillData Skill => _skill;

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

    /// <param name="equippedByName">장착 중인 캐릭터 이름. 아무도 장착 안 했으면 null/empty</param>
    public void Bind(SkillData skill, bool isSelected, string equippedByName, Action<SkillData> onClicked)
    {
        _skill = skill;
        _onClicked = onClicked;

        gameObject.SetActive(true);

        if (_skillIcon != null)
        {
            _skillIcon.sprite = skill != null ? skill.SkillIcon : null;
            _skillIcon.enabled = _skillIcon.sprite != null;
        }

        if (_skillNameTxt != null)
        {
            _skillNameTxt.text = skill != null ? skill.SkillName : string.Empty;
        }

        if (_skillTierTxt != null)
        {
            _skillTierTxt.text = skill != null ? SkillTextFormatter.GetTierText(skill.Tier) : string.Empty;
        }

        bool isEquipped = string.IsNullOrEmpty(equippedByName) == false;

        if (_equippedStatus != null)
        {
            _equippedStatus.SetActive(isEquipped);
        }

        if (_equippedStatusTxt != null)
        {
            _equippedStatusTxt.text = isEquipped ? $"{equippedByName} 장착중" : string.Empty;
        }

        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (_base != null) _base.SetActive(!isSelected);
        if (_selected != null) _selected.SetActive(isSelected);
    }

    /// <summary>풀로 반환하기 전 리셋.</summary>
    public void ResetSlot()
    {
        _skill = null;
        _onClicked = null;
        gameObject.SetActive(false);
    }

    private void HandleClick()
    {
        if (_skill != null)
        {
            _onClicked?.Invoke(_skill);
        }
    }
}