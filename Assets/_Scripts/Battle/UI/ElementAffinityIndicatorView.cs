using TMPro;
using UnityEngine;

/// <summary>
/// 적 프리팹에 부착. 스킬 대상으로 지정된 동안에만, 그 스킬의 속성이 이 적의 약점/저항인지 텍스트로 표시.
/// 평소엔 캔버스 자체가 완전히 꺼져있다가 BattleTargetCycler가 스킬 타겟팅 중에만
/// ShowWeak()/ShowResist()/Hide() 호출.
/// - EnemyTargetOverlay와 동일하게 매 프레임 카메라를 향해 회전(billboard).
/// </summary>
public class ElementAffinityIndicatorView : MonoBehaviour
{
    [Header("Root (텍스트뿐 아니라 배경/프레임 등 캔버스 전체를 포함하는 루트)")]
    [Tooltip("비워두면 이 컴포넌트가 붙은 오브젝트 자신을 루트로 사용")]
    [SerializeField] private GameObject _canvasRoot;

    [SerializeField] private TMP_Text _text;

    [Header("Billboard (카메라 향해 회전)")]
    [SerializeField] private bool _billboard = true;

    [Header("표시 문구")]
    [SerializeField] private string _weakText = "약점";
    [SerializeField] private string _resistText = "저항";

    [Header("색상")]
    [SerializeField] private Color _weakColor = Color.red;
    [SerializeField] private Color _resistColor = new Color(0.4f, 0.8f, 1f);

    private GameObject Root => _canvasRoot != null ? _canvasRoot : gameObject;

    private void Awake()
    {
        Hide();
    }

    private void LateUpdate()
    {
        if (_billboard == false) return;
        if (Camera.main == null) return;

        transform.rotation = Camera.main.transform.rotation;
    }

    public void ShowWeak()
    {
        SetTextAndShow(_weakText, _weakColor);
    }

    public void ShowResist()
    {
        SetTextAndShow(_resistText, _resistColor);
    }

    public void Hide()
    {
        Root.SetActive(false);
    }

    private void SetTextAndShow(string text, Color color)
    {
        if (_text != null)
        {
            _text.text = text;
            _text.color = color;
        }

        Root.SetActive(true);
    }
}