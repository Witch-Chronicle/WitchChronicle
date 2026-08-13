using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 밭 하나의 트리거 존.
    /// 플레이어가 이 영역에 들어오면 담당 슬롯의 FloatingUI만 표시.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FarmZoneTrigger : MonoBehaviour
    {
        [Header("담당 밭")]
        [SerializeField] private PlotSlot _targetSlot;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[FarmZone] Enter: {other.name} / tag: {other.tag}");
            if (!other.CompareTag("Player")) return;
            SetSlotNear(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SetSlotNear(false);
        }

        private void SetSlotNear(bool near)
        {
            if (PlotManager.Instance == null)
            {
                Debug.LogWarning("[FarmZoneTrigger] PlotManager.Instance 없음");
                return;
            }

            if (_targetSlot == null)
            {
                Debug.LogWarning($"[FarmZoneTrigger] {name} - Target Slot 미설정");
                return;
            }

            PlotManager.Instance.SetFloatingUINearBySlot(_targetSlot, near);
        }
    }
}