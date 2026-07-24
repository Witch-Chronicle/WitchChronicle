using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// BattleUnitPresenter 테스트용 치트. 캐릭터(Presenter가 붙은 오브젝트)에 함께 부착.
/// 1~3: 공격 / 4: 스킬 / 5: 피격 / 6: 패링 / 7: 사망 / 8: 승리 / 9: 디졸브
/// </summary>
[RequireComponent(typeof(BattleUnitPresenter))]
public class BattlePresenterDebug : MonoBehaviour
{
    private BattleUnitPresenter _presenter;
    private DeathDissolve _dissolve;

    private void Awake()
    {
        _presenter = GetComponent<BattleUnitPresenter>();
        _dissolve = GetComponent<DeathDissolve>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) _presenter.PlayAttack(0);
        if (kb.digit2Key.wasPressedThisFrame) _presenter.PlayAttack(1);
        if (kb.digit3Key.wasPressedThisFrame) _presenter.PlayAttack(2);
        if (kb.digit4Key.wasPressedThisFrame) _presenter.PlaySkill();
        if (kb.digit5Key.wasPressedThisFrame) _presenter.PlayHit();
        if (kb.digit6Key.wasPressedThisFrame) _presenter.PlayParry();
        if (kb.digit7Key.wasPressedThisFrame) _presenter.PlayDeath();
        if (kb.digit8Key.wasPressedThisFrame) _presenter.PlayVictory();
        if (kb.digit9Key.wasPressedThisFrame && _dissolve != null) _dissolve.Play();
    }
}
