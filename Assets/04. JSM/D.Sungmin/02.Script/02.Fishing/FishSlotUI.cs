using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image gradeFrame;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("등급별 프레임 색")]
    [SerializeField] private Color commonColor    = new Color(0.72f, 0.72f, 0.72f);
    [SerializeField] private Color rareColor      = new Color(0.30f, 0.60f, 1.00f);
    [SerializeField] private Color legendaryColor = new Color(1.00f, 0.72f, 0.20f);

    public void Setup(FishItemData fish, int count)
    {
        if (fish == null) return;
        if (iconImage != null)  iconImage.sprite = fish.icon;
        if (nameText != null)   nameText.text    = fish.itemName;
        if (countText != null)  countText.text   = count > 1 ? $"x{count}" : "";
        if (gradeFrame != null) gradeFrame.color = GetGradeColor(fish.grade);
    }

    private Color GetGradeColor(FishGrade g)
    {
        switch (g)
        {
            case FishGrade.Common:    return commonColor;
            case FishGrade.Rare:      return rareColor;
            case FishGrade.Legendary: return legendaryColor;
        }
        return commonColor;
    }
}