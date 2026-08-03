using UnityEngine;
using Unity.Cinemachine;

namespace WitchChronicle.Alchemy
{
    public class AlchemyCameraController : MonoBehaviour
    {
        [Header("시네머신 가상 카메라")]
        [SerializeField] private CinemachineCamera _alchemyVCam;

        [Header("우선순위")]
        [SerializeField] private int _highPriority = 20;
        [SerializeField] private int _lowPriority = 5;

        private void Awake()
        {
            SwitchToPlayerView();
        }

        public void SwitchToAlchemyView()
        {
            if (_alchemyVCam != null) _alchemyVCam.Priority = _highPriority;
        }

        public void SwitchToPlayerView()
        {
            if (_alchemyVCam != null) _alchemyVCam.Priority = _lowPriority;
        }
    }
}