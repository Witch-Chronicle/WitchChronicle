using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [Header("현재 진행 중인 던전 데이터 (자동 연결됨)")]
    [SerializeField] private DungeonData _dungeonData;

    private bool _isCleared;

    private void Start()
    {
        if (DungeonManager.Instance != null)
        {
            _dungeonData = DungeonManager.Instance.CurrentDungeonData;
        }
    }

    /// <summary>
    /// 던전 클리어 처리 (인자 없이 호출 가능)
    /// </summary>
    public void ClearDungeon()
    {
        if (_isCleared)
        {
            return;
        }

        if (PersistentCharacterManager.Instance != null)
        {
            PersistentCharacterManager.Instance.RestoreActivePartyVitals();
        }

        // 혹시 Start 시점에 못 가져왔을 경우 대비
        if (_dungeonData == null && DungeonManager.Instance != null)
        {
            _dungeonData = DungeonManager.Instance.CurrentDungeonData;
        }

        if (_dungeonData == null)
        {
            Debug.LogWarning("[DungeonController] ClearDungeon 실패: 현재 DungeonData를 찾을 수 없습니다.");
            return;
        }

        _isCleared = true;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.AddProgress(QuestObjectiveType.ClearDungeon, _dungeonData.Id);
        }

        Debug.Log($"[DungeonController] Dungeon Clear : {_dungeonData.DungeonName} ({_dungeonData.Id})");
    }
}