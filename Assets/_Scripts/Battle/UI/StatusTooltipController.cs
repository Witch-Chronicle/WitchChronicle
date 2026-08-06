using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StatusTooltip 오브젝트에 붙는 컨트롤러. 호버된 상태이상 아이콘(World Space 포함) 근처에
/// 상태이상 이름 + 설명을 표시/숨김.
/// - StatTooltipController와 동일한 위치 계산 방식(우하단 모서리 기준 좌표 변환)을 사용하되,
///   World Space Canvas 위 아이콘(EnemyTargetOverlay)에도 적용 가능하도록 eventCamera를 그대로 활용.
/// </summary>
public class StatusTooltipController : MonoBehaviour
{
    public static StatusTooltipController Instance { get; private set; }

    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private RectTransform _backgroundRect;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _descriptionTxt;
    [SerializeField] private Vector2 _cornerOffset = new Vector2(8f, -8f);

    // 기본 Pivot (우하단 방향으로 펼쳐지는 상태 기준: X=0 좌측기준->오른쪽으로, Y=1 상단기준->아래로)
    private static readonly Vector2 DefaultPivot = new Vector2(0f, 1f);

    private RectTransform _parentRect;
    private Canvas _canvas;

    private void Awake()
    {
        Instance = this;

        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        if (_backgroundRect == null) _backgroundRect = _rectTransform.Find("Background") as RectTransform;
        _parentRect = _rectTransform.parent as RectTransform;
        _canvas = GetComponentInParent<Canvas>();

        Hide();
    }

    /// <summary>
    /// targetRect(호버된 상태이상 아이콘)의 우하단 모서리 근처에 툴팁을 표시.
    /// </summary>
    public void Show(string statusName, string description, RectTransform targetRect, Camera eventCamera)
    {
        if (targetRect == null) return;

        if (_nameTxt != null) _nameTxt.text = statusName;
        if (_descriptionTxt != null) _descriptionTxt.text = description;

        gameObject.SetActive(true);

        // Content Size Fitter가 Background에 붙어있으니, 그 대상 기준으로 강제 레이아웃 갱신
        if (_backgroundRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_backgroundRect);
        }

        UpdatePosition(targetRect, eventCamera);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdatePosition(RectTransform targetRect, Camera eventCamera)
    {
        if (_parentRect == null || _rectTransform == null) return;

        Vector3[] worldCorners = new Vector3[4];
        targetRect.GetWorldCorners(worldCorners);
        Vector3 bottomRightWorld = worldCorners[3]; // 우하단 모서리 고정 기준

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, bottomRightWorld);
        screenPoint += _cornerOffset;

        float canvasScale = _canvas != null ? _canvas.scaleFactor : 1f;
        RectTransform sizeSource = _backgroundRect != null ? _backgroundRect : _rectTransform;
        Vector2 tooltipScreenSize = sizeSource.rect.size * canvasScale;

        // Pivot(0,1) 고정 기준: 기준점에서 오른쪽으로 tooltipWidth, 아래로 tooltipHeight만큼 펼쳐짐
        // 오른쪽 끝이 화면을 넘으면 width만큼 왼쪽으로 이동
        if (screenPoint.x + tooltipScreenSize.x > Screen.width)
        {
            screenPoint.x -= tooltipScreenSize.x;
        }

        // 아래쪽 끝이 화면을 넘으면(0 밑으로 내려가면) height만큼 위로 이동
        if (screenPoint.y - tooltipScreenSize.y < 0f)
        {
            screenPoint.y += tooltipScreenSize.y;
        }

        Camera parentCam = _canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (_canvas != null ? _canvas.worldCamera : null);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPoint, parentCam, out Vector2 localPoint))
        {
            _rectTransform.anchoredPosition = localPoint;
        }
    }
}