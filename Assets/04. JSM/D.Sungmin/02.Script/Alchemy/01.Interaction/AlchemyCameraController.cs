using UnityEngine;
using Unity.Cinemachine;

namespace WitchChronicle.Alchemy
{
    /// <summary>
    /// 시네머신 Virtual Camera 우선순위 스왑 방식.
    /// 낚시 시스템과 동일한 카메라 전환 패턴.
    /// </summary>
    public class AlchemyCameraController : MonoBehaviour
    {
        [Header("시네머신 가상 카메라")]
        [SerializeField] private CinemachineCamera _playerVCam;   // 플레이어 따라다니는 vcam
        [SerializeField] private CinemachineCamera _alchemyVCam;  // 가마솥 뷰 vcam

        [Header("우선순위")]
        [SerializeField] private int _highPriority = 20;
        [SerializeField] private int _lowPriority = 5;

        private void Awake()
        {
            // 시작 시 플레이어 카메라 우세
            SwitchToPlayerView();
        }

        public void SwitchToAlchemyView()
        {
            if (_alchemyVCam != null) _alchemyVCam.Priority = _highPriority;
            if (_playerVCam != null) _playerVCam.Priority = _lowPriority;
        }

        public void SwitchToPlayerView()
        {
            if (_alchemyVCam != null) _alchemyVCam.Priority = _lowPriority;
            if (_playerVCam != null) _playerVCam.Priority = _highPriority;
        }
    }
}