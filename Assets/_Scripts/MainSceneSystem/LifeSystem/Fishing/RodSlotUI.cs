using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RodSlotUI : MonoBehaviour
{
    [Header("표시 요소")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private GameObject equippedHighlight;
    [SerializeField] private Button equipButton;

    [Header("등급별 카드 배경 색")]
    [SerializeField] private Color rank1Color = new Color(0.85f, 0.85f, 0.85f);       // 회색 (일반)
    [SerializeField] private Color rank2Color = new Color(0.55f, 0.75f, 1.0f);        // 파랑 (희귀)
    [SerializeField] private Color rank3Color = new Color(1.0f, 0.75f, 0.3f);         // 금색 (전설)

    [Header("장착 하이라이트")]
    [SerializeField] private Color equippedBorderColor = new Color(1f, 0.95f, 0.5f);

    private RodItemData boundRod;

    private void Awake()
    {
        if (equipButton != null)
            equipButton.onClick.AddListener(OnClickEquip);
    }

    public void Setup(RodItemData rod, bool isEquipped)
    {
        boundRod = rod;
        if (rod == null) return;

        if (iconImage != null) iconImage.sprite = rod.icon;
        if (nameText != null)  nameText.text    = rod.itemName;

        // 등급별 카드 배경 색
        if (cardBackground != null)
            cardBackground.color = GetRankColor(rod.rodRank);

        // 장착 하이라이트
        if (equippedHighlight != null) equippedHighlight.SetActive(isEquipped);

        // 버튼: 장착 중이면 비활성화
        if (equipButton != null) equipButton.interactable = !isEquipped;
    }

    private Color GetRankColor(int rank)
    {
        switch (rank)
        {
            case 1: return rank1Color;
            case 2: return rank2Color;
            case 3: return rank3Color;
            default: return rank1Color;
        }
    }

    private void OnClickEquip()
    {
        if (boundRod == null || FishingManager.Instance == null) return;
        FishingManager.Instance.EquipRod(boundRod);
    }
}