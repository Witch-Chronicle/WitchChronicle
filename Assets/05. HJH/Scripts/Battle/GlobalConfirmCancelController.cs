using UnityEngine;

/// <summary>
/// ConfirmCancelCanvas에 부착. 공격/스킬 확인 UI(패널)의 표시 여부만 관리.
/// - 씬에 하나뿐인 공용 오브젝트라서, 캐릭터 프리팹(BattleTargetCycler)이 인스펙터로 직접 참조할 수 없음.
/// - 실제 확정/취소는 BattleUIInputReader가 키보드 입력(Enter/Esc)으로 BattleTargetCycler.Confirm()/Cancel()을
///   직접 호출하므로, 이 컨트롤러는 콜백을 들고 있지 않고 패널의 표시 여부만 담당.
/// - 카메라가 타겟에 바짝 붙으면 World Space UI가 화면 밖으로 밀려날 수 있어서,
///   ConfirmCancelGroup은 Screen Space-Overlay로 따로 빼서 항상 화면에 고정.
/// - 씬 시작 시점엔 비활성 상태 유지, Show() 호출 시에만 활성화.
/// </summary>
public class GlobalConfirmCancelController : MonoBehaviour
{
    public static GlobalConfirmCancelController Instance { get; private set; }

    [SerializeField] private GameObject _panelRoot; // ConfirmCancelGroup

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        HidePanelImmediate();
    }

    /// <summary>
    /// 공격/스킬 대상 선택 진입 시 호출. 패널만 켬.
    /// </summary>
    public void Show()
    {
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 확정/취소 완료 후 호출. 패널을 끔.
    /// </summary>
    public void Hide()
    {
        HidePanelImmediate();
    }

    private void HidePanelImmediate()
    {
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(false);
        }
    }
}