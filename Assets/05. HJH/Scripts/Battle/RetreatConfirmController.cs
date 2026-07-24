using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RetreatConfirm 팝업 열고닫기 전담. RetreatBtn 클릭 시 열리고, ConfirmBtn/CancelBtn 클릭 시 닫힘.
/// - ConfirmBtn 클릭 시 TransitionPanel을 FadeIn(화면 다시 덮음)해서 후퇴 연출 시작.
/// * 실제 후퇴 처리 로직은 미정 - Confirm 클릭 시 처리 지점만 마련해둠.
/// </summary>
public class RetreatConfirmController : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Button _retreatBtn;

    [Header("Confirm Panel")]
    [SerializeField] private GameObject _retreatConfirmPanel;
    [SerializeField] private Button _confirmBtn;
    [SerializeField] private Button _cancelBtn;

    [Header("Transition (후퇴 확정 시 화면 덮기)")]
    [SerializeField] private TransitionController _transitionController;

    private void Awake()
    {
        if (_retreatBtn != null) _retreatBtn.onClick.AddListener(Open);
        if (_confirmBtn != null) _confirmBtn.onClick.AddListener(HandleConfirm);
        if (_cancelBtn != null) _cancelBtn.onClick.AddListener(Close);

        if (_retreatConfirmPanel != null)
        {
            _retreatConfirmPanel.SetActive(false);
        }
    }

    private void Open()
    {
        if (_retreatConfirmPanel != null)
        {
            _retreatConfirmPanel.SetActive(true);
        }
    }

    private void Close()
    {
        if (_retreatConfirmPanel != null)
        {
            _retreatConfirmPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 후퇴 확정 처리. TransitionPanel을 FadeIn해서 화면을 덮음.
    /// 실제 후퇴 로직(전투 종료 처리, 씬 전환 등)은 추후 구현 예정.
    /// </summary>
    private void HandleConfirm()
    {
        Debug.Log("[RetreatConfirmController] 후퇴 확정 (실제 처리 로직 미구현)");

        Close();
        BattleUIContext.Instance.ForceEndBattle(BattleTeamType.Enemy);
        //     if (_transitionController != null)
        //     {
        //         _transitionController.FadeIn();
        //     }
        // }
    }
}