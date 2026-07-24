using TMPro;
using UnityEngine;

/// <summary>
/// StatTooltip 오브젝트에 붙는 컨트롤러. 호버된 대상(LabelTxt)의 우하단 모서리 근처에 텍스트 표시/숨김을 담당.
/// - target RectTransform의 우하단 모서리(GetWorldCorners 기준 index 3)를 화면 좌표로 변환한 뒤,
///   그 지점 근처에 툴팁을 배치.
/// - StatTooltip의 Pivot(0,1) 기준으로, 그 지점 오른쪽 아래로 펼쳐지도록 위치 계산.
/// </summary>
public class StatTooltipController : MonoBehaviour
{
    public static StatTooltipController Instance { get; private set; }

    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private TMP_Text _tooltipTxt;
    [SerializeField] private Vector2 _cornerOffset = new Vector2(8f, -8f); // 모서리에서 살짝 띄우는 여백

    private RectTransform _parentRect;
    private Canvas _canvas;

    private void Awake()
    {
        Instance = this;

        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        _parentRect = _rectTransform.parent as RectTransform;
        _canvas = GetComponentInParent<Canvas>();

        Hide();
    }

    /// <summary>
    /// targetRect(호버된 LabelTxt)의 우하단 모서리 근처에 툴팁을 표시.
    /// </summary>
    public void Show(string text, RectTransform targetRect, Camera eventCamera)
    {
        if (string.IsNullOrEmpty(text) || targetRect == null) return;

        if (_tooltipTxt != null)
        {
            _tooltipTxt.text = text;
        }

        gameObject.SetActive(true);
        UpdatePosition(targetRect, eventCamera);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdatePosition(RectTransform targetRect, Camera eventCamera)
    {
        if (_canvas == null || _parentRect == null || _rectTransform == null) return;

        // targetRect의 4개 월드 코너: [0]좌하단 [1]좌상단 [2]우상단 [3]우하단
        Vector3[] worldCorners = new Vector3[4];
        targetRect.GetWorldCorners(worldCorners);
        Vector3 bottomRightWorld = worldCorners[3];

        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, bottomRightWorld);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPoint + _cornerOffset, cam, out Vector2 localPoint))
        {
            _rectTransform.anchoredPosition = localPoint;
        }
    }
}