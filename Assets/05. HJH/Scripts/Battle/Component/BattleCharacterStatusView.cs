using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab_BattleCharacter / Selected_BattleCharacter에 붙는 뷰.
/// - Icon은 아직 데이터 없어서 비워둠
/// - HP/MP는 BattleUnit.OnHpChanged/OnMpChanged를 구독해서 실시간 갱신 (임시 테스트용 이벤트)
/// - OrderTxt는 "이번 라운드 전체 턴 순서에서 몇 번째인지"를 표시
/// </summary>
public class BattleCharacterStatusView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _orderTxt;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TMP_Text _hpTxt;
    [SerializeField] private Slider _mpSlider;
    [SerializeField] private TMP_Text _mpTxt;

    public BattleUnit BoundUnit { get; private set; }

    public void Bind(BattleUnit unit)
    {
        UnsubscribeCurrent();

        BoundUnit = unit;

        if (unit == null) return;

        if (_nameTxt != null) _nameTxt.text = unit.UnitName;

        unit.OnHpChanged += HandleHpChanged;
        unit.OnMpChanged += HandleMpChanged;

        UpdateHp(unit.CurrentHp, unit.MaxHp);
        UpdateMp(unit.CurrentMp, unit.MaxMp);
    }

    public void UpdateHp(int currentHp, int maxHp)
    {
        if (_hpSlider != null) _hpSlider.value = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        if (_hpTxt != null) _hpTxt.text = $"{currentHp}/{maxHp}";
    }

    public void UpdateMp(int currentMp, int maxMp)
    {
        if (_mpSlider != null) _mpSlider.value = maxMp > 0 ? (float)currentMp / maxMp : 0f;
        if (_mpTxt != null) _mpTxt.text = $"{currentMp}/{maxMp}";
    }

    /// <summary>
    /// 이번 라운드 전체 턴 순서 상 몇 번째인지 표시.
    /// isDead가 true면 순번 대신 "-"로 표시 (죽은 유닛은 다음 라운드부터 턴 순서 자체에서 제외되므로).
    /// </summary>
    public void UpdateOrder(int roundOrderNumber, bool isDead = false)
    {
        if (_orderTxt == null) return;

        if (isDead)
        {
            _orderTxt.text = "-";
            return;
        }

        _orderTxt.text = roundOrderNumber > 0 ? roundOrderNumber.ToString() : string.Empty;
    }

    public void Clear()
    {
        UnsubscribeCurrent();

        BoundUnit = null;
        if (_nameTxt != null) _nameTxt.text = string.Empty;
        if (_orderTxt != null) _orderTxt.text = string.Empty;
        if (_hpSlider != null) _hpSlider.value = 0f;
        if (_hpTxt != null) _hpTxt.text = string.Empty;
        if (_mpSlider != null) _mpSlider.value = 0f;
        if (_mpTxt != null) _mpTxt.text = string.Empty;
    }

    private void HandleHpChanged()
    {
        if (BoundUnit != null) UpdateHp(BoundUnit.CurrentHp, BoundUnit.MaxHp);
    }

    private void HandleMpChanged()
    {
        if (BoundUnit != null) UpdateMp(BoundUnit.CurrentMp, BoundUnit.MaxMp);
    }

    private void UnsubscribeCurrent()
    {
        if (BoundUnit == null) return;

        BoundUnit.OnHpChanged -= HandleHpChanged;
        BoundUnit.OnMpChanged -= HandleMpChanged;
    }

    private void OnDestroy()
    {
        UnsubscribeCurrent();
    }
}