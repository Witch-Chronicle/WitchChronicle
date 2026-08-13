using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// - EnhanceNPC 오브젝트에 붙여서 사용
/// - F2 키를 누르면 EnhancePanel이 토글(열림/닫힘) 됨
/// </summary>
public class EnhanceNPC : MonoBehaviour
{
    public static EnhanceNPC Instance { get; private set; }

    [Header("Enhance UI")]
    [SerializeField] private UIPanelAnimator _enhancePanelAnimator; // 하이어라키의 EnhancePanel 오브젝트 연결 (UIPanelAnimator 컴포넌트)

    private bool _isEnhanceOpen;

    public bool IsOpen => _isEnhanceOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 시작할 때는 닫힌 상태로 초기화 (애니메이션 없이 즉시)
        _isEnhanceOpen = false;

        if (_enhancePanelAnimator != null)
        {
            _enhancePanelAnimator.SetClosedImmediate();
            _enhancePanelAnimator.OnClosed += HandleEnhancePanelClosed;
        }
    }

    public void ToggleEnhanceUI()
    {
        _isEnhanceOpen = !_isEnhanceOpen;

        if (_enhancePanelAnimator != null)
        {
            if (_isEnhanceOpen)
            {
                // 대화 도중 강화 UI가 열리는 경우, 대화창 패널만 끔 (CursorLocker 상태는 안 건드림)
                if (DialogueUI.Instance != null)
                {
                    DialogueUI.Instance.HidePanelOnly();
                }

                // 패널을 화면에 표시하기 전에 배경 Blur부터 요청 (캡처 시 패널 자체가 안 찍히도록)
                UIBackgroundBlurManager.Instance?.Show();

                _enhancePanelAnimator.Open();
                QuestListUI.Instance.Close();
                MainHUDUIController.Instance.Close();
                CursorLocker.Instance.EnterUIMode();
            }
            else
            {
                _enhancePanelAnimator.Close();
                QuestListUI.Instance.Open();
                MainHUDUIController.Instance.Open();
                CursorLocker.Instance.ExitUIMode();

                // Blur는 여기서 바로 안 끔 - Close 애니메이션이 끝난 뒤 HandleEnhancePanelClosed()에서 해제
            }
        }
        else
        {
            Debug.LogWarning("[EnhanceNPC] enhancePanelAnimator가 연결되지 않았습니다.");
        }
    }

    private void HandleEnhancePanelClosed()
    {
        UIBackgroundBlurManager.Instance?.Hide();
    }

    private void OnDestroy()
    {
        if (_enhancePanelAnimator != null)
        {
            _enhancePanelAnimator.OnClosed -= HandleEnhancePanelClosed;
        }
    }
}