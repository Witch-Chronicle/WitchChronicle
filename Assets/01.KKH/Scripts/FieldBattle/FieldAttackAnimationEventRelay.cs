using UnityEngine;

/// <summary>
/// 필드 공격 애니메이션 이벤트 중계
/// </summary>
public class FieldAttackAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private FieldAttackController _attackController;

    /// <summary>
    /// 공격 컨트롤러 참조 초기화
    /// </summary>
    private void Awake()
    {
        if (_attackController == null)
        {
            _attackController =
                GetComponentInParent<FieldAttackController>();
        }
    }

    /// <summary>
    /// 공격 명중 시점 전달
    /// </summary>
    public void OnFieldAttackImpact()
    {
        if (_attackController == null)
        {
            return;
        }

        _attackController.NotifyAttackImpact();
    }

    /// <summary>
    /// 공격 애니메이션 종료 전달
    /// </summary>
    public void OnFieldAttackFinished()
    {
        if (_attackController == null)
        {
            return;
        }

        _attackController.NotifyAttackFinished();
    }
}