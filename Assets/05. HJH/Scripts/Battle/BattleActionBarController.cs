using UnityEngine;
using DG.Tweening;

/// <summary>
/// Btns(전투 액션 버튼 묶음)의 표시/숨김을 담당.
/// - List 패널(아이템/스킬)이 열릴 때 Hide(), 닫힐 때 Show()
/// - 순수 슬라이드로 우측 밖으로 빠졌다가 다시 들어옴 (페이드 없음)
/// * _autoReactToTurnEvents: 아군 턴 시작/종료/전투 종료에 따라 자동으로 Show()/Hide()할지 여부.
///   Btns는 켜둔 채로 사용(기본값), ConfirmCancelGroup처럼 오직 외부 호출로만 제어되어야 하는
///   패널은 이 옵션을 꺼서 턴 이벤트에 자동 반응하지 않게 함.
/// </summary>
public class BattleActionBarController : MonoBehaviour
{
    [Header("자동 반응 (Btns용, ConfirmCancelGroup 등은 꺼두세요)")]
    [SerializeField] private bool _autoReactToTurnEvents = true;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private Ease _hideEase = Ease.InQuad;
    [SerializeField] private Ease _showEase = Ease.OutQuad;

    private float _visiblePosX;
    private float _hiddenPosX;
    private bool _isInitialized;
    private bool _isSubscribed;
    private Tween _slideTween;

    /// <summary>
    /// 초기화
    /// </summary>
    private void Awake()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// 이벤트 구독 시도
    /// </summary>
    private void OnEnable()
    {
        TrySubscribeBattleUIContext();
    }

    /// <summary>
    /// 이벤트 구독 보정
    /// </summary>
    private void Start()
    {
        TrySubscribeBattleUIContext();

        if (_autoReactToTurnEvents)
        {
            RefreshByCurrentTurn();
        }
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeBattleUIContext();
        _slideTween?.Kill();
    }

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        if (_rectTransform != null)
        {
            _visiblePosX = _rectTransform.anchoredPosition.x;
            _hiddenPosX = _visiblePosX + _rectTransform.rect.width;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// BattleUIContext 이벤트 구독. _autoReactToTurnEvents가 꺼져 있으면 구독하지 않음.
    /// </summary>
    private void TrySubscribeBattleUIContext()
    {
        if (_autoReactToTurnEvents == false)
        {
            return;
        }

        if (_isSubscribed)
        {
            return;
        }

        if (BattleUIContext.Instance == null)
        {
            return;
        }

        BattleUIContext.Instance.OnTurnStarted += HandleTurnStarted;
        BattleUIContext.Instance.OnTurnEnded += HandleTurnEnded;
        BattleUIContext.Instance.OnBattleEnded += HandleBattleEnded;

        _isSubscribed = true;
    }

    /// <summary>
    /// BattleUIContext 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeBattleUIContext()
    {
        if (_isSubscribed == false)
        {
            return;
        }

        if (BattleUIContext.Instance == null)
        {
            _isSubscribed = false;
            return;
        }

        BattleUIContext.Instance.OnTurnStarted -= HandleTurnStarted;
        BattleUIContext.Instance.OnTurnEnded -= HandleTurnEnded;
        BattleUIContext.Instance.OnBattleEnded -= HandleBattleEnded;

        _isSubscribed = false;
    }

    /// <summary>
    /// 현재 턴 기준 표시 상태 갱신
    /// </summary>
    private void RefreshByCurrentTurn()
    {
        if (BattleUIContext.Instance == null)
        {
            HideImmediate();
            return;
        }

        BattleUnit currentUnit = BattleUIContext.Instance.CurrentUnit;

        if (currentUnit != null && currentUnit.TeamType == BattleTeamType.Player)
        {
            Show();
            return;
        }

        HideImmediate();
    }

    /// <summary>
    /// 턴 시작 처리
    /// </summary>
    /// <param name="unit">턴 유닛</param>
    private void HandleTurnStarted(BattleUnit unit)
    {
        if (unit != null && unit.TeamType == BattleTeamType.Player)
        {
            Show();
            return;
        }

        Hide();
    }

    /// <summary>
    /// 턴 종료 처리
    /// </summary>
    /// <param name="unit">턴 종료 유닛</param>
    private void HandleTurnEnded(BattleUnit unit)
    {
        Hide();
    }

    /// <summary>
    /// 전투 종료 처리
    /// </summary>
    /// <param name="winner">승리 팀</param>
    private void HandleBattleEnded(BattleTeamType winner)
    {
        Hide();
    }

    public void Hide()
    {
        EnsureInitialized();

        if (_rectTransform == null) return;

        _slideTween?.Kill();

        SetInteractable(false);

        _slideTween = _rectTransform.DOAnchorPosX(_hiddenPosX, _duration).SetEase(_hideEase);
    }

    public void Show()
    {
        EnsureInitialized();

        if (_rectTransform == null) return;

        _slideTween?.Kill();

        gameObject.SetActive(true);

        _slideTween = _rectTransform
            .DOAnchorPosX(_visiblePosX, _duration)
            .SetEase(_showEase)
            .OnComplete(() => SetInteractable(true));
    }


    /// <summary>
    /// 액션 버튼 즉시 숨김
    /// </summary>
    private void HideImmediate()
    {
        EnsureInitialized();

        if (_rectTransform == null)
        {
            return;
        }

        _slideTween?.Kill();

        _rectTransform.anchoredPosition = new Vector2(
            _hiddenPosX,
            _rectTransform.anchoredPosition.y);

        SetInteractable(false);
    }

    /// <summary>
    /// 버튼 입력 가능 여부 설정
    /// </summary>
    /// <param name="isInteractable">입력 가능 여부</param>
    private void SetInteractable(bool isInteractable)
    {
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.interactable = isInteractable;
        _canvasGroup.blocksRaycasts = isInteractable;
    }
}