using UnityEngine;

/// <summary>
/// 보스 전투 승리 후 던전으로 복귀할 때 보스가 있던 자리에 출구 포탈을 스폰합니다.
/// </summary>
public class BossPortalSpawner : MonoBehaviour
{
    private GameObject _exitPortalPrefab;
    private Vector3 _spawnPosition;

    public void Setup(GameObject exitPortalPrefab, Vector3 spawnPosition)
    {
        _exitPortalPrefab = exitPortalPrefab;
        _spawnPosition = spawnPosition;
    }

    /// <summary>
    /// 전투 승리 후 BattleEncounterContext.DestroyEncounter()에 의해 호출됨
    /// </summary>
    public void SpawnPortal()
    {
        if (_exitPortalPrefab == null)
        {
            Debug.LogWarning("[BossPortalSpawner] 출구 포탈 프리팹이 설정되지 않았습니다.");
            return;
        }

        Instantiate(_exitPortalPrefab, _spawnPosition, Quaternion.identity);
        Debug.Log($"[BossPortalSpawner] 보스 처치 완료! 위치({_spawnPosition})에 출구 포탈 생성!");
    }
}