using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투 스킬 선택 버튼
/// </summary>
public class BattleSkillButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _skillNameText;

    private SkillData _skillData;
    private Action<SkillData> _onClickSkill;

    /// <summary>
    /// 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    /// <summary>
    /// 스킬 버튼 초기화
    /// </summary>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="canUse">사용 가능 여부</param>
    /// <param name="onClickSkill">클릭 콜백</param>
    public void Initialize(SkillData skillData, bool canUse, Action<SkillData> onClickSkill)
    {
        _skillData = skillData;
        _onClickSkill = onClickSkill;

        if (_skillNameText != null)
        {
            if (_skillData != null)
            {
                _skillNameText.text = $"{_skillData.SkillName} MP {_skillData.MpCost}";
            }
            else
            {
                _skillNameText.text = "None";
            }
        }

        if (_button != null)
        {
            _button.interactable = canUse;
        }
    }

    /// <summary>
    /// 버튼 클릭 처리
    /// </summary>
    private void HandleClick()
    {
        if (_skillData == null)
        {
            return;
        }

        _onClickSkill?.Invoke(_skillData);
    }
}