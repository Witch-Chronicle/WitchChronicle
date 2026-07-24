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
        }
    }

    public void ToggleEnhanceUI()
    {
        _isEnhanceOpen = !_isEnhanceOpen;

        if (_enhancePanelAnimator != null)
        {
            if (_isEnhanceOpen)
            {
                _enhancePanelAnimator.Open();
                // 추가
                CursorLocker.Instance.EnterUIMode();
            }
            else
            {
                _enhancePanelAnimator.Close();
                // 추가
                CursorLocker.Instance.ExitUIMode();
            }
        }
        else
        {
            Debug.LogWarning("[EnhanceNPC] enhancePanelAnimator가 연결되지 않았습니다.");
        }
    }
}