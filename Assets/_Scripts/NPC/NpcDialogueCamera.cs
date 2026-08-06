using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// NPC 대화용 시네마틱 카메라.
///
/// 씬에 빈 오브젝트 하나를 만들어 이 컴포넌트를 붙이고, 대화 전용 CinemachineCamera를 연결해서 쓴다.
/// NPC마다 카메라를 두지 않고 하나를 공용으로 쓰며, 상호작용할 때마다 위치를 계산해 옮긴다.
///
/// 배치 방식: 플레이어를 기준으로 NPC 반대쪽(뒤) + 옆으로 비켜선 지점에 카메라를 두어
/// 플레이어의 어깨 너머로 NPC가 보이는 구도를 만든다.
///
/// 연결할 CinemachineCamera는 Body/Aim을 모두 "Do Nothing"으로 두어야 한다.
/// (이 스크립트가 Transform을 직접 잡기 때문에, 절차적 컴포넌트가 있으면 서로 덮어쓴다.)
/// </summary>
public class NpcDialogueCamera : MonoBehaviour
{
    public static NpcDialogueCamera Instance { get; private set; }

    [Header("대화 전용 시네머신 카메라")]
    [SerializeField] private CinemachineCamera _dialogueCamera;

    [Header("우선순위")]
    [SerializeField] private int _highPriority = 20;
    [SerializeField] private int _lowPriority = 0;

    [Header("카메라 배치 (플레이어 기준)")]
    [Tooltip("NPC 반대 방향으로 물러나는 거리. 클수록 멀리서 잡는다.")]
    [SerializeField] private float _backDistance = 2.6f;

    [Tooltip("옆으로 비켜서는 거리. 0이면 플레이어 뒤통수에 가려진다. 음수면 반대쪽 어깨.")]
    [SerializeField] private float _sideOffset = 1.3f;

    [Tooltip("카메라 높이.")]
    [SerializeField] private float _height = 1.7f;

    [Header("바라보는 지점")]
    [Tooltip("플레이어와 NPC의 중간 지점을 기준으로 한 높이.")]
    [SerializeField] private float _lookHeight = 1.4f;

    [Tooltip("0이면 정확히 중간, 1에 가까울수록 NPC 쪽을 본다.")]
    [Range(0f, 1f)]
    [SerializeField] private float _lookBias = 0.6f;

    private bool _isActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 이 컴포넌트는 NPC처럼 다른 오브젝트에 같이 붙을 수 있으므로
            // 절대 gameObject를 파괴하면 안 된다. 자신만 비활성화한다.
            Debug.LogWarning(
                $"[NpcDialogueCamera] 이미 다른 인스턴스({Instance.name})가 있어 " +
                $"'{name}'의 이 컴포넌트는 사용하지 않습니다. 씬에 하나만 두세요.");

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
    /// 플레이어와 NPC를 함께 잡는 위치로 카메라를 옮기고 활성화한다.
    /// </summary>
    /// <param name="player">플레이어 Transform</param>
    /// <param name="npc">대화 상대 NPC Transform</param>
    public void Focus(Transform player, Transform npc)
    {
        if (_dialogueCamera == null || player == null || npc == null)
        {
            return;
        }

        Vector3 toNpc = npc.position - player.position;
        toNpc.y = 0f;

        if (toNpc.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 forward = toNpc.normalized;
        Vector3 side = Vector3.Cross(Vector3.up, forward);

        Vector3 cameraPosition =
            player.position
            - forward * _backDistance
            + side * _sideOffset
            + Vector3.up * _height;

        Vector3 lookPoint =
            Vector3.Lerp(player.position, npc.position, _lookBias)
            + Vector3.up * _lookHeight;

        _dialogueCamera.transform.SetPositionAndRotation(
            cameraPosition,
            Quaternion.LookRotation(lookPoint - cameraPosition));

        _dialogueCamera.Priority = _highPriority;
        _isActive = true;
    }

    /// <summary>
    /// 원래 카메라로 되돌린다.
    /// </summary>
    public void Release()
    {
        if (_dialogueCamera != null)
        {
            _dialogueCamera.Priority = _lowPriority;
        }

        _isActive = false;
    }

    private void Update()
    {
        if (_isActive == false)
        {
            return;
        }

        // 대화창뿐 아니라 상점·강화 같은 후속 UI까지 모두 닫혀
        // 플레이어가 조작권을 되찾은 시점에 원래 카메라로 복귀한다.
        // (대화창만 보면, 상점을 여는 순간 대화 패널이 닫히면서 카메라가 먼저 빠져버린다.)
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

        return DialogueUI.Instance != null && DialogueUI.Instance.IsPanelActive;
    }
}
