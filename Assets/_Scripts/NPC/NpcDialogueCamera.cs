using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// NPC 대화용 시네마틱 카메라.
///
/// 플레이어와 NPC를 잡는 공용 CinemachineCamera를 사용.
/// NPC 상호작용이 시작되면 대화 카메라를 활성화하고,
/// 대화/상점/강화 등 모든 후속 UI까지 종료된 뒤 원래 카메라로 복귀한다.
///
/// 카메라 복귀 시 해당 NPC의 WorldInteractionUI도 다시 활성화한다.
/// </summary>
public class NpcDialogueCamera : MonoBehaviour
{
    public static NpcDialogueCamera Instance { get; private set; }

    [Header("대화 전용 시네머신 카메라")]
    [SerializeField]
    private CinemachineCamera _dialogueCamera;

    [Header("우선순위")]
    [SerializeField]
    private int _highPriority = 20;

    [SerializeField]
    private int _lowPriority = 0;

    [Header("카메라 배치 (플레이어 기준)")]
    [Tooltip("NPC 반대 방향으로 물러나는 거리. 클수록 멀리서 잡는다.")]
    [SerializeField]
    private float _backDistance = 2.6f;

    [Tooltip("옆으로 비켜서는 거리. 0이면 플레이어 뒤통수에 가려진다. 음수면 반대쪽 어깨.")]
    [SerializeField]
    private float _sideOffset = 1.3f;

    [Tooltip("카메라 높이.")]
    [SerializeField]
    private float _height = 1.7f;

    [Header("바라보는 지점")]
    [Tooltip("플레이어와 NPC의 중간 지점을 기준으로 한 높이.")]
    [SerializeField]
    private float _lookHeight = 1.4f;

    [Tooltip("0이면 정확히 중간, 1에 가까울수록 NPC 쪽을 본다.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float _lookBias = 0.6f;

    private bool _isActive;

    // 현재 대화/상호작용 중인 NPC
    private NPC _currentNPC;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                $"[NpcDialogueCamera] 이미 다른 인스턴스({Instance.name})가 있어 " +
                $"'{name}'의 이 컴포넌트는 사용하지 않습니다. 씬에 하나만 두세요."
            );

            enabled = false;
            return;
        }

        Instance = this;

        Release();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 플레이어와 NPC를 함께 잡는 위치로 카메라를 옮기고 활성화.
    /// </summary>
    public void Focus(
        Transform player,
        NPC npc)
    {
        if (_dialogueCamera == null ||
            player == null ||
            npc == null)
        {
            return;
        }

        Transform npcTransform = npc.transform;

        Vector3 toNpc =
            npcTransform.position - player.position;

        toNpc.y = 0f;

        if (toNpc.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // 현재 상호작용 NPC 저장
        _currentNPC = npc;

        Vector3 forward =
            toNpc.normalized;

        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                forward
            );

        Vector3 cameraPosition =
            player.position
            - forward * _backDistance
            + side * _sideOffset
            + Vector3.up * _height;

        Vector3 lookPoint =
            Vector3.Lerp(
                player.position,
                npcTransform.position,
                _lookBias
            )
            + Vector3.up * _lookHeight;

        _dialogueCamera.transform.SetPositionAndRotation(
            cameraPosition,
            Quaternion.LookRotation(
                lookPoint - cameraPosition
            )
        );

        _dialogueCamera.Priority =
            _highPriority;

        _isActive = true;
    }

    /// <summary>
    /// 원래 카메라로 되돌리고
    /// NPC의 월드 상호작용 UI를 다시 활성화한다.
    /// </summary>
    public void Release()
    {
        if (_dialogueCamera != null)
        {
            _dialogueCamera.Priority = _lowPriority;
        }

        _isActive = false;

        if (_currentNPC != null)
        {
            // 기존 거리 / Interact 상태를 유지한 채
            // World Interaction UI 표시 제한만 해제
            _currentNPC.SetWorldInteractionUISuppressed(false);

            _currentNPC = null;
        }
    }

    private void Update()
    {
        if (_isActive == false)
        {
            return;
        }

        // 대화창뿐 아니라 상점/강화 같은 후속 UI까지 모두 닫혀
        // 플레이어가 완전히 필드 조작권을 되찾았을 때 복귀
        if (IsInteractionOpen() == false)
        {
            Release();
        }
    }

    /// <summary>
    /// NPC와의 상호작용이 아직 이어지는 중인지 판단.
    /// </summary>
    private static bool IsInteractionOpen()
    {
        if (CursorLocker.Instance != null)
        {
            return CursorLocker.Instance.IsUIMode;
        }

        return DialogueUI.Instance != null &&
               DialogueUI.Instance.IsPanelActive;
    }
}