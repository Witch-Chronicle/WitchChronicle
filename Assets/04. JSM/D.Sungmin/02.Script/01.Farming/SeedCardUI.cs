using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SeedCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image cardBackground;
    public Image seedIcon;
    public TextMeshProUGUI seedNameText;
    public TextMeshProUGUI countText;
    public Button selectButton;
    public GameObject selectedIndicator;   // 선택됨 표시 (선택시만 활성)

    [Header("색상")]
    public Color normalColor = new Color(0.96f, 0.92f, 0.85f, 1f);
    public Color selectedColor = new Color(1f, 0.95f, 0.75f, 1f);

    public SeedItemData SeedData { get; private set; }
    private Action<SeedItemData> onSelected;

    public void Setup(SeedItemData seed, Action<SeedItemData> callback)
    {
        SeedData = seed;
        onSelected = callback;

        seedNameText.text = seed.itemName;

        if (seed.icon != null)
            seedIcon.sprite = seed.icon;

        int count = SeedSelectPopup.GetOwnedCount(seed);
        countText.text = $"x{count}";

        // 개수 0이면 반투명 처리
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = count > 0 ? 1f : 0.5f;

        SetSelected(false);

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onSelected?.Invoke(SeedData));
    }

    public void SetSelected(bool selected)
    {
        cardBackground.color = selected ? selectedColor : normalColor;
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
    }
}