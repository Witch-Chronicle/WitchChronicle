using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 가챠 창 오른쪽 목록의 마도서 한 칸.
/// 아이콘·이름·보유 개수를 표시하고, 클릭하면 선택을 알린다.
/// </summary>
public class SkillGachaBookSlot : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Button _button;

    [Header("선택 표시")]
    [SerializeField] private GameObject _selectedMark;

    private SkillBookItemData _book;
    private Action<SkillBookItemData> _onClicked;

    /// <summary>이 칸이 표시 중인 마도서.</summary>
    public SkillBookItemData Book => _book;

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
    /// <param name="book">마도서</param>
    /// <param name="count">보유 개수</param>
    /// <param name="onClicked">클릭 콜백</param>
    public void Bind(SkillBookItemData book, int count, Action<SkillBookItemData> onClicked)
    {
        _book = book;
        _onClicked = onClicked;

        gameObject.SetActive(true);

        if (_iconImage != null)
        {
            _iconImage.sprite = book != null ? book.icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_nameText != null)
        {
            _nameText.text = book != null ? book.itemName : string.Empty;
        }

        if (_countText != null)
        {
            _countText.text = $"x{count}";
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

    /// <summary>이 칸 숨기기(목록이 줄었을 때).</summary>
    public void Clear()
    {
        _book = null;
        gameObject.SetActive(false);
    }

    private void HandleClick()
    {
        if (_book != null)
        {
            _onClicked?.Invoke(_book);
        }
    }
}
