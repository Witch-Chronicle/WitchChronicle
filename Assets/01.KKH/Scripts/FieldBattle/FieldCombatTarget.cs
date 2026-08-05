using UnityEngine;

/// <summary>
/// 필드 전투 대상 정보
/// </summary>
public class FieldCombatTarget : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("록온 및 번개 이펙트가 도착할 위치")]
    [SerializeField] private Transform _hitPoint;

    private BattleEncounter _battleEncounter;

    public Transform HitPoint =>
        _hitPoint != null
            ? _hitPoint
            : transform;

    public BattleEncounter BattleEncounter =>
        _battleEncounter;

    public bool IsAvailable =>
        isActiveAndEnabled &&
        gameObject.activeInHierarchy &&
        _battleEncounter != null;

    /// <summary>
    /// 조우 정보 연결
    /// </summary>
    private void Awake()
    {
        _battleEncounter =
            GetComponentInParent<BattleEncounter>();
    }

    /// <summary>
    /// 록온 위치 반환
    /// </summary>
    /// <returns>록온 월드 위치</returns>
    public Vector3 GetAimPosition()
    {
        return HitPoint.position;
    }
}