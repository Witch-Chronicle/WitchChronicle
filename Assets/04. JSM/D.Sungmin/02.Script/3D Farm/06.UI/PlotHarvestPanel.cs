using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 수확 확정 팝업 (ReadyToHarvest 상태 상호작용)
    /// 수확하기 → 같은 씨앗으로 다음 사이클
    /// 씨앗 교체 → 수확 후 씨앗 선택 팝업 오픈
    /// </summary>
    public class PlotHarvestPanel : MonoBehaviour
    {
        [Header("UI - 루트")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _closeButton;

        [Header("UI - 씨앗 정보")]
        [SerializeField] private Image _seedIcon;
        [SerializeField] private TextMeshProUGUI _seedName;
        [SerializeField] private TextMeshProUGUI _cycleInfo;

        [Header("UI - 수확 정보")]
        [SerializeField] private Image _harvestIcon;
        [SerializeField] private TextMeshProUGUI _harvestText;

        [Header("UI - 버튼")]
        [SerializeField] private Button _harvestButton;
        [SerializeField] private Button _swapButton;

        private PlotSlot _targetSlot;
        private bool _isOpen;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _harvestButton.onClick.AddListener(OnHarvestClicked);
            _swapButton.onClick.AddListener(OnSwapClicked);
            if (_root != null) _root.SetActive(false);
        }

        public void Open(PlotSlot slot)
        {
            if (slot == null || slot.PlantedSeed == null) return;

            _targetSlot = slot;
            var seed = slot.PlantedSeed;

            if (_seedIcon != null && seed.seedSprite != null)
                _seedIcon.sprite = seed.seedSprite;
            if (_seedName != null)
                _seedName.text = seed.seedName;

            int minutes = Mathf.RoundToInt(seed.growthTime / 60f);
            if (_cycleInfo != null)
                _cycleInfo.text = $"{minutes}분마다 {seed.harvestAmount}개";

            if (_harvestIcon != null && seed.harvestSprite != null)
                _harvestIcon.sprite = seed.harvestSprite;
            if (_harvestText != null)
                _harvestText.text = $"{seed.harvestName} x {slot.PendingHarvestCount}";

            _root.SetActive(true);

            if (!_isOpen)
            {
                _isOpen = true;
                if (PlotManager.Instance != null) PlotManager.Instance.NotifyPanelOpened();
            }
        }

        public void Close()
        {
            _root.SetActive(false);
            _targetSlot = null;

            if (_isOpen)
            {
                _isOpen = false;
                if (PlotManager.Instance != null) PlotManager.Instance.NotifyPanelClosed();
            }
        }

        private void OnHarvestClicked()
        {
            if (_targetSlot == null) return;

            _targetSlot.Harvest();
            Close();
        }

        private void OnSwapClicked()
        {
            if (_targetSlot == null) return;
            var slot = _targetSlot;

            slot.Harvest();
            Close();

            if (PlotManager.Instance != null && PlotManager.Instance.SeedSelectPanel != null)
                PlotManager.Instance.SeedSelectPanel.Open(slot);
        }
    }
}