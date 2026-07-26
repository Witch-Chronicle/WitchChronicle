using UnityEngine;
using TMPro;

/// <summary>
/// EnhancePanel/Enhance/Preview 쪽 Prefab_EnhanceEquipPreview에 붙는 스크립트.
/// 스탯 하나에 대해 "현재값 -> 다음값 (증가량)"을 표시.
/// </summary>
public class EnhancePreviewRow : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI _statTxt;
    [SerializeField] private TextMeshProUGUI _currentValue;
    [SerializeField] private TextMeshProUGUI _nextValue;
    [SerializeField] private TextMeshProUGUI _growthValue;

    public void Setup(string statLabel, int currentValue, int nextValue)
    {
        if (_statTxt != null)
        {
            _statTxt.text = statLabel;
        }

        if (_currentValue != null)
        {
            _currentValue.text = currentValue.ToString();
        }

        if (_nextValue != null)
        {
            _nextValue.text = nextValue.ToString();
        }

        if (_growthValue != null)
        {
            int growth = nextValue - currentValue;
            _growthValue.text = growth > 0 ? $"(+{growth})" : "";
        }
    }
}