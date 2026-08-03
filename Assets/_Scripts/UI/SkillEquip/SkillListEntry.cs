using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 보유 스킬 목록의 한 줄. 아이콘·이름·티어를 표시하고 클릭 시 알린다.
/// 이미 장착 중이면 표시를 다르게 한다.
/// </summary>
public class SkillListEntry : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _tierText;
    [SerializeField] private Button _button;

    [Header("장착 중 표시")]
    [SerializeField] private GameObject _equippedMark;

    [Header("티어별 색상 (0=1티어 ... 3=4티어)")]
    [SerializeField]
    private Color[] _tierColors =
    {
        new Color(1f, 0.85f, 0.3f),   // 1티어 금색
        new Color(0.8f, 0.5f, 1f),    // 2티어 보라
        new Color(0.4f, 0.7f, 1f),    // 3티어 파랑
        new Color(0.8f, 0.8f, 0.8f),  // 4티어 회색
    };

    private SkillData _skill;
    private Action<SkillData> _onClicked;

    /// <summary>이 줄이 표시 중인 스킬.</summary>
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

    /// <summary>줄 내용 채우기.</summary>
    /// <param name="skill">표시할 스킬</param>
    /// <param name="isEquipped">현재 캐릭터가 장착 중인지</param>
    /// <param name="onClicked">클릭 콜백</param>
    public void Bind(SkillData skill, bool isEquipped, Action<SkillData> onClicked)
    {
        _skill = skill;
        _onClicked = onClicked;

        gameObject.SetActive(true);

        if (_iconImage != null)
        {
            _iconImage.sprite = skill != null ? skill.SkillIcon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_nameText != null)
        {
            _nameText.text = skill != null ? skill.SkillName : string.Empty;
        }

        if (_tierText != null)
        {
            _tierText.text = skill != null ? $"{skill.Tier}티어" : string.Empty;
            _tierText.color = skill != null ? GetTierColor(skill.Tier) : Color.white;
        }

        if (_equippedMark != null)
        {
            _equippedMark.SetActive(isEquipped);
        }
    }

    /// <summary>이 줄 숨기기.</summary>
    public void Clear()
    {
        _skill = null;
        gameObject.SetActive(false);
    }

    /// <summary>티어(1이 최상)에 대응하는 색.</summary>
    private Color GetTierColor(int tier)
    {
        if (_tierColors == null || _tierColors.Length == 0)
        {
            return Color.white;
        }

        int index = Mathf.Clamp(tier - 1, 0, _tierColors.Length - 1);
        return _tierColors[index];
    }

    private void HandleClick()
    {
        if (_skill != null)
        {
            _onClicked?.Invoke(_skill);
        }
    }
}
