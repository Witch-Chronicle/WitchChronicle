using System.Collections;
using UnityEngine;

/// <summary>
/// 별자리 공격 전체 시퀀스 관리
/// 카메라, 시간 제어, 입력, 방어막, 투사체 흐름 연결
/// </summary>
public class ConstellationPathAttackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ConstellationPathBattleManager _pathBattleManager;
    [SerializeField] private ConstellationPathTimeController _timeController;

    private readonly ConstellationPathShieldState _shieldState = new ConstellationPathShieldState();

    private Coroutine _attackRoutine;

    public bool IsRunning => _attackRoutine != null;
    public ConstellationPathShieldState ShieldState => _shieldState;

    /// <summary>
    /// 내부 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_pathBattleManager == null)
        {
            _pathBattleManager = GetComponent<ConstellationPathBattleManager>();
        }

        if (_timeController == null)
        {
            _timeController = GetComponent<ConstellationPathTimeController>();
        }
    }

    /// <summary>
    /// 비활성화 시 별자리 공격 중단
    /// </summary>
    private void OnDisable()
    {
        StopAttack();
    }

    /// <summary>
    /// 별자리 공격 강제 중단
    /// </summary>
    public void StopAttack()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        _pathBattleManager?.StopConstellationPath();
        _timeController?.RestoreImmediate();
        _shieldState.Clear();
    }
}