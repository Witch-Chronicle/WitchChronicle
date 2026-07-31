using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 밭 전체 영역 트리거
    /// 플레이어가 이 영역에 들어오면 PlotManager를 통해 모든 FloatingUI 표시/숨김
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FarmZoneTrigger : MonoBehaviour
    {
        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SetAllNear(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SetAllNear(false);
        }

        private void SetAllNear(bool near)
        {
            if (PlotManager.Instance == null)
            {
                Debug.LogWarning("[FarmZoneTrigger] PlotManager.Instance 없음");
                return;
            }
            PlotManager.Instance.SetAllFloatingUIsPlayerNear(near);
        }
    }
}