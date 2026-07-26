using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [SerializeField]
    private DungeonData _dungeonData;


    private bool _isCleared;


    public void ClearDungeon()
    {
        if (_isCleared)
        {
            return;
        }

        _isCleared = true;

        QuestManager.Instance.AddProgress(QuestObjectiveType.ClearDungeon, _dungeonData.Id);

        Debug.Log($"Dungeon Clear : {_dungeonData.Id}");
    }
}