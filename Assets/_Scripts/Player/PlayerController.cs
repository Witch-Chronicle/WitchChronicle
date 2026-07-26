using UnityEngine;
using UnityEngine.InputSystem;

/// 아리엘 필드 이동. 카메라 기준 방향으로 이동하고 이동 방향을 바라봄.
/// InputActionAsset에 "Player" 맵과 "Move"(Vector2) 액션 필요.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public InputActionAsset InputAsset;
    public float MoveSpeed = 4f;      // 평소 이동 (걷기~조깅)
    public float SprintSpeed = 6f;    // Shift 질주 (StarterAssets 달리기 임계값 5.335 이상이어야 Run 애니메이션)
    public float RotationSpeed = 12f;

    private CharacterController _controller;
    [SerializeField] private Transform _cameraTransform;
    private InputAction _moveAction;
    private InputAction _sprintAction;
    private float _verticalVelocity;
    private Animator _animator;                                  // 자식 모델의 Animator (없으면 무시)
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int GroundedParam = Animator.StringToHash("Grounded");
    private static readonly int MotionSpeedParam = Animator.StringToHash("MotionSpeed");
    private bool _hasGroundedParam;
    private bool _hasMotionSpeedParam;

    private const float Gravity = -9.81f;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _cameraTransform = Camera.main.transform;
        _moveAction = InputAsset.FindAction("Player/Move", throwIfNotFound: true);
        _sprintAction = InputAsset.FindAction("Player/Sprint", throwIfNotFound: true);
        _animator = GetComponentInChildren<Animator>();          // 모델이 자식으로 붙어 있으면 자동 연결

        // StarterAssets 컨트롤러처럼 추가 파라미터를 쓰는 경우 대응 (없으면 건드리지 않음)
        if (_animator != null)
        {
            foreach (var parameter in _animator.parameters)
            {
                if (parameter.nameHash == GroundedParam) _hasGroundedParam = true;
                if (parameter.nameHash == MotionSpeedParam) _hasMotionSpeedParam = true;
            }
        }
    }

    private void OnEnable() => InputAsset.FindActionMap("Player", throwIfNotFound: true).Enable();
    private void OnDisable() => InputAsset.FindActionMap("Player", throwIfNotFound: true).Disable();

    private void Update()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();

        // 카메라 기준 이동 방향 (수평 성분만)
        Vector3 forward = _cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = _cameraTransform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 move = forward * input.y + right * input.x;
        if (move.sqrMagnitude > 1f) move.Normalize();

        // 질주 (Player/Sprint 액션 — Shift, 게임패드 스틱 클릭 등 바인딩은 에셋에서 관리)
        float speed = _sprintAction.IsPressed() ? SprintSpeed : MoveSpeed;

        // 중력
        if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
        _verticalVelocity += Gravity * Time.deltaTime;

        _controller.Move((move * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);

        // 이동 방향으로 부드럽게 회전
        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, RotationSpeed * Time.deltaTime);
        }

        // 이동 속도 → 애니메이션 (Animator의 Speed 파라미터로 Idle/Walk/Run 전환)
        if (_animator != null)
        {
            Vector3 planarVelocity = _controller.velocity;
            planarVelocity.y = 0f;
            _animator.SetFloat(SpeedParam, planarVelocity.magnitude);

            // StarterAssets ThirdPerson 컨트롤러용 파라미터
            if (_hasGroundedParam) _animator.SetBool(GroundedParam, _controller.isGrounded);
            if (_hasMotionSpeedParam) _animator.SetFloat(MotionSpeedParam, 1f);   // 애니메이션 재생 배속
        }
    }
}
