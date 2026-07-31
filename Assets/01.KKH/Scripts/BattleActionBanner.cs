using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 전투 행동 이름 배너 연출
/// </summary>
public class BattleActionBanner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _panelRoot;
    [SerializeField] private TMP_Text _actorNameText;
    [SerializeField] private TMP_Text _actionNameText;

    [Header("Animation")]
    [SerializeField] private float _showDuration = 0.18f;
    [SerializeField] private float _hideDuration = 0.12f;
    [SerializeField] private float _hiddenOffsetX = -80f;
    [SerializeField] private Ease _showEase = Ease.OutCubic;
    [SerializeField] private Ease _hideEase = Ease.InCubic;

    private float _visiblePosX;
    private Tween _fadeTween;
    private Tween _moveTween;
    private bool _isInitialized;

    /// <summary>
    /// 배너 초기화
    /// </summary>
    private void Awake()
    {
        EnsureInitialized();
        HideImmediate();
    }

    /// <summary>
    /// Tween 정리
    /// </summary>
    private void OnDisable()
    {
        KillTweens();
    }

    /// <summary>
    /// 행동 배너 표시
    /// </summary>
    /// <param name="actionRequest">표시 행동 요청</param>
    public void Show(BattleActionRequest actionRequest)
    {
        if (actionRequest == null)
        {
            HideImmediate();
            return;
        }

        EnsureInitialized();
        KillTweens();

        if (_actorNameText != null)
        {
            _actorNameText.text = actionRequest.Actor != null
                ? actionRequest.Actor.UnitName
                : string.Empty;
        }

        if (_actionNameText != null)
        {
            _actionNameText.text = GetActionName(actionRequest);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _fadeTween = _canvasGroup
                .DOFade(1f, _showDuration)
                .SetEase(_showEase);
        }

        if (_panelRoot != null)
        {
            Vector2 position = _panelRoot.anchoredPosition;
            position.x = _visiblePosX + _hiddenOffsetX;
            _panelRoot.anchoredPosition = position;

            _moveTween = _panelRoot
                .DOAnchorPosX(_visiblePosX, _showDuration)
                .SetEase(_showEase);
        }
    }

    /// <summary>
    /// 행동 배너 숨김
    /// </summary>
    /// <param name="onComplete">숨김 완료 콜백</param>
    public void Hide(System.Action onComplete = null)
    {
        EnsureInitialized();
        KillTweens();

        if (_panelRoot != null)
        {
            _moveTween = _panelRoot
                .DOAnchorPosX(
                    _visiblePosX + _hiddenOffsetX,
                    _hideDuration)
                .SetEase(_hideEase);
        }

        if (_canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        _fadeTween = _canvasGroup
            .DOFade(0f, _hideDuration)
            .SetEase(_hideEase)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 행동 배너 즉시 숨김
    /// </summary>
    public void HideImmediate()
    {
        EnsureInitialized();
        KillTweens();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_panelRoot != null)
        {
            Vector2 position = _panelRoot.anchoredPosition;
            position.x = _visiblePosX + _hiddenOffsetX;
            _panelRoot.anchoredPosition = position;
        }
    }

    /// <summary>
    /// 행동 이름 반환
    /// </summary>
    /// <param name="actionRequest">행동 요청</param>
    /// <returns>표시 행동 이름</returns>
    private string GetActionName(BattleActionRequest actionRequest)
    {
        switch (actionRequest.CommandType)
        {
            case CommandType.Attack:
                return "기본 공격";

            case CommandType.Skill:
                return actionRequest.SkillData != null
                    ? actionRequest.SkillData.SkillName
                    : "스킬";

            case CommandType.Defense:
                return "방어";

            case CommandType.Item:
                return "아이템";

            case CommandType.Escape:
                return "도주";

            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// 참조 및 위치 초기화
    /// </summary>
    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_panelRoot == null)
        {
            _panelRoot = transform as RectTransform;
        }

        if (_panelRoot != null)
        {
            _visiblePosX = _panelRoot.anchoredPosition.x;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// 배너 Tween 중단
    /// </summary>
    private void KillTweens()
    {
        _fadeTween?.Kill();
        _moveTween?.Kill();

        _fadeTween = null;
        _moveTween = null;
    }
}