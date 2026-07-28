using TMPro;
using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 밭 위에 떠 있는 UI (월드스페이스 Canvas)
    /// - Growing 상태: 남은 시간 표시
    /// - ReadyToHarvest 상태: 대기 개수 표시
    /// </summary>
    public class PlotFloatingUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PlotSlot _plotSlot;
        
        [Header("UI 오브젝트")]
        [SerializeField] private GameObject _timerRoot;      // Growing 시 표시
        [SerializeField] private TMP_Text _timerText;
        
        [SerializeField] private GameObject _countRoot;      // ReadyToHarvest 시 표시
        [SerializeField] private TMP_Text _countText;
        
        [Header("카메라 바라보기")]
        [SerializeField] private bool _faceCamera = true;
        
        private Camera _mainCamera;
        
        private void Awake()
        {
            if (_plotSlot == null) _plotSlot = GetComponentInParent<PlotSlot>();
            _mainCamera = Camera.main;
            
            HideAll();
        }
        
        private void Update()
        {
            if (_plotSlot == null) return;
            
            // 카메라 방향으로 회전
            if (_faceCamera && _mainCamera != null)
            {
                Vector3 lookDir = transform.position - _mainCamera.transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDir);
            }
            
            // 상태에 따라 UI 갱신
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            switch (_plotSlot.State)
            {
                case PlotState.Growing:
                    ShowTimer();
                    break;
                    
                case PlotState.ReadyToHarvest:
                    ShowCount();
                    break;
                    
                default:
                    HideAll();
                    break;
            }
        }
        
        private void ShowTimer()
        {
            if (_timerRoot != null) _timerRoot.SetActive(true);
            if (_countRoot != null) _countRoot.SetActive(false);
            
            if (_timerText != null)
            {
                float remaining = _plotSlot.GetRemainingSeconds();
                int min = Mathf.FloorToInt(remaining / 60f);
                int sec = Mathf.FloorToInt(remaining % 60f);
                _timerText.text = $"{min:D2}:{sec:D2}";
            }
        }
        
        private void ShowCount()
        {
            if (_timerRoot != null) _timerRoot.SetActive(false);
            if (_countRoot != null) _countRoot.SetActive(true);
            
            if (_countText != null)
            {
                _countText.text = $"x{_plotSlot.PendingHarvestCount}";
            }
        }
        
        private void HideAll()
        {
            if (_timerRoot != null) _timerRoot.SetActive(false);
            if (_countRoot != null) _countRoot.SetActive(false);
        }
    }
}