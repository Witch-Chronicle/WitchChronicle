using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 상태이상 아이콘(BattleCharacterStatusView/EnemyTargetOverlay에서 동적 생성됨)에 붙어서,
/// 마우스 호버 시 StatusTooltipController에 이름+설명 표시를 요청.
/// World Space Canvas 위 아이콘에서도 동작 (Canvas에 GraphicRaycaster + Event Camera가 세팅되어 있어야 함).
/// </summary>
public class StatusTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string _statusName;
    private string _description;
    private RectTransform _rectTransform;
    private bool _isHovering;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void SetTooltipInfo(string statusName, string description)
    {
        _statusName = statusName;
        _description = description;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;

        if (StatusTooltipController.Instance == null) return;

        StatusTooltipController.Instance.Show(_statusName, _description, _rectTransform, eventData.enterEventCamera);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltipIfHovering();
    }

    private void OnDisable()
    {
        // 호버 중인 상태에서 아이콘이 비활성화(SetActive(false))되면 OnPointerExit이 호출되지 않으므로,
        // 여기서 직접 닫아줌. Destroy 시에도 OnDisable이 먼저 호출되니 이걸로 파괴 케이스까지 커버됨.
        HideTooltipIfHovering();
    }

    private void HideTooltipIfHovering()
    {
        if (_isHovering == false) return;

        _isHovering = false;

        if (StatusTooltipController.Instance == null) return;

        StatusTooltipController.Instance.Hide();
    }
}