using UnityEngine;

/// <summary>
/// 상호작용한 상대(플레이어) 쪽으로 NPC를 부드럽게 돌리고,
/// 대화가 끝나면 원래 방향으로 되돌린다.
///
/// NPC 오브젝트에 NPC 스크립트와 같이 붙여서 사용.
/// 좌우(Y축)만 회전시키므로 플레이어가 높이 차이가 있는 곳에 서 있어도 NPC가 기울지 않는다.
/// </summary>
[RequireComponent(typeof(NPC))]
public class NpcLookAtPlayer : MonoBehaviour
{
    [Header("회전 속도")]
    [Tooltip("클수록 빨리 돌아본다. 4~10 정도가 자연스럽다.")]
    [SerializeField] private float _turnSpeed = 6f;

    [Header("복귀")]
    [Tooltip("대화가 끝난 뒤 원래 방향으로 돌아가기까지의 대기 시간(초).")]
    [SerializeField] private float _restoreDelay = 0.4f;

    private Quaternion _originalRotation;
    private Quaternion _targetRotation;
    private bool _isFacing;
    private float _restoreTimer;

    private void Awake()
    {
        _originalRotation = transform.rotation;
        _targetRotation = _originalRotation;
    }

    /// <summary>
    /// 지정한 대상 쪽을 바라보기 시작한다. NPC.Interact()에서 호출.
    /// </summary>
    /// <param name="target">바라볼 대상(플레이어)</param>
    public void FaceTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        // 플레이어가 NPC와 정확히 같은 위치에 겹쳐 있으면 방향을 만들 수 없다.
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        _targetRotation = Quaternion.LookRotation(direction);
        _isFacing = true;
        _restoreTimer = 0f;
    }

    private void Update()
    {
        if (_isFacing)
        {
            // 대화창뿐 아니라 상점·강화 같은 후속 UI까지 모두 닫힌 뒤에 복귀 대기 시작
            if (IsInteractionOpen() == false)
            {
                _restoreTimer += Time.deltaTime;

                if (_restoreTimer >= _restoreDelay)
                {
                    _targetRotation = _originalRotation;
                    _isFacing = false;
                }
            }
            else
            {
                _restoreTimer = 0f;
            }
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _targetRotation,
            1f - Mathf.Exp(-_turnSpeed * Time.deltaTime));
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
