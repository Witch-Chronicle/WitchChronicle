using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// SkillEquipPanel의 보유 스킬 목록 한 줄.
/// SkillEquipOwnedScrollView(RecycledScrollView)가 뷰포트 범위만큼만 재사용/바인딩합니다.
/// </summary>
public class SkillEquipOwnedSlot : MonoBehaviour, IRecycledScrollCell<SkillOwnedSlotEntry>
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
    /// <summary>
    /// RecycledScrollView가 셀을 재사용/재배치할 때마다 호출합니다.
    /// </summary>
    public void Bind(SkillOwnedSlotEntry entry, int index)
    {
        Bind(entry.Skill, entry.IsSelected, entry.EquippedByName, entry.OnClicked);
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
    /// <summary>
    /// 이전 ObjectPool 방식에서 풀 반환 전 리셋용으로 쓰던 메서드입니다.
    /// RecycledScrollView는 셀을 Bind()로 즉시 덮어써서 재사용하므로 자동으로는 호출되지 않지만,
    /// 필요 시 외부에서 명시적으로 비우고 싶을 때 사용할 수 있어 남겨둡니다.
    /// </summary>
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