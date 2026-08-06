using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishingUIManager : MonoBehaviour
{
    public static FishingUIManager Instance { get; private set; }

    [Header("루트 패널")]
    [SerializeField] private GameObject fishingPanel;
    [SerializeField] private GameObject resultPopup;

    [Header("낚시 씬 이미지 (레거시, 3D 씬 사용 시 미연결)")]
    [SerializeField] private Image fishingSceneImage;
    [SerializeField] private Sprite sceneIdle;
    [SerializeField] private Sprite sceneBite;
    [SerializeField] private Sprite sceneReeling;

    [Header("상단 UI")]
    [SerializeField] private GameObject fishingTimer;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject statusBubble;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject tensionGaugeGroup;

    [Header("진행 게이지 (Bite/Reeling만)")]
    [SerializeField] private GameObject progressGauge;

    [Header("메인 액션 버튼")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private Image actionButtonImage;

    [Header("액션 버튼 상태별 투명도")]
    [Tooltip("활성 상태 알파 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float _activeAlpha = 1f;
    [Tooltip("비활성 상태 알파 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float _inactiveAlpha = 0.4f;

    [Header("결과 팝업")]
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultText;

    [Header("텐션 미니게임 컨트롤러")]
    [SerializeField] private FishingReelController reelController;

    private bool _isPanelOpen = false;
    private float _sessionTime = 0f;
    private bool _timerRunning = false;

    public bool IsPanelOpen => _isPanelOpen;

    private void Awake()
    {
        Debug.Log("[FishingUIManager] Awake 실행됨");
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (fishingPanel != null) fishingPanel.SetActive(false);
        if (resultPopup != null) resultPopup.SetActive(false);
    }

    private void OnEnable()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    private void Start()
    {
        Debug.Log("[FishingUIManager] Start 실행됨");
        if (FishingManager.Instance != null)
        {
            FishingManager.Instance.OnStateChanged += HandleStateChanged;
            FishingManager.Instance.OnFishCaught += HandleFishCaught;
            FishingManager.Instance.OnFishEscaped += HandleFishEscaped;
            FishingManager.Instance.OnFishingSessionStarted += HandleSessionStarted;
            FishingManager.Instance.OnFishingSessionEnded += HandleSessionEnded;
            Debug.Log("[FishingUIManager] 이벤트 등록 성공");
        }
        else
        {
            Debug.LogError("[FishingUIManager] FishingManager.Instance가 null! 씬에 FishingManager 오브젝트 있는지 확인!");
        }
    }

    private void OnDisable()
    {
        if (FishingManager.Instance != null)
        {
            FishingManager.Instance.OnStateChanged -= HandleStateChanged;
            FishingManager.Instance.OnFishCaught -= HandleFishCaught;
            FishingManager.Instance.OnFishEscaped -= HandleFishEscaped;
            FishingManager.Instance.OnFishingSessionStarted -= HandleSessionStarted;
            FishingManager.Instance.OnFishingSessionEnded -= HandleSessionEnded;
        }
        if (actionButton != null)
            actionButton.onClick.RemoveListener(OnActionButtonClicked);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (_timerRunning)
        {
            _sessionTime += Time.deltaTime;
            if (timerText != null)
            {
                int m = Mathf.FloorToInt(_sessionTime / 60f);
                int s = Mathf.FloorToInt(_sessionTime % 60f);
                timerText.text = $"{m:00}:{s:00}";
            }
        }
    }

    private void HandleSessionStarted()
    {
        Debug.Log("[FishingUIManager] HandleSessionStarted 호출됨! fishingPanel is null? " + (fishingPanel == null));
        OpenPanel();
    }

    private void HandleSessionEnded() => ClosePanel();

    public void OpenPanel()
    {
        Debug.Log("[FishingUIManager] OpenPanel 호출됨");
        if (fishingPanel == null)
        {
            Debug.LogError("[FishingUIManager] fishingPanel이 null! 인스펙터 연결 확인");
            return;
        }
        fishingPanel.SetActive(true);
        _isPanelOpen = true;

        _sessionTime = 0f;
        _timerRunning = true;

        HandleStateChanged(FishingState.Idle);
    }

    public void ClosePanel()
    {
        if (fishingPanel == null) return;
        fishingPanel.SetActive(false);
        _isPanelOpen = false;
        _timerRunning = false;

        reelController?.StopMiniGame();
    }

    private void HandleStateChanged(FishingState state)
    {
        switch (state)
        {
            case FishingState.Idle:
                SetScene(sceneIdle);
                ShowStatus("낚시 준비");
                HideTension();
                HideProgressGauge();
                SetActionButton("\n줄 풀기", true);
                if (resultPopup != null) resultPopup.SetActive(false);
                break;

            case FishingState.Casting:
                SetScene(sceneIdle);
                ShowStatus("낚싯대를 던지는 중...");
                HideTension();
                HideProgressGauge();
                SetActionButton("\n줄 감기", false);
                break;

            case FishingState.Waiting:
                SetScene(sceneIdle);
                ShowStatus("기다리고 있어요...");
                HideTension();
                HideProgressGauge();
                SetActionButton("\n줄 감기", false);
                break;

            case FishingState.Bite:
                SetScene(sceneBite);
                HideStatus();
                ShowTension();
                ShowProgressGauge();
                SetActionButton("\n줄 감기!", true);
                break;

            case FishingState.Reeling:
                SetScene(sceneReeling);
                HideStatus();
                ShowTension();
                ShowProgressGauge();
                SetActionButton("\n홀드!", true);
                reelController?.StartMiniGame(
                    FishingManager.Instance?.HookedFish,
                    FishingManager.Instance?.CurrentRod
                );
                break;

            case FishingState.Result:
                HideTension();
                HideProgressGauge();
                reelController?.StopMiniGame();
                break;
        }
    }

    private void HandleFishCaught(FishItemData fish)
    {
        string name = fish != null ? fish.itemName : "물고기";
        ShowResult("성공!", $"{name} 획득!");
    }

    private void HandleFishEscaped(FishingReelController.FailReason reason)
    {
        string message;
        switch (reason)
        {
            case FishingReelController.FailReason.LineBreak:
                message = "줄이 끊어졌다...";
                break;
            case FishingReelController.FailReason.Escape:
                message = "물고기가 도망갔다...";
                break;
            case FishingReelController.FailReason.Timeout:
                message = "너무 오래 걸렸다... 물고기가 지쳐 도망갔다.";
                break;
            default:
                message = "놓쳤어요...";
                break;
        }
        ShowResult(" 실패", message);
    }

    private void OnActionButtonClicked()
    {
        if (FishingManager.Instance == null) return;
        switch (FishingManager.Instance.State)
        {
            case FishingState.Idle:
                FishingManager.Instance.StartFishing();
                break;
            case FishingState.Bite:
                FishingManager.Instance.OnCatchButtonPressed();
                break;
        }
    }

    public void OnResultConfirmClicked()
    {
        if (resultPopup != null) resultPopup.SetActive(false);
        FishingManager.Instance?.ReturnToIdle();
    }

    private void SetScene(Sprite s)
    {
        if (fishingSceneImage != null && s != null) fishingSceneImage.sprite = s;
    }

    private void ShowStatus(string msg)
    {
        if (statusBubble != null) statusBubble.SetActive(true);
        if (statusText != null) statusText.text = msg;
    }
    private void HideStatus()
    {
        if (statusBubble != null) statusBubble.SetActive(false);
    }

    private void ShowTension() { if (tensionGaugeGroup != null) tensionGaugeGroup.SetActive(true); }
    private void HideTension() { if (tensionGaugeGroup != null) tensionGaugeGroup.SetActive(false); }

    private void ShowProgressGauge() { if (progressGauge != null) progressGauge.SetActive(true); }
    private void HideProgressGauge() { if (progressGauge != null) progressGauge.SetActive(false); }

    private void SetActionButton(string label, bool interactable)
    {
        if (actionButtonText != null) actionButtonText.text = label;

        if (actionButtonImage != null)
        {
            // RGB는 흰색 유지 (원본 이미지 색 그대로), 알파값만 조절
            float alpha = interactable ? _activeAlpha : _inactiveAlpha;
            actionButtonImage.color = new Color(1f, 1f, 1f, alpha);
        }

        if (actionButton != null) actionButton.interactable = interactable;
    }

    private void ShowResult(string title, string body)
    {
        if (resultPopup == null) return;
        resultPopup.SetActive(true);
        if (resultTitle != null) resultTitle.text = title;
        if (resultText != null) resultText.text = body;
    }
}