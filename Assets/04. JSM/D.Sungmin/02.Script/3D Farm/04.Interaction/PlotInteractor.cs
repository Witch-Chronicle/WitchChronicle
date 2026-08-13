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
            // if (_promptUI != null) _promptUI.SetActive(false);
        }

        private void Update()
        {
            if (!_isPlayerInside) return;
            if (Keyboard.current == null) return;

            // 이미 Plot 관련 패널이 열려있으면(다른 슬롯 포함) F 입력을 무시.
            // 이걸 막지 않으면 패널이 열린 채로 다시 F를 눌러 카운터가 중복으로 올라가고,
            // Esc를 눌렀을 때 LifeUIManager가 아니라 PausePanel 쪽으로 새어나가는 문제가 생긴다.
            if (PlotManager.Instance != null && PlotManager.Instance.IsAnyPanelOpen)
            {
                return;
            }

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                HandleInteraction();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _isPlayerInside = true;
            // if (_promptUI != null) _promptUI.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _isPlayerInside = false;
            // if (_promptUI != null) _promptUI.SetActive(false);
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