using UnityEngine;
using UnityEngine.UI;

public class AtkController : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Button _atkBtn;

    [Header("Target Cycler")]
    [SerializeField] private BattleTargetCycler _targetCycler;

    [Header("Camera")]
    [SerializeField] private BattleCameraDirector _cameraDirector;

    private void Awake()
    {
        if (_atkBtn != null) _atkBtn.onClick.AddListener(HandleAtkClicked);
    }

    private void HandleAtkClicked()
    {
        if (_targetCycler == null) return;

        BattleUnit currentUnit = BattleUIContext.Instance != null
            ? BattleUIContext.Instance.CurrentUnit
            : null;

        if (_cameraDirector == null || currentUnit == null)
        {
            _targetCycler.EnterAttackMode();
            return;
        }

        _cameraDirector.PlayTargetOverview(
            currentUnit,
            () => _targetCycler.EnterAttackMode());
    }
}