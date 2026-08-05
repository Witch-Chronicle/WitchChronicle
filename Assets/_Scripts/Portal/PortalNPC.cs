using UnityEngine;

/// <summary>
/// - PortalNPC 오브젝트에 붙여서 사용
/// - 상호작용 시 PortalPanel이 토글(열림/닫힘) 됨
/// </summary>
public class PortalNPC : MonoBehaviour, ITFInteractable
{
    public static PortalNPC Instance { get; private set; }

    public string Prompt => "[F] 던전 들어가기";

    [Header("Portal UI")]
    [SerializeField] private UIPanelAnimator _portalPanelAnimator; // 하이어라키의 PortalPanel 오브젝트 연결 (UIPanelAnimator 컴포넌트)

    private bool _isPortalOpen;

    public bool IsOpen => _isPortalOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 시작할 때는 닫힌 상태로 초기화 (애니메이션 없이 즉시)
        _isPortalOpen = false;

        if (_portalPanelAnimator != null)
        {
            _portalPanelAnimator.SetClosedImmediate();
            _portalPanelAnimator.OnClosed += HandlePortalPanelClosed;
        }
    }

    public void TogglePortal()
    {
        _isPortalOpen = !_isPortalOpen;

        if (_portalPanelAnimator != null)
        {
            if (_isPortalOpen)
            {
                // 대화 도중 포탈 UI가 열리는 경우, 대화창 패널만 끔
                // CursorLocker 상태는 건드리지 않음
                if (DialogueUI.Instance != null)
                {
                    DialogueUI.Instance.HidePanelOnly();
                }

                // 패널을 화면에 표시하기 전에 배경 Blur부터 요청
                // 캡처 시 PortalPanel 자체가 찍히지 않도록 함
                UIBackgroundBlurManager.Instance?.Show();

                _portalPanelAnimator.Open();
                QuestListUI.Instance.Close();
                CursorLocker.Instance.EnterUIMode();
            }
            else
            {
                _portalPanelAnimator.Close();
                QuestListUI.Instance.Open();
                CursorLocker.Instance.ExitUIMode();

                // Blur는 여기서 바로 끄지 않음
                // Close 애니메이션이 끝난 뒤 HandlePortalPanelClosed()에서 해제
            }
        }
        else
        {
            Debug.LogWarning("[PortalNPC] portalPanelAnimator가 연결되지 않았습니다.");
        }
    }

    private void HandlePortalPanelClosed()
    {
        UIBackgroundBlurManager.Instance?.Hide();
    }

    public void Interact(GameObject interactor)
    {
        TogglePortal();
    }

    private void OnDestroy()
    {
        if (_portalPanelAnimator != null)
        {
            _portalPanelAnimator.OnClosed -= HandlePortalPanelClosed;
        }
    }
}