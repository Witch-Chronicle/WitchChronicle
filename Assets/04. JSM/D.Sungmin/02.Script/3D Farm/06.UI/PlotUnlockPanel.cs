using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 잠긴 슬롯 해제 확인 팝업
    /// </summary>
    public class PlotUnlockPanel : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _currentGoldText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private PlotSlot _targetSlot;
        private int _unlockCost;
        private bool _isOpen;

        private void Awake()
        {
            _confirmButton.onClick.AddListener(OnConfirm);
            _cancelButton.onClick.AddListener(Close);
            if (_root != null) _root.SetActive(false);
        }

        public void Open(PlotSlot slot, int cost)
        {
            _targetSlot = slot;
            _unlockCost = cost;

            _costText.text = $"{cost:N0} G";
            int currentGold = PlayerInventory.Instance != null ? PlayerInventory.Instance.Gold : 0;
            _currentGoldText.text = $"보유: {currentGold:N0} G";

            _confirmButton.interactable = currentGold >= cost;

            _root.SetActive(true);

            if (!_isOpen)
            {
                _isOpen = true;
                if (PlotManager.Instance != null)
                    PlotManager.Instance.NotifyPanelOpened();
            }
        }

        public void Close()
        {
            _root.SetActive(false);
            _targetSlot = null;

            if (_isOpen)
            {
                _isOpen = false;
                if (PlotManager.Instance != null)
                    PlotManager.Instance.NotifyPanelClosed();
            }
        }

        private void OnConfirm()
        {
            if (_targetSlot == null) return;

            if (PlayerInventory.Instance == null || !PlayerInventory.Instance.TrySpendGold(_unlockCost))
            {
                Debug.LogWarning("[PlotUnlockPanel] 골드 부족 또는 인벤토리 없음");
                return;
            }

            _targetSlot.Unlock();
            Close();
        }
    }
}