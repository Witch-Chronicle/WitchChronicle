using UnityEngine;

/// <summary>
/// 전투 씬 전용 공간 위치 설정
/// </summary>
public class BattleScenePositioner : MonoBehaviour
{
    [Header("Battle Scene Position")]
    [SerializeField] private Vector3 _battleWorldPosition = new Vector3(0f, 1000f, 0f);

    /// <summary>
    /// 전투 씬 위치 초기화
    /// </summary>
    private void Awake()
    {
        transform.position = _battleWorldPosition;
    }
}