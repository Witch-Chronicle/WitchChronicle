using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 현재 퀘스트 목록 UI 관리
/// 진행 중인 퀘스트 표시 및 목록 갱신 처리
/// Tab 키로 우측 슬라이드 아웃/인 토글.
///
/// QuestItem 개수에 따라 Content 높이를 계산하여
/// Scroll View와 전체 Panel 높이를 자동으로 증가시킨다.
///
/// Scroll View는 현재 기본 높이를 최소값으로 유지하고,
/// Content가 그보다 커질 경우 제한 없이 계속 늘어난다.
/// </summary>
public class QuestListUI : MonoBehaviour
{
    public static QuestListUI Instance { get; private set; }

    [SerializeField]
    private GameObject _panel;

    [Header("Quest List")]
    [SerializeField]
    private Transform _content;

    [SerializeField]
    private QuestItem _questItemPrefab;

    [Header("Auto Size")]
    [Tooltip("Scroll View의 RectTransform")]
    [SerializeField]
    private RectTransform _scrollViewRect;

    [Tooltip("Scroll View/Viewport/Content의 RectTransform")]
    [SerializeField]
    private RectTransform _contentRect;

    [Tooltip("Scroll View의 최소 높이. 현재 기본 높이를 입력하세요.")]
    [SerializeField]
    private float _minScrollHeight = 376.3183f;

    [Tooltip("Scroll View가 늘어난 만큼 전체 패널에 더해질 기준 높이")]
    [SerializeField]
    private float _basePanelHeight;

    [Header("Slide Animation")]
    [SerializeField]
    private RectTransform _panelRect;

    [SerializeField]
    private float _slideDuration = 0.3f;

    [SerializeField]
    private Ease _slideEase = Ease.OutQuad;

    [SerializeField]
    private float _hiddenExtraOffset = 40f;

    private float _visiblePosX;
    private float _hiddenPosX;

    private float _initialPanelHeight;
    private float _initialScrollHeight;

    private bool _isInitialized;
    private bool _isOpen = true;

    private Coroutine _resizeCoroutine;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveReferences();
        CacheInitialSize();
    }

    private void ResolveReferences()
    {
        if (_panelRect == null && _panel != null)
        {
            _panelRect = _panel.GetComponent<RectTransform>();
        }

        if (_contentRect == null && _content != null)
        {
            _contentRect = _content.GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// 현재 Inspector에서 설정되어 있는
    /// Panel / Scroll View의 기본 크기를 저장한다.
    /// </summary>
    private void CacheInitialSize()
    {
        if (_panelRect != null)
        {
            _initialPanelHeight = _panelRect.rect.height;
        }

        if (_scrollViewRect != null)
        {
            _initialScrollHeight = _scrollViewRect.rect.height;

            // Inspector 값을 따로 지정하지 않았더라도
            // 현재 Scroll View Height를 최소값으로 사용
            if (_minScrollHeight <= 0f)
            {
                _minScrollHeight = _initialScrollHeight;
            }
        }

        // 기본 Panel 높이에서 Scroll View 높이를 뺀 값.
        // 즉 Title / Key / Divider / 상하 여백 등
        // Scroll View를 제외한 고정 영역의 크기.
        _basePanelHeight =
            _initialPanelHeight -
            _initialScrollHeight;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        ResolveReferences();

        if (_panelRect != null)
        {
            _visiblePosX =
                _panelRect.anchoredPosition.x;

            _hiddenPosX =
                _visiblePosX +
                _panelRect.rect.width +
                _hiddenExtraOffset;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// 현재 진행 퀘스트 목록 갱신
    /// </summary>
    public void Refresh()
    {
        Clear();

        if (QuestManager.Instance == null)
        {
            RequestResize();
            return;
        }

        List<QuestRuntime> quests =
            QuestManager.Instance.GetRunningQuests();

        foreach (QuestRuntime quest in quests)
        {
            if (quest == null)
            {
                continue;
            }

            if (quest.State == QuestState.Rewarded)
            {
                continue;
            }

            CreateItem(quest);
        }

        RequestResize();
    }

    private void CreateItem(QuestRuntime runtime)
    {
        if (_questItemPrefab == null ||
            _content == null ||
            runtime == null)
        {
            return;
        }

        QuestItem item =
            Instantiate(
                _questItemPrefab,
                _content
            );

        item.Setup(runtime);
    }

    private void Clear()
    {
        if (_content == null)
        {
            return;
        }

        foreach (Transform child in _content)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Destroy / Instantiate 이후
    /// 다음 프레임에 Layout을 다시 계산한다.
    /// </summary>
    private void RequestResize()
    {
        if (_resizeCoroutine != null)
        {
            StopCoroutine(_resizeCoroutine);
        }

        _resizeCoroutine =
            StartCoroutine(
                RefreshSizeNextFrame()
            );
    }

    private IEnumerator RefreshSizeNextFrame()
    {
        yield return null;

        RefreshPanelSize();

        _resizeCoroutine = null;
    }

    /// <summary>
    /// Content 높이에 맞춰
    /// Scroll View와 전체 Panel 높이를 증가시킨다.
    ///
    /// Scroll View는 최소 높이 이하로 줄어들지 않으며,
    /// 최대 높이 제한 없이 계속 증가한다.
    /// </summary>
    private void RefreshPanelSize()
    {
        ResolveReferences();

        if (_panelRect == null ||
            _scrollViewRect == null ||
            _contentRect == null)
        {
            return;
        }

        // Content 내부의 VerticalLayoutGroup,
        // LayoutElement 등의 계산을 즉시 반영
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            _contentRect
        );

        float contentHeight =
            LayoutUtility.GetPreferredHeight(
                _contentRect
            );

        if (contentHeight <= 0f)
        {
            contentHeight =
                _contentRect.rect.height;
        }

        // 최소 높이는 유지.
        // Content가 더 크면 제한 없이 그대로 증가.
        float targetScrollHeight =
            Mathf.Max(
                _minScrollHeight,
                contentHeight
            );

        _scrollViewRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            targetScrollHeight
        );

        // Scroll View를 제외한 나머지 영역은 그대로 유지하고
        // Scroll View가 커진 만큼 Panel도 같이 증가.
        float targetPanelHeight =
            _basePanelHeight +
            targetScrollHeight;

        _panelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            targetPanelHeight
        );

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            _panelRect
        );
    }

    public void Close()
    {
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    public void Open()
    {
        if (_panel != null)
        {
            _panel.SetActive(true);

            RequestResize();
        }
    }

    /// <summary>
    /// Tab 키 토글:
    /// 우측으로 슬라이드 아웃 <-> 제자리로 슬라이드 인.
    /// </summary>
    public void ToggleSlide()
    {
        EnsureInitialized();

        if (_panelRect == null)
        {
            return;
        }

        _isOpen = !_isOpen;

        float targetX =
            _isOpen
                ? _visiblePosX
                : _hiddenPosX;

        _panelRect.DOKill();

        _panelRect
            .DOAnchorPosX(
                targetX,
                _slideDuration
            )
            .SetEase(_slideEase);
    }

    /// <summary>
    /// Blur UI가 표시될 때 호출.
    /// 기존 Open/Close 상태는 유지하면서 패널만 잠시 숨김.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_panel == null)
        {
            return;
        }

        _panel.SetActive(visible);

        if (visible)
        {
            RequestResize();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (_panelRect != null)
        {
            _panelRect.DOKill();
        }
    }
}