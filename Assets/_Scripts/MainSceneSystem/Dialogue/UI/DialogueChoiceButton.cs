using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 대화 선택지 버튼(DialogueButton_v1)에 부착.
/// - Base가 기본 상태, Hover 또는 클릭 시 Selected로 전환.
/// - ChoiceTxt는 Base/Selected 공용이라 항상 그대로 둠(색상 변경 없음).
/// </summary>
public class DialogueChoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Text")]
    [SerializeField] private TMP_Text _text;

    [Header("선택 상태 표시 (Base/Selected 번갈아 활성화)")]
    [SerializeField] private GameObject _baseObject;
    [SerializeField] private GameObject _selectedObject;

    private Button _button;
    private DialogueChoice _choice;

    /// <summary>
    /// 초기화
    /// </summary>
    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);

        SetSelected(false);
    }

    /// <summary>
    /// 선택지 설정
    /// </summary>
    public void Setup(DialogueChoice choice)
    {
        _choice = choice;
        _text.text = choice.text;

        SetSelected(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetSelected(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetSelected(false);
    }

    /// <summary>
    /// Base/Selected 토글. true면 Selected만 활성, false면 Base만 활성.
    /// </summary>
    private void SetSelected(bool isSelected)
    {
        if (_baseObject != null) _baseObject.SetActive(!isSelected);
        if (_selectedObject != null) _selectedObject.SetActive(isSelected);
    }

    /// <summary>
    /// 선택 입력
    /// </summary>
    private void OnClick()
    {
        // 클릭 시에도 Selected 상태로 표시 (다음 노드로 넘어가면서 이 버튼 자체는 곧 파괴됨)
        SetSelected(true);

        DialogueManager.Instance.SelectChoice(_choice);
    }
}