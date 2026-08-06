using UnityEngine;
using DG.Tweening;

/// <summary>
/// 우측 하단 고정 MainHUDPanel의 슬라이드 인/아웃.
/// Tab 키의 QuestListUI와 동일한 패턴:
/// - Open()/Close(): 즉시 위치 이동(애니메이션 없음), Blur 등 외부 시스템이 사용
/// - ToggleSlide(): T 입력에 의한 애니메이션 슬라이드 전환, IsHidden 상태 관리
/// </summary>
public class MainHUDUIController : MonoBehaviour
{
    public static MainHUDUIController Instance { get; private set; }

    [Header("Slide Panel")]
    [SerializeField] private RectTransform _panelRect;
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private Ease _slideEase = Ease.OutQuad;
    [Tooltip("패널 너비만큼이 아니라 그보다 이만큼 더 밀려나가도록")]
    [SerializeField] private float _hiddenExtraOffset = 40f;

    /// <summary>T키 토글 기준 현재 숨겨진 상태인지.</summary>
    public bool IsHidden { get; private set; }

    private float _shownX;
    private float _hiddenX;
    private bool _isInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        if (_panelRect == null)
        {
            _panelRect = transform as RectTransform;
        }

        if (_panelRect != null)
        {
            _shownX = _panelRect.anchoredPosition.x;
            _hiddenX = _shownX + _panelRect.rect.width + _hiddenExtraOffset;
        }

        _isInitialized = true;
    }

    private void OnDestroy()
    {
        if (_panelRect != null)
        {
            _panelRect.DOKill();
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 즉시 표시. Blur 해제 등에서 사용. IsHidden 상태는 건드리지 않는다.
    /// </summary>
    public void Open()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 즉시 숨김. Blur 표시 등에서 사용. IsHidden 상태는 건드리지 않는다.
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// T 키 토글: 우측으로 슬라이드 아웃 <-> 제자리로 슬라이드 인.
    /// SetActive를 쓰지 않고 위치만 이동시켜서 애니메이션이 끊기지 않게 함.
    /// </summary>
    public void ToggleSlide()
    {
        EnsureInitialized();

        if (_panelRect == null) return;

        IsHidden = !IsHidden;

        float targetX = IsHidden ? _hiddenX : _shownX;

        _panelRect.DOKill();
        _panelRect
            .DOAnchorPosX(targetX, _slideDuration)
            .SetEase(_slideEase);
    }
}