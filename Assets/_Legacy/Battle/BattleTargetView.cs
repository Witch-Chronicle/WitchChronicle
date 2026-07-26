using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab_BattleTarget에 붙는 뷰. HP/MP는 BattleUnit.OnHpChanged/OnMpChanged 구독으로 실시간 갱신.
/// 클릭하면 콜백을 호출해서 "이 유닛을 대상으로 선택했다"를 알림.
/// </summary>
public class BattleTargetView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _orderTxt;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TMP_Text _hpTxt;
    [SerializeField] private Slider _mpSlider;
    [SerializeField] private TMP_Text _mpTxt;
    [SerializeField] private Button _button;

    private BattleUnit _unit;
    private Action<BattleUnit> _onClick;

    private void Awake()
    {
        if (_button != null) _button.onClick.AddListener(HandleClick);
    }

    public void Bind(BattleUnit unit, int roundOrderNumber, Action<BattleUnit> onClick)
    {
        UnsubscribeCurrent();

        _unit = unit;
        _onClick = onClick;

        if (unit == null) return;

        if (_nameTxt != null) _nameTxt.text = unit.UnitName;
        if (_orderTxt != null) _orderTxt.text = roundOrderNumber > 0 ? roundOrderNumber.ToString() : string.Empty;

        unit.OnHpChanged += HandleHpChanged;
        unit.OnMpChanged += HandleMpChanged;

        UpdateHp(unit.CurrentHp, unit.MaxHp);
        UpdateMp(unit.CurrentMp, unit.MaxMp);
    }

    private void UpdateHp(int currentHp, int maxHp)
    {
        if (_hpSlider != null) _hpSlider.value = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        if (_hpTxt != null) _hpTxt.text = $"{currentHp}/{maxHp}";
    }

    private void UpdateMp(int currentMp, int maxMp)
    {
        if (_mpSlider != null) _mpSlider.value = maxMp > 0 ? (float)currentMp / maxMp : 0f;
        if (_mpTxt != null) _mpTxt.text = $"{currentMp}/{maxMp}";
    }

    private void HandleHpChanged()
    {
        if (_unit != null) UpdateHp(_unit.CurrentHp, _unit.MaxHp);
    }

    private void HandleMpChanged()
    {
        if (_unit != null) UpdateMp(_unit.CurrentMp, _unit.MaxMp);
    }

    private void HandleClick()
    {
        _onClick?.Invoke(_unit);
    }

    private void UnsubscribeCurrent()
    {
        if (_unit == null) return;

        _unit.OnHpChanged -= HandleHpChanged;
        _unit.OnMpChanged -= HandleMpChanged;
    }

    private void OnDestroy()
    {
        UnsubscribeCurrent();
    }
}