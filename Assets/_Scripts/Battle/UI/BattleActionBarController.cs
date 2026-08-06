using UnityEngine;
using DG.Tweening;

/// <summary>
/// Btns(전투 액션 버튼 묶음)의 표시/숨김을 담당.
/// - World Space Canvas 대응: 화면 밖 슬라이드 대신 CanvasGroup.DOFade + RectTransform.DOScale로
///   페이드인+스케일업 / 페이드아웃+스케일다운.
/// - gameObject.SetActive는 사용하지 않음. World Space 하위 요소가 스스로 SetActive(false)로 꺼지면,
///   부모(WorldSpaceCanvas)가 나중에 다시 켜져도 그 자식은 계속 꺼진 채로 남는 Unity 특성 때문
///   (한 라운드 돌고 다시 자기 턴이 왔을 때 UI가 안 뜨는 버그의 원인이었음).
///   대신 CanvasGroup의 alpha/interactable/blocksRaycasts만으로 숨김을 표현.
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

    [Header("Fade + Scale")]
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private Ease _hideEase = Ease.InQuad;
    [SerializeField] private Ease _showEase = Ease.OutQuad;
    [SerializeField] private Vector3 _hiddenScale = new Vector3(0.85f, 0.85f, 0.85f);

    private Vector3 _visibleScale;
    private bool _isInitialized;
    private bool _isSubscribed;
    private Sequence _fadeScaleSequence;
    private BattleActor _ownerActor;

    private void Awake()
    {
        EnsureInitialized();

        if (_ownerActor == null)
        {
            _ownerActor = GetComponentInParent<BattleActor>();
        }
    }

    private void OnEnable()
    {
        TrySubscribeBattleUIContext();
    }

    private void Start()
    {
        TrySubscribeBattleUIContext();

        if (_autoReactToTurnEvents)
        {
            RefreshByCurrentTurn();
        }
    }

    private void OnDisable()
    {
        UnsubscribeBattleUIContext();
        _fadeScaleSequence?.Kill();
    }

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        if (_rectTransform != null)
        {
            _visibleScale = _rectTransform.localScale;
        }
        else
        {
            _visibleScale = Vector3.one;
        }

        _isInitialized = true;
    }

    private void TrySubscribeBattleUIContext()
    {
        if (_autoReactToTurnEvents == false) return;
        if (_isSubscribed) return;
        if (BattleUIContext.Instance == null) return;

        BattleUIContext.Instance.OnTurnStarted += HandleTurnStarted;
        BattleUIContext.Instance.OnTurnEnded += HandleTurnEnded;
        BattleUIContext.Instance.OnBattleEnded += HandleBattleEnded;

        _isSubscribed = true;
    }

    private void UnsubscribeBattleUIContext()
    {
        if (_isSubscribed == false) return;

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

    private void RefreshByCurrentTurn()
    {
        if (BattleUIContext.Instance == null)
        {
            HideImmediate();
            return;
        }

        BattleUnit currentUnit = BattleUIContext.Instance.CurrentUnit;

        if (IsMyTurn(currentUnit))
        {
            Show();
            return;
        }

        HideImmediate();
    }

    private void HandleTurnStarted(BattleUnit unit)
    {
        if (IsMyTurn(unit))
        {
            Show();
            return;
        }

        Hide();
    }

    /// <summary>
    /// 이 Btns가 속한 캐릭터의 턴인지 확인. 단순히 "아군 턴인지"가 아니라
    /// "내 캐릭터의 턴인지"까지 비교해야 함 (안 그러면 아군 턴마다 모든 캐릭터의 Btns가 동시에 표시됨).
    /// </summary>
    private bool IsMyTurn(BattleUnit unit)
    {
        if (unit == null || unit.TeamType != BattleTeamType.Player) return false;
        if (_ownerActor == null || _ownerActor.HasBattleUnit == false) return false;

        return unit == _ownerActor.BattleUnit;
    }

    private void HandleTurnEnded(BattleUnit unit)
    {
        Hide();
    }

    private void HandleBattleEnded(BattleTeamType winner)
    {
        Hide();
    }

    public void Hide()
    {
        EnsureInitialized();

        if (_rectTransform == null || _canvasGroup == null) return;

        _fadeScaleSequence?.Kill();

        SetInteractable(false);

        _fadeScaleSequence = DOTween.Sequence();
        _fadeScaleSequence.Join(_canvasGroup.DOFade(0f, _duration).SetEase(_hideEase));
        _fadeScaleSequence.Join(_rectTransform.DOScale(_hiddenScale, _duration).SetEase(_hideEase));
        BattleUIInputReader.Instance?.SuspendCommandUI();
    }

    public void Show()
    {
        EnsureInitialized();

        if (_rectTransform == null || _canvasGroup == null) return;

        _fadeScaleSequence?.Kill();

        SetInteractable(false); // 애니메이션 끝나기 전 클릭 방지

        _fadeScaleSequence = DOTween.Sequence();
        _fadeScaleSequence.Join(_canvasGroup.DOFade(1f, _duration).SetEase(_showEase));
        _fadeScaleSequence.Join(_rectTransform.DOScale(_visibleScale, _duration).SetEase(_showEase));
        _fadeScaleSequence.OnComplete(() => SetInteractable(true));
        BattleUIInputReader.Instance?.ResumeCommandUI();
    }

    /// <summary>
    /// 애니메이션 없이 즉시 숨김 (초기화 등).
    /// </summary>
    private void HideImmediate()
    {
        EnsureInitialized();

        if (_rectTransform == null || _canvasGroup == null) return;

        _fadeScaleSequence?.Kill();

        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = _hiddenScale;

        SetInteractable(false);
    }

    private void SetInteractable(bool isInteractable)
    {
        if (_canvasGroup == null) return;

        _canvasGroup.interactable = isInteractable;
        _canvasGroup.blocksRaycasts = isInteractable;
    }
}