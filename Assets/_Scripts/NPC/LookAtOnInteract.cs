using UnityEngine;

/// <summary>
/// 상호작용 상대 쪽으로 부드럽게 돌아본다.
///
/// NPC와 플레이어 양쪽에 붙일 수 있다.
///  - NPC에 붙이면: 말을 건 플레이어를 바라보고, 대화가 끝나면 원래 방향으로 복귀.
///  - 플레이어에 붙이면: 말을 건 NPC를 바라본다. 복귀는 꺼두고 쓴다.
///
/// 좌우(Y축)만 회전시키므로 상대가 높이 차이가 있는 곳에 있어도 기울지 않는다.
///
/// 중요: 상호작용 중이 아닐 때는 transform.rotation을 건드리지 않는다.
/// 매 프레임 회전을 쓰면 PlayerController의 이동 회전과 충돌한다.
/// </summary>
public class LookAtOnInteract : MonoBehaviour
{
    [Header("회전 속도")]
    [Tooltip("클수록 빨리 돌아본다. 4~10 정도가 자연스럽다.")]
    [SerializeField] private float _turnSpeed = 6f;

    [Header("복귀")]
    [Tooltip("대화가 끝나면 원래 방향으로 되돌아간다. 플레이어에게는 꺼두는 것을 권장.")]
    [SerializeField] private bool _restoreOriginalRotation = true;

    [Tooltip("대화가 끝난 뒤 복귀를 시작하기까지의 대기 시간(초).")]
    [SerializeField] private float _restoreDelay = 0.4f;

    private Quaternion _originalRotation;
    private Quaternion _targetRotation;
    private float _restoreTimer;

    /// <summary>회전을 제어 중인지. false면 transform에 손대지 않는다.</summary>
    private bool _isDriving;

    /// <summary>상호작용이 끝나 복귀 대기/진행 중인지.</summary>
    private bool _isReleasing;

    private void Awake()
    {
        _originalRotation = transform.rotation;
        _targetRotation = _originalRotation;
    }

    /// <summary>
    /// 지정한 대상 쪽을 바라보기 시작한다. NPC.Interact()에서 호출.
    /// </summary>
    /// <param name="target">바라볼 대상</param>
    public void FaceTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        // 상대가 정확히 같은 위치에 겹쳐 있으면 방향을 만들 수 없다.
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // 대화 시작 시점의 방향을 기억해두면, 그 사이 이동해 있었더라도
        // 엉뚱한 스폰 방향이 아니라 직전 방향으로 되돌아간다.
        if (_isDriving == false)
        {
            _originalRotation = transform.rotation;
        }

        _targetRotation = Quaternion.LookRotation(direction);
        _isDriving = true;
        _isReleasing = false;
        _restoreTimer = 0f;
    }

    private void Update()
    {
        if (_isDriving == false)
        {
            return;
        }

        if (_isReleasing == false && IsInteractionOpen() == false)
        {
            _restoreTimer += Time.deltaTime;

            if (_restoreTimer >= _restoreDelay)
            {
                if (_restoreOriginalRotation)
                {
                    _targetRotation = _originalRotation;
                    _isReleasing = true;
                }
                else
                {
                    // 복귀하지 않는 설정이면 여기서 제어를 완전히 놓는다.
                    _isDriving = false;
                    return;
                }
            }
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _targetRotation,
            1f - Mathf.Exp(-_turnSpeed * Time.deltaTime));

        // 복귀가 끝나면 더 이상 회전을 건드리지 않는다.
        if (_isReleasing && Quaternion.Angle(transform.rotation, _targetRotation) < 0.5f)
        {
            transform.rotation = _targetRotation;
            _isDriving = false;
            _isReleasing = false;
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
