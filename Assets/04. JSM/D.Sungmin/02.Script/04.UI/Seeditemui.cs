using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 씨앗 선택 팝업 안의 개별 씨앗 항목.
/// 씨앗 이름, 성장 시간, 아이콘을 표시하고 클릭하면 콜백을 호출한다.
/// </summary>
public class SeedItemUI : MonoBehaviour
{
    public Image seedIcon;
    public TextMeshProUGUI seedNameText;
    public TextMeshProUGUI growthTimeText;
    public Button selectButton;

    private SeedData seedData;
    private Action<SeedData> onSelected;

    public void Setup(SeedData seed, Action<SeedData> callback)
    {
        seedData = seed;
        onSelected = callback;

        seedNameText.text = seed.seedName;

        // 성장 시간을 분:초로 표시
        int min = Mathf.FloorToInt(seed.growthTime / 60f);
        int sec = Mathf.FloorToInt(seed.growthTime % 60f);
        growthTimeText.text = $"{min}분 {sec:00}초";

        if (seed.seedSprite != null)
            seedIcon.sprite = seed.seedSprite;

        selectButton.onClick.AddListener(() => onSelected?.Invoke(seedData));
    }
}