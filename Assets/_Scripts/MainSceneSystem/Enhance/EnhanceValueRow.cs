using UnityEngine;
using TMPro;

/// <summary>
/// EnhancePanel/Enhance/Info/Main의 Current/Next 양쪽 Preview에서 공통으로 쓰는 스탯 행.
/// 라벨(스탯명) + 값만 표시.
/// </summary>
public class EnhanceValueRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _labelTxt;
    [SerializeField] private TextMeshProUGUI _valueTxt;

    public void Setup(string label, int value)
    {
        if (_labelTxt != null) _labelTxt.text = label;
        if (_valueTxt != null) _valueTxt.text = value.ToString();
    }
}