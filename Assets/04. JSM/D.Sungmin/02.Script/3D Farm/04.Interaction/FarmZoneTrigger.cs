using System.Collections.Generic;
using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 밭 전체 영역 트리거
    /// 플레이어가 이 영역에 들어오면 모든 밭의 FloatingUI를 한꺼번에 표시/숨김
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FarmZoneTrigger : MonoBehaviour
    {
        [Header("표시할 FloatingUI 목록")]
        [Tooltip("비워두면 Awake에서 씬에 있는 PlotFloatingUI를 자동 수집")]
        [SerializeField] private List<PlotFloatingUI> _floatingUIs = new List<PlotFloatingUI>();

        [Header("옵션")]
        [SerializeField] private bool _autoCollectOnAwake = true;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            if (_autoCollectOnAwake && _floatingUIs.Count == 0)
            {
                _floatingUIs.AddRange(FindObjectsOfType<PlotFloatingUI>(true));
            }
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
            for (int i = 0; i < _floatingUIs.Count; i++)
            {
                if (_floatingUIs[i] != null)
                    _floatingUIs[i].SetPlayerNear(near);
            }
        }
    }
}