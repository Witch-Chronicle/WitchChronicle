using UnityEngine;
using UnityEngine.InputSystem;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 밭 슬롯 상호작용 처리
    /// (FloatingUI 표시는 FarmZoneTrigger가 밭 전체 영역 기준으로 담당)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlotInteractor : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlotSlot _plotSlot;
        [SerializeField] private GameObject _promptUI;

        private bool _isPlayerInside;

        private void Reset()
        {
            _plotSlot = GetComponent<PlotSlot>();
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            if (_plotSlot == null) _plotSlot = GetComponent<PlotSlot>();
            if (_promptUI != null) _promptUI.SetActive(false);
        }

        private void Update()
        {
            if (!_isPlayerInside) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                HandleInteraction();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _isPlayerInside = true;
            if (_promptUI != null) _promptUI.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _isPlayerInside = false;
            if (_promptUI != null) _promptUI.SetActive(false);
        }

        private void HandleInteraction()
        {
            if (_plotSlot == null || PlotManager.Instance == null) return;

            switch (_plotSlot.State)
            {
                case PlotState.Locked:
                {
                    int cost = PlotManager.Instance.GetUnlockCost(_plotSlot.PlotIndex);
                    if (PlotManager.Instance.UnlockPanel != null)
                        PlotManager.Instance.UnlockPanel.Open(_plotSlot, cost);
                    break;
                }

                case PlotState.Empty:
                {
                    if (PlotManager.Instance.SeedSelectPanel != null)
                        PlotManager.Instance.SeedSelectPanel.Open(_plotSlot);
                    break;
                }

                case PlotState.Growing:
                    // Growing 중엔 상호작용 없음 (FloatingUI로 정보 확인)
                    break;

                case PlotState.ReadyToHarvest:
                {
                    if (PlotManager.Instance.HarvestPanel != null)
                        PlotManager.Instance.HarvestPanel.Open(_plotSlot);
                    break;
                }
            }
        }
    }
}