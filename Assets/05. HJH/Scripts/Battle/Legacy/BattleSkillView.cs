using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab_BattleSkill에 붙는 뷰. 스킬 정보 표시 + 클릭 시 콜백 전달만 담당.
/// - MP 부족 등으로 사용 불가능한 스킬은 Button.interactable을 꺼서 클릭 자체를 막음.
/// </summary>
public class BattleSkillView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _typeTxt;
    [SerializeField] private TMP_Text _requiredMpTxt;
    [SerializeField] private Button _button;

    private SkillData _skillData;
    private Action<SkillData> _onClick;

    private void Awake()
    {
        if (_button != null) _button.onClick.AddListener(HandleClick);
    }

    public void Bind(SkillData skillData, bool canUse, Action<SkillData> onClick)
    {
        _skillData = skillData;
        _onClick = onClick;

        if (skillData == null) return;

        if (_nameTxt != null) _nameTxt.text = skillData.SkillName;
        if (_typeTxt != null) _typeTxt.text = skillData.SkillType.ToString();
        if (_requiredMpTxt != null) _requiredMpTxt.text = $"MP {skillData.MpCost}";

        if (_button != null) _button.interactable = canUse;
    }

    private void HandleClick()
    {
        _onClick?.Invoke(_skillData);
    }
}