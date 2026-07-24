using UnityEngine;

/// <summary>
/// 전투 씬에서 BattleUnit을 시각적으로 표현하기 위함
/// 실제 전투 데이터는 BattleUnit이 가지고 있고, 이 클래스는 위치, 회전, 표시 오브젝트 역할을 담당
/// </summary>
public class BattleActorView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _visualRoot;

    private BattleUnit _battleUnit;

    public BattleUnit BattleUnit => _battleUnit;
    public bool HasUnit => _battleUnit != null;

    /// <summary>
    /// BattleUnit 데이터를 이 View에 연결
    /// </summary>
    /// <param name="battleUnit">표현할 전투 유닛</param>
    public void Bind(BattleUnit battleUnit)
    {
        _battleUnit = battleUnit;

        if (_battleUnit != null)
        {
            name = $"{_battleUnit.TeamType}_{_battleUnit.UnitName}_View";
        }
    }

    /// <summary>
    /// Actor View의 배치 위치와 회전을 설정
    /// </summary>
    /// <param name="position">배치할 월드 위치</param>
    /// <param name="rotation">적용할 월드 회전</param>
    public void SetFormationPose(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// 현재 연결된 BattleUnit 정보를 해제
    /// </summary>
    public void Clear()
    {
        _battleUnit = null;
    }
}