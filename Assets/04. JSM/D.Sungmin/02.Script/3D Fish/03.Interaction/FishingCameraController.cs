using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 낚시 카메라 Priority 스위칭 담당.
/// 평소엔 낮게 두고, 낚시 진입 시 높여서 활성화.
/// </summary>
public class FishingCameraController : MonoBehaviour
{
    [Header("Cinemachine Camera")]
    [SerializeField] private CinemachineCamera fishingCamera;

    [Header("Priority")]
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 5;

    private void Awake()
    {
        if (fishingCamera != null)
            fishingCamera.Priority = inactivePriority;
    }

    public void SwitchToFishing()
    {
        fishingCamera.Priority = activePriority;
    }

    public void SwitchToMain()
    {
        fishingCamera.Priority = inactivePriority;
    }
}