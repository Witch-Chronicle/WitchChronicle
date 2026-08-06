using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BattleHUDPanel 하위 RetreatConfirm에 부착. 후퇴 확인 팝업의 실제 소유자.
/// 씬에 하나뿐인 오브젝트라 캐릭터 프리팹(RetreatBtnController)이 직접 참조하지 못하고,
/// RetreatBtnController가 런타임에 이 컴포넌트를 찾아서 Open()만 호출해줌.
/// - Open: 패널을 켬 + 배경 Blur 요청
/// - Cancel: 패널 닫고 + 요청했던 캐릭터의 WorldCanvas 복귀 + Blur 해제
/// - Confirm: 패널 닫고 + 전투 강제 종료(후퇴) + Blur 해제. 전투가 끝나므로 WorldCanvas를 별도로
///   복귀시키지 않음 (BattleCharacterUIManager가 OnBattleEnded로 어차피 전부 정리함).
/// </summary>
public class RetreatConfirmController : MonoBehaviour
{
    [Header("Retreat Canvas (전체를 감싸는 부모)")]
    [SerializeField] private GameObject _retreatCanvas;

    [Header("Confirm Panel")]
    [SerializeField] private GameObject _retreatConfirmPanel;
    [SerializeField] private Button _confirmBtn;
    [SerializeField] private Button _cancelBtn;

    private void Awake()
    {
        if (_confirmBtn != null) _confirmBtn.onClick.AddListener(HandleConfirm);
        if (_cancelBtn != null) _cancelBtn.onClick.AddListener(HandleCancel);

        if (_retreatCanvas != null)
        {
            _retreatCanvas.SetActive(false);
        }
    }

    /// <summary>
    /// RetreatBtnController에서 호출. RetreatCanvas 전체를 켜고(그 안의 ConfirmPanel도 같이 보임)
    /// 배경 Blur를 요청.
    /// </summary>
    public void Open()
    {
        if (_retreatCanvas != null)
        {
            _retreatCanvas.SetActive(true);
        }

        UIBackgroundBlurManager.Instance?.Show();
    }

    private void HandleCancel()
    {
        ClosePanel();

        if (BattleCharacterUIManager.Instance != null)
        {
            BattleCharacterUIManager.Instance.ShowCurrentUI();
        }
    }

    private void HandleConfirm()
    {
        ClosePanel();

        Debug.Log("[RetreatConfirmController] 후퇴 확정");

        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.ForceEndBattle(BattleTeamType.Enemy);
        }
    }

    private void ClosePanel()
    {
        if (_retreatCanvas != null)
        {
            _retreatCanvas.SetActive(false);
        }

        UIBackgroundBlurManager.Instance?.Hide();
    }
}