using UnityEngine;
using UnityEngine.InputSystem;

/// 필드 ↔ UI 모드 전환 관리.
/// UI 모드: 커서 표시 + Player 액션맵 비활성 (이동·상호작용·카메라 회전 전부 정지)
/// 필드 모드: 커서 잠금/숨김 + Player 액션맵 활성
/// UI 연동: 메뉴·대화창 열 때 EnterUIMode(), 닫을 때 ExitUIMode() 호출.
/// Look/Zoom이 같은 Player 맵에 있어서 카메라도 같이 멈춤.
public class CursorLocker : MonoBehaviour
{
    // 추가
    public static CursorLocker Instance { get; private set; }

    [SerializeField] private InputActionAsset _inputAsset;   // PlayerInputAction 연결

    private InputActionMap _playerMap;

    public bool IsUIMode { get; private set; }



    //추가
    // 현재 열린 UI 개수
    private int _uiOpenCount = 1;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);

            return;
        }


        Instance = this;

        if (_inputAsset == null)
        {
            Debug.LogError($"{name}: CursorLocker에 Input Asset이 연결되지 않았습니다. 인스펙터에서 PlayerInputAction을 넣어주세요.");
            enabled = false;
            return;
        }
        _playerMap = _inputAsset.FindActionMap("Player", throwIfNotFound: true);
    }

    private void Start() => ExitUIMode();

    private void Update()
    {
        // // ESC로 모드 토글 (UI 붙기 전 테스트용 — 메뉴 UI 생기면 그쪽에서 호출)
        // if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        // {
        //     if (IsUIMode) ExitUIMode();
        //     else EnterUIMode();
        // }
    }

    /// UI 열릴 때 호출: 커서 표시, 캐릭터·카메라 입력 정지
    public void EnterUIMode()
    {
        // 추가 
        _uiOpenCount++;

        Debug.Log($"UI OPEN COUNT : {_uiOpenCount}");

        // 이미 UI 모드라면 추가 처리 X
        if (_uiOpenCount > 1)
        {
            return;
        }


        IsUIMode = true;
        _playerMap.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// UI 닫힐 때 호출: 커서 잠금, 캐릭터·카메라 입력 재개
    public void ExitUIMode()
    {
        // 추가
        if (_uiOpenCount <= 0)
        {
            return;
        }

        _uiOpenCount--;

        Debug.Log($"UI OPEN COUNT : {_uiOpenCount}");

        // 아직 다른 UI가 열려있음
        if (_uiOpenCount > 0)
        {
            return;
        }


        IsUIMode = false;
        _playerMap.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
