using UnityEngine;

/// <summary>
/// 테스트용 포탈 NPC.
/// - Portal 오브젝트에 붙여서 사용
/// - F4 키를 누르면 PortalPanel이 토글(열림/닫힘) 됨
/// </summary>
public class PortalNPC : MonoBehaviour, ITFInteractable
{
    public static PortalNPC Instance { get; private set; }

    public string Prompt => "[F] 던전 들어가기";

    [Header("Portal UI")]
    [SerializeField] private UIPanelAnimator _portalPanelAnimator; // 하이어라키의 PortalPanel 오브젝트 연결 (UIPanelAnimator 컴포넌트)

    private bool _isPortalOpen;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 시작할 때는 포탈 닫힌 상태로 초기화 (애니메이션 없이 즉시)
        _isPortalOpen = false;
        if (_portalPanelAnimator != null)
        {
            _portalPanelAnimator.SetClosedImmediate();
        }
    }


    public void TogglePortal()
    {
        _isPortalOpen = !_isPortalOpen;

        if (_portalPanelAnimator != null)
        {
            if (_isPortalOpen)
            {
                _portalPanelAnimator.Open();

                // 추가
                CursorLocker.Instance.EnterUIMode();
            }
            else
            {
                _portalPanelAnimator.Close();
                // 추가
                CursorLocker.Instance.ExitUIMode();
            }
        }
        else
        {
            Debug.LogWarning("[PortalNPC] portalPanelAnimator가 연결되지 않았습니다.");
        }
    }

    public void Interact(GameObject interactor)
    {
        TogglePortal();
    }
}