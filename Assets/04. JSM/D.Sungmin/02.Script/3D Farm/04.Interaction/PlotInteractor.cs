using UnityEngine;
using UnityEngine.InputSystem;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 밭 슬롯 상호작용 처리
    /// Player가 Trigger 안에 있을 때 E키로 상태별 UI 오픈
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlotInteractor : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlotSlot _plotSlot;
        [SerializeField] private GameObject _promptUI;  // "E키로 상호작용" 월드스페이스 UI (선택)

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

            if (Keyboard.current.eKey.wasPressedThisFrame)
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
                    // TODO: PlotGrowingPanel (다음 단계)
                    Debug.Log($"[PlotInteractor] Growing → progress={_plotSlot.GetGrowthProgress():F2}");
                    break;

                case PlotState.ReadyToHarvest:
                    // TODO: PlotHarvestPanel (다음 단계)
                    // 우선 자동 수확 (임시)
                    _plotSlot.Harvest();
                    Debug.Log("[PlotInteractor] ReadyToHarvest → 자동 수확 (임시)");
                    break;
            }
        }
    }
}