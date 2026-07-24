using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceButton : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _text;

    private Button _button;

    private DialogueChoice _choice;


    /// <summary>
    /// 초기화
    /// </summary>
    private void Awake()
    {
        _button = GetComponent<Button>();

        _button.onClick.AddListener(OnClick);
    }


    /// <summary>
    /// 선택지 설정
    /// </summary>
    public void Setup(DialogueChoice choice)
    {
        _choice = choice;

        _text.text = choice.text;
    }


    /// <summary>
    /// 선택 입력
    /// </summary>
    private void OnClick()
    {
        DialogueManager.Instance.SelectChoice(_choice);
    }
}