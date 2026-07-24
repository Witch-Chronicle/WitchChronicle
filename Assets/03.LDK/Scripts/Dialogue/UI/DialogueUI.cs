using System.Collections;
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


    [Header("Dialogue Area")]
    [SerializeField] private Button _dialogueAreaButton;


    [Header("Character")]
    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _speakerText;


    [Header("Dialogue")]
    [SerializeField] private TMP_Text _dialogueText;


    [Header("Choice")]
    [SerializeField] private Transform _choiceRoot;
    [SerializeField] private Button _choicePrefab;


    [Header("Typing")]
    [SerializeField] private float _typingSpeed = 0.03f;


    private Coroutine _typingCoroutine;

    private bool _isTyping;

    private bool _lastNode;

    private string _currentText;



    /// <summary>
    /// 싱글톤 초기화 및 입력 연결
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);

            return;
        }

        Instance = this;

        _dialogueAreaButton.onClick.AddListener(OnClickDialogue);

        _panel.SetActive(false);
    }




    /// <summary>
    /// 대화창 표시
    /// </summary>
    public void Show()
    {
        if (_panel.activeSelf)
        {
            return;
        }

        _panel.SetActive(true);

        CursorLocker.Instance.EnterUIMode();
    }

    /// <summary>
    /// 대화창 숨김
    /// </summary>
    public void Hide()
    {
        if (_panel.activeSelf == false)
        {
            return;
        }

        _panel.SetActive(false);

        CursorLocker.Instance.ExitUIMode();
    }



    /// <summary>
    /// 대화 정보 갱신
    /// </summary>
    public void Refresh(Sprite portrait, string speaker, string text, bool lastNode)
    {
        _portrait.sprite = portrait;

        _speakerText.text = speaker;

        _currentText = text;

        _lastNode = lastNode;


        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }


        _typingCoroutine = StartCoroutine(Typing(text));
    }



    /// <summary>
    /// 대사 타이핑 출력
    /// </summary>
    private IEnumerator Typing(string text)
    {
        _isTyping = true;


        _dialogueText.text = "";


        foreach (char c in text)
        {
            _dialogueText.text += c;

            yield return new WaitForSeconds(_typingSpeed);
        }


        _isTyping = false;


        if (_lastNode)
        {
            yield return new WaitForSeconds(1f);

            DialogueManager.Instance.EndDialogue();
        }
    }



    /// <summary>
    /// 선택지 버튼 제거
    /// </summary>
    public void ClearChoices()
    {
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
        Button button = Instantiate(_choicePrefab, _choiceRoot);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();

        text.text = choice.text;


        button.onClick.AddListener(() =>
        {
            DialogueManager.Instance.SelectChoice(choice);
        });
    }



    /// <summary>
    /// 대화 영역 클릭 처리
    /// </summary>
    private void OnClickDialogue()
    {
        if (_isTyping)
        {
            SkipTyping();

            return;
        }


        DialogueManager.Instance.NextDialogue();
    }



    /// <summary>
    /// 타이핑 즉시 완료
    /// </summary>
    private void SkipTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }


        _dialogueText.text = _currentText;


        _isTyping = false;
    }
}