using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투 대상 선택 버튼
/// </summary>
public class BattleTargetButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _targetNameText;

    private BattleUnit _targetUnit;
    private Action<BattleUnit> _onClickTarget;

    /// <summary>
    /// 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    /// <summary>
    /// 대상 버튼 초기화
    /// </summary>
    /// <param name="targetUnit">대상 유닛</param>
    /// <param name="onClickTarget">클릭 콜백</param>
    public void Initialize(BattleUnit targetUnit, Action<BattleUnit> onClickTarget)
    {
        _targetUnit = targetUnit;
        _onClickTarget = onClickTarget;

        if (_targetNameText != null && _targetUnit != null)
        {
            _targetNameText.text = $"{_targetUnit.UnitName} HP {_targetUnit.CurrentHp}/{_targetUnit.MaxHp}";
        }
    }

    /// <summary>
    /// 버튼 클릭 처리
    /// </summary>
    private void HandleClick()
    {
        if (_targetUnit == null)
        {
            return;
        }

        _onClickTarget?.Invoke(_targetUnit);
    }
}