using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Result 패널의 캐릭터별 경험치 결과 한 줄(Prefab_CharacterXp) 표시 담당.
/// </summary>
public class CharacterXpRow : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private Image _icon; // 일단 비워둠 (추후 아이콘 데이터 연결)
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _levelTxt;
    [SerializeField] private GameObject _levelUpImg;

    [Header("Exp")]
    [SerializeField] private Slider _xpSlider;
    [SerializeField] private TMP_Text _earnedXpTxt;
    [SerializeField] private TMP_Text _requiredXpTxt;

    /// <summary>
    /// 보상 결과 데이터를 UI에 반영.
    /// </summary>
    public void SetData(CharacterRewardResult result)
    {
        if (result == null)
        {
            return;
        }

        if (_nameTxt != null) _nameTxt.text = result.CharacterName;
        if (_levelTxt != null) _levelTxt.text = $"Lv. {result.LevelAfter}";
        if (_levelUpImg != null) _levelUpImg.SetActive(result.DidLevelUp);

        if (_xpSlider != null)
        {
            _xpSlider.maxValue = Mathf.Max(1, result.RequiredExp);
            _xpSlider.value = result.CurrentExp;
        }

        if (_earnedXpTxt != null) _earnedXpTxt.text = $"+ {result.ExpGained}";
        if (_requiredXpTxt != null) _requiredXpTxt.text = $"{result.CurrentExp} / {result.RequiredExp}";
    }
}