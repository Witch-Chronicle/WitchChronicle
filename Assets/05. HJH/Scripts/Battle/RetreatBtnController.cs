using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RetreatBtn 클릭 처리. 캐릭터 프리팹(Btns) 안에 있는 컴포넌트.
/// RetreatConfirmController(팝업 실제 소유자)는 BattleHUDPanel 하위의 씬 오브젝트라
/// 프리팹 인스펙터로 직접 연결할 수 없어서 런타임에 자동으로 찾음.
/// 클릭 시: 이 캐릭터의 WorldCanvas를 잠깐 숨기고(alpha 0), RetreatConfirmController.Open() 호출.
/// </summary>
public class RetreatBtnController : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Button _retreatBtn;

    [Header("Retreat Confirm (씬 오브젝트라 인스펙터 연결 대신 런타임 자동 탐색)")]
    [SerializeField] private RetreatConfirmController _retreatConfirmController;

    private void Awake()
    {
        if (_retreatBtn != null) _retreatBtn.onClick.AddListener(HandleRetreatClicked);
    }

    private void OnEnable()
    {
        EnsureRetreatConfirmController();
    }

    private void EnsureRetreatConfirmController()
    {
        if (_retreatConfirmController != null) return;

        _retreatConfirmController = FindFirstObjectByType<RetreatConfirmController>(FindObjectsInactive.Include);
    }

    private void HandleRetreatClicked()
    {
        EnsureRetreatConfirmController();

        if (_retreatConfirmController == null)
        {
            Debug.LogWarning("[RetreatBtnController] RetreatConfirmController를 찾지 못했습니다.");
            return;
        }

        if (BattleCharacterUIManager.Instance != null)
        {
            BattleCharacterUIManager.Instance.HideCurrentUI();
        }

        _retreatConfirmController.Open();
    }
}