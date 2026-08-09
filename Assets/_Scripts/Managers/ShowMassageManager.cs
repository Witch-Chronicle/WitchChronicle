using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
public class ShowMessageManager : MonoBehaviour
{
    public static ShowMessageManager Instance { get; private set; }
    [Header("References")]
    [Tooltip("Fade In/Out으로 켜고 끌 메시지 UI의 루트 오브젝트입니다. CanvasGroup이 필요합니다.")]
    [SerializeField]
    private GameObject _messageRoot;
    [SerializeField]
    private CanvasGroup _messageRootCanvasGroup;
    [SerializeField]
    private TMP_Text _message;
    [Header("Timing")]
    [SerializeField]
    private float _duration;
    [Tooltip("MessageRoot가 나타날 때 걸리는 Fade 시간입니다.")]
    [SerializeField, Min(0f)]
    private float _fadeInDuration = 0.15f;
    [Tooltip("MessageRoot가 사라질 때 걸리는 Fade 시간입니다.")]
    [SerializeField, Min(0f)]
    private float _fadeOutDuration = 0.15f;
    [SerializeField]
    private PlayerInteractor _playerInteractor;
    private bool _isShowingMessage;
    // 인벤토리, 스탯, 상점 등의 UI가 열렸을 때
    // 상호작용 메시지 표시를 막기 위한 상태
    private bool _isBlockedByUI;
    private bool _isRootVisible;
    private Tween _fadeTween;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ResolveReferences();
        InitializeRootImmediate();
    }
    private void Start()
    {
        FindPlayerInteractor();
    }
    private void Update()
    {
        // 외부 UI가 열려 있는 동안에는 메시지를 절대 표시하지 않음
        if (_isBlockedByUI)
        {
            SetMessageVisible(false);
            return;
        }
        if (_isShowingMessage)
        {
            return;
        }
        if (_playerInteractor == null)
        {
            FindPlayerInteractor();
            SetMessageVisible(false);
            return;
        }
        if (_playerInteractor.Current == null)
        {
            SetMessageVisible(false);
            return;
        }
        SetMessageVisible(true);
        if (_message != null)
        {
            _message.text = _playerInteractor.Current.Prompt;
        }
    }
    /// <summary>
    /// 인벤토리, 스탯, Pause 등의 전체 화면 UI가 열릴 때 호출합니다.
    /// 상호작용 메시지를 즉시 숨기고 자동 표시를 차단합니다.
    /// </summary>
    public void BlockByUI()
    {
        _isBlockedByUI = true;
        StopAllCoroutines();
        _isShowingMessage = false;
        SetMessageVisible(false);
    }
    /// <summary>
    /// 전체 화면 UI가 모두 닫혔을 때 호출합니다.
    /// 이후 Update에서 현재 상호작용 대상에 따라 다시 표시됩니다.
    /// </summary>
    public void UnblockByUI()
    {
        _isBlockedByUI = false;
    }
    /// <summary>
    /// PlayerInteractor를 런타임에 탐색합니다.
    /// </summary>
    private void FindPlayerInteractor()
    {
        if (_playerInteractor != null)
        {
            return;
        }
        _playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        if (_playerInteractor == null)
        {
            Debug.LogWarning(
                "[ShowMessageManager] PlayerInteractor를 찾을 수 없습니다.",
                this
            );
        }
    }
    /// <summary>
    /// 지정한 시간 동안 시스템 메시지를 표시합니다.
    /// UI에 의해 차단된 상태에서는 표시하지 않습니다.
    /// </summary>
    public void ShowMessage(string message)
    {
        if (_isBlockedByUI)
        {
            return;
        }
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(message, _duration));
    }
    private IEnumerator ShowRoutine(string message, float duration)
    {
        _isShowingMessage = true;
        SetMessageVisible(true);
        if (_message != null)
        {
            _message.text = message;
        }
        yield return new WaitForSeconds(duration);
        _isShowingMessage = false;
    }
    private void ResolveReferences()
    {
        if (_messageRoot == null)
        {
            return;
        }
        if (_messageRootCanvasGroup == null)
        {
            _messageRootCanvasGroup = _messageRoot.GetComponent<CanvasGroup>();
        }
        if (_messageRootCanvasGroup == null)
        {
            Debug.LogError(
                "[ShowMessageManager] MessageRoot에 CanvasGroup이 필요합니다.",
                this
            );
        }
    }
    /// <summary>
    /// 시작 시 MessageRoot를 애니메이션 없이 즉시 숨긴 상태로 초기화합니다.
    /// </summary>
    private void InitializeRootImmediate()
    {
        _isRootVisible = false;
        if (_messageRootCanvasGroup != null)
        {
            _messageRootCanvasGroup.alpha = 0f;
            _messageRootCanvasGroup.interactable = false;
            _messageRootCanvasGroup.blocksRaycasts = false;
        }
        if (_messageRoot != null)
        {
            _messageRoot.SetActive(false);
        }
    }
    /// <summary>
    /// MessageRoot를 DOTween Fade로 표시하거나 숨깁니다.
    /// 이미 같은 상태라면 아무 동작도 하지 않아 Tween이 매 프레임 재시작되지 않습니다.
    /// Message 텍스트 자체는 Fade와 무관하게 즉시 반영됩니다.
    /// </summary>
    private void SetMessageVisible(bool visible)
    {
        if (visible == _isRootVisible)
        {
            return;
        }
        _isRootVisible = visible;
        if (_messageRootCanvasGroup == null)
        {
            // CanvasGroup이 없다면 기존 방식(즉시 On/Off)으로 폴백
            _messageRoot?.SetActive(visible);
            return;
        }
        _fadeTween?.Kill();
        if (visible)
        {
            _messageRoot.SetActive(true);
            _fadeTween = _messageRootCanvasGroup
                .DOFade(1f, _fadeInDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
        else
        {
            _fadeTween = _messageRootCanvasGroup
                .DOFade(0f, _fadeOutDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (_messageRoot != null)
                    {
                        _messageRoot.SetActive(false);
                    }
                });
        }
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        _fadeTween?.Kill();
    }
}