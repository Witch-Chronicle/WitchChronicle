using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대화 UI 관리
/// 대사 출력, 선택지 생성, 대화 진행 입력 처리
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Dialogue Area (Background에 Button 컴포넌트)")]
    [SerializeField] private Button _dialogueAreaButton;

    [Header("Character (CharacterWrap 하위)")]
    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _speakerText;

    [Header("Dialogue (Context 자체가 TMP)")]
    [SerializeField] private TMP_Text _dialogueText;

    [Header("Choice")]
    [SerializeField] private Transform _choiceRoot;
    [SerializeField] private Button _choicePrefab;

    /// <summary>
    /// 대화창 패널이 지금 활성 상태인지 여부.
    /// 외부 UI가 대화 흐름을 가로챘는지 확인할 때 사용합니다.
    /// </summary>
    public bool IsPanelActive =>
        _panel != null && _panel.activeSelf;

    /// <summary>
    /// 싱글톤 초기화 및 입력 연결
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_dialogueAreaButton != null)
        {
            _dialogueAreaButton.onClick.AddListener(OnClickDialogue);
        }

        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    /// <summary>
    /// 대화창 표시
    /// </summary>
    public void Show()
    {
        if (_panel == null || _panel.activeSelf)
        {
            return;
        }

        _panel.SetActive(true);
        CursorLocker.Instance?.EnterUIMode();
    }

    /// <summary>
    /// 대화창 숨김
    /// </summary>
    public void Hide()
    {
        if (_panel == null || !_panel.activeSelf)
        {
            return;
        }

        _panel.SetActive(false);
        CursorLocker.Instance?.ExitUIMode();
    }

    /// <summary>
    /// 대화 정보 갱신
    /// 마지막 노드여도 자동으로 종료하지 않습니다.
    /// </summary>
    public void Refresh(
        Sprite portrait,
        string speaker,
        string text,
        bool lastNode)
    {
        if (_portrait != null)
        {
            _portrait.sprite = portrait;
        }

        if (_speakerText != null)
        {
            _speakerText.text = speaker;
        }

        if (_dialogueText != null)
        {
            _dialogueText.text = text;
        }
    }

    /// <summary>
    /// 선택지 버튼 제거
    /// </summary>
    public void ClearChoices()
    {
        if (_choiceRoot == null)
        {
            return;
        }

        foreach (Transform child in _choiceRoot)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 선택지 버튼 생성
    /// </summary>
    public void CreateChoice(DialogueChoice choice)
    {
        if (choice == null ||
            _choicePrefab == null ||
            _choiceRoot == null)
        {
            return;
        }

        Button button =
            Instantiate(_choicePrefab, _choiceRoot);

        TMP_Text text =
            button.GetComponentInChildren<TMP_Text>();

        if (text != null)
        {
            text.text = choice.text;
        }

        button.onClick.AddListener(() =>
        {
            DialogueManager.Instance?.SelectChoice(choice);
        });
    }

    /// <summary>
    /// 대화 영역 클릭 처리
    /// </summary>
    private void OnClickDialogue()
    {
        DialogueManager.Instance?.NextDialogue();
    }

    /// <summary>
    /// 대화창 패널만 숨김.
    /// CursorLocker 상태는 건드리지 않습니다.
    /// 대화 도중 다른 UI로 전환할 때 사용합니다.
    /// </summary>
    public void HidePanelOnly()
    {
        if (_panel == null || !_panel.activeSelf)
        {
            return;
        }

        _panel.SetActive(false);
    }

    /// <summary>
    /// 대화창 패널만 다시 표시.
    /// HidePanelOnly()로 숨긴 패널을 원래대로 복원할 때 사용합니다.
    /// CursorLocker 상태는 건드리지 않습니다.
    /// </summary>
    public void ShowPanelOnly()
    {
        if (_panel == null || _panel.activeSelf)
        {
            return;
        }

        _panel.SetActive(true);
    }

    private void OnDestroy()
    {
        if (_dialogueAreaButton != null)
        {
            _dialogueAreaButton.onClick.RemoveListener(OnClickDialogue);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}