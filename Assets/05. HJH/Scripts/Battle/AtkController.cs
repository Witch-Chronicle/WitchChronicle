using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AtkBtn 클릭 처리. BattleTargetCycler는 씬에 하나뿐인 오브젝트라
/// 캐릭터 프리팹 인스펙터로 직접 연결할 수 없어서 런타임에 자동으로 찾음.
/// 카메라 전환(TargetOverview)은 BattleTargetCycler.EnterAttackMode() 내부에서 처리.
/// </summary>
public class AtkController : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Button _atkBtn;

    [Header("Target Cycler (씬 오브젝트라 인스펙터 연결 대신 런타임 자동 탐색)")]
    [SerializeField] private BattleTargetCycler _targetCycler;

    private void Awake()
    {
        if (_atkBtn != null) _atkBtn.onClick.AddListener(HandleAtkClicked);
    }

    private void OnEnable()
    {
        EnsureTargetCycler();
    }

    private void EnsureTargetCycler()
    {
        if (_targetCycler != null) return;

        _targetCycler = BattleTargetCycler.Instance;

        if (_targetCycler == null)
        {
            _targetCycler = FindFirstObjectByType<BattleTargetCycler>(FindObjectsInactive.Include);
        }
    }

    private void HandleAtkClicked()
    {
        EnsureTargetCycler();

        if (_targetCycler == null)
        {
            Debug.LogWarning("[AtkController] BattleTargetCycler를 찾지 못했습니다.");
            return;
        }

        _targetCycler.EnterAttackMode();
    }
}