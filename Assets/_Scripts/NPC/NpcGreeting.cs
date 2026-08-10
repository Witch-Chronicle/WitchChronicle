using UnityEngine;

/// <summary>
/// NPC 인사 연출.
///
/// - 상호작용을 시작하면 인사 애니메이션과 인사 음성을 재생한다.
/// - 대화가 끝나(모든 UI가 닫혀) 플레이어가 조작권을 되찾으면 작별 음성을 재생한다.
///
/// NPC 오브젝트에 NPC 스크립트와 같이 붙여서 사용.
/// 음성 클립은 NPCData(공용 SO)가 아니라 이 컴포넌트에 두어,
/// NPC마다 자유롭게 지정하고 다른 시스템에 영향을 주지 않도록 했다.
/// </summary>
[RequireComponent(typeof(NPC))]
public class NpcGreeting : MonoBehaviour
{
    [Header("애니메이션")]
    [Tooltip("비워두면 자식에서 자동으로 찾는다.")]
    [SerializeField] private Animator _animator;

    [Tooltip("인사 동작을 재생할 Trigger 파라미터 이름. 비우면 애니메이션을 건드리지 않는다.")]
    [SerializeField] private string _greetTrigger = "Greet";

    [Header("음성")]
    [Tooltip("말을 걸었을 때 재생할 대사.")]
    [SerializeField] private AudioClip _greetVoice;

    [Tooltip("대화를 마치고 창이 닫혔을 때 재생할 대사.")]
    [SerializeField] private AudioClip _farewellVoice;

    [Range(0f, 1f)]
    [SerializeField] private float _volume = 1f;

    private int _greetHash;
    private bool _hasTrigger;
    private bool _isTalking;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        _greetHash = Animator.StringToHash(_greetTrigger);
        _hasTrigger = HasTriggerParameter(_greetHash);
    }

    /// <summary>
    /// 상호작용 시작. NPC.Interact()에서 호출한다.
    /// </summary>
    public void OnInteracted()
    {
        // 대화 중에 다시 말을 거는 경우까지 중복 재생하지 않는다.
        if (_isTalking)
        {
            return;
        }

        _isTalking = true;

        if (_hasTrigger && _animator != null)
        {
            _animator.SetTrigger(_greetHash);
        }

        PlayVoice(_greetVoice);
    }

    private void Update()
    {
        if (_isTalking == false)
        {
            return;
        }

        // 대화창뿐 아니라 상점·강화 같은 후속 UI까지 모두 닫힌 시점을 대화 종료로 본다.
        if (IsInteractionOpen())
        {
            return;
        }

        _isTalking = false;

        PlayVoice(_farewellVoice);
    }

    /// <summary>
    /// 대사 한 줄을 재생한다.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    private void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlaySfxOneShot(clip, _volume);
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

    /// <summary>
    /// Animator에 해당 Trigger 파라미터가 실제로 있는지 확인.
    /// (없는 파라미터를 건드리면 경고가 쏟아진다.)
    /// </summary>
    /// <param name="hash">파라미터 해시</param>
    /// <returns>존재 여부</returns>
    private bool HasTriggerParameter(int hash)
    {
        if (_animator == null || string.IsNullOrEmpty(_greetTrigger))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == hash)
            {
                return true;
            }
        }

        return false;
    }
}
