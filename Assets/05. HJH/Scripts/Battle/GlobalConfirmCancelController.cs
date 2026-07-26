using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ConfirmCancelCanvas에 부착. 공격/스킬 확인 버튼(ConfirmBtn/CancelBtn)의 실제 소유자.
/// - 씬에 하나뿐인 공용 오브젝트라서, 캐릭터 프리팹(BattleTargetCycler)이 인스펙터로 직접 참조할 수 없음.
///   대신 BattleTargetCycler가 Show(onConfirm, onCancel)을 호출해서 콜백만 넘기고,
///   실제 버튼 클릭 이벤트는 이 컨트롤러가 받아서 그 콜백을 실행해줌.
/// - 카메라가 타겟에 바짝 붙으면 World Space UI가 화면 밖으로 밀려날 수 있어서,
///   ConfirmCancelGroup은 Screen Space-Overlay로 따로 빼서 항상 화면에 고정.
/// - 씬 시작 시점엔 비활성 상태 유지, Show() 호출 시에만 활성화.
/// </summary>
public class GlobalConfirmCancelController : MonoBehaviour
{
    public static GlobalConfirmCancelController Instance { get; private set; }

    [SerializeField] private GameObject _panelRoot; // ConfirmCancelGroup
    [SerializeField] private Button _confirmBtn;
    [SerializeField] private Button _cancelBtn;

    private Action _onConfirm;
    private Action _onCancel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_confirmBtn != null) _confirmBtn.onClick.AddListener(HandleConfirmClicked);
        if (_cancelBtn != null) _cancelBtn.onClick.AddListener(HandleCancelClicked);

        HidePanelImmediate();
    }

    /// <summary>
    /// 공격/스킬 대상 선택 진입 시 호출. 패널을 켜고, Confirm/Cancel 클릭 시 실행할 콜백을 등록.
    /// </summary>
    public void Show(Action onConfirm, Action onCancel)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (_panelRoot != null)
        {
            _panelRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 확정/취소 완료 후 호출. 패널을 끄고 콜백 참조도 정리.
    /// </summary>
    public void Hide()
    {
        HidePanelImmediate();

        _onConfirm = null;
        _onCancel = null;
    }

    private void HidePanelImmediate()
    {
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(false);
        }
    }

    private void HandleConfirmClicked()
    {
        _onConfirm?.Invoke();
    }

    private void HandleCancelClicked()
    {
        _onCancel?.Invoke();
    }
}