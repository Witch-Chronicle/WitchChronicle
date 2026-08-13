using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 필드 전투 진입 카메라 제어
/// </summary>
public class FieldEncounterCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera _encounterCamera;

    [Header("Priority")]
    [SerializeField] private int _activePriority = 40;
    [SerializeField] private int _inactivePriority = 0;

    [Header("Player Advantage View")]
    [Tooltip("플레이어 기준 카메라 후방 거리")]
    [SerializeField] private float _backDistance = 2.8f;

    [Tooltip("플레이어 기준 카메라 측면 거리")]
    [SerializeField] private float _sideOffset = 1.4f;

    [Tooltip("플레이어 기준 카메라 높이")]
    [SerializeField] private float _cameraHeight = 0.7f;

    [Tooltip("플레이어 시선 기준 높이")]
    [SerializeField] private float _playerFocusHeight = 1.15f;

    [Tooltip("시선 중심의 적 반영 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float _targetFocusWeight = 0.72f;

    [Tooltip("최종 시선 높이 보정")]
    [SerializeField] private float _lookHeightOffset = 0.1f;

    /// <summary>
    /// 카메라 참조 초기화
    /// </summary>
    private void Awake()
    {
        if (_encounterCamera == null)
        {
            _encounterCamera =
                GetComponent<CinemachineCamera>();
        }

        SetCameraActive(false);
    }

    /// <summary>
    /// 비활성화 시 우선순위 초기화
    /// </summary>
    private void OnDisable()
    {
        SetCameraActive(false);
    }

    /// <summary>
    /// 플레이어 선공 구도 재생
    /// </summary>
    /// <param name="player">플레이어 Transform</param>
    /// <param name="target">피격 대상</param>
    public void PlayPlayerAdvantageView(
        Transform player,
        FieldCombatTarget target)
    {
        if (_encounterCamera == null ||
            player == null ||
            target == null)
        {
            return;
        }

        Vector3 playerFocusPosition =
            player.position +
            Vector3.up *
            _playerFocusHeight;

        Vector3 targetPosition =
            target.GetAimPosition();

        Vector3 forward =
            targetPosition -
            playerFocusPosition;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
        {
            forward =
                player.forward;
        }

        forward.Normalize();

        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                forward).normalized;

        Vector3 cameraPosition =
            playerFocusPosition -
            forward *
            _backDistance +
            right *
            _sideOffset +
            Vector3.up *
            _cameraHeight;

        Vector3 lookPosition =
            Vector3.Lerp(
                playerFocusPosition,
                targetPosition,
                _targetFocusWeight) +
            Vector3.up *
            _lookHeightOffset;

        Vector3 lookDirection =
            lookPosition -
            cameraPosition;

        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        transform.SetPositionAndRotation(
            cameraPosition,
            Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up));

        SetCameraActive(true);

        Debug.Log(
            "[FieldEncounterCamera] " +
            "플레이어 선공 구도 활성화");
    }

    /// <summary>
    /// 전투 진입 카메라 해제
    /// </summary>
    public void DeactivateCamera()
    {
        SetCameraActive(false);
    }

    /// <summary>
    /// 카메라 활성 상태 설정
    /// </summary>
    /// <param name="isActive">활성 여부</param>
    private void SetCameraActive(
        bool isActive)
    {
        if (_encounterCamera == null)
        {
            return;
        }

        _encounterCamera.Priority =
            isActive
                ? _activePriority
                : _inactivePriority;
    }
}