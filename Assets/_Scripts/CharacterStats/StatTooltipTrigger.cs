using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스탯 LabelTxt에 붙어서, 마우스 호버 시 StatTooltipController에 표시를 요청.
/// StatUIController가 Awake 시점에 각 LabelTxt에 자동으로 추가하고 텍스트를 세팅함.
/// </summary>
public class StatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string _tooltipText;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void SetTooltipText(string text)
    {
        _tooltipText = text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(_tooltipText)) return;
        if (StatTooltipController.Instance == null) return;

        StatTooltipController.Instance.Show(_tooltipText, _rectTransform, eventData.pressEventCamera);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (StatTooltipController.Instance == null) return;

        StatTooltipController.Instance.Hide();
    }
}