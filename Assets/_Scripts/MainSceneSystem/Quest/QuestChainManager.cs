using UnityEngine;

/// <summary>
/// 메인 스토리 진행 관리
/// </summary>
public class QuestChainManager : MonoBehaviour
{
    public static QuestChainManager Instance { get; private set; }


    [SerializeField]
    private QuestChainData _mainStory;


    private int _currentIndex;


    /// <summary>
    /// 초기화
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);

            return;
        }

        Instance = this;
    }


    /// <summary>
    /// 스토리 시작
    /// </summary>
    public void StartStory()
    {
        _currentIndex = 0;


        StartCurrentQuest();
    }


    /// <summary>
    /// 현재 메인 퀘스트 시작
    /// </summary>
    private void StartCurrentQuest()
    {
        if (_currentIndex >= _mainStory.mainQuests.Count)
        {
            Debug.Log("Story Complete");

            return;
        }


        QuestData quest = _mainStory.mainQuests[_currentIndex];


        QuestManager.Instance.StartQuest(quest.id);


        Debug.Log($"Main Quest Start : {quest.title}");
    }


    /// <summary>
    /// 다음 메인 퀘스트 이동
    /// </summary>
    public void NextQuest()
    {
        _currentIndex++;


        StartCurrentQuest();
    }


    /// <summary>
    /// 현재 메인 퀘스트 조회
    /// </summary>
    public QuestData GetCurrentQuest()
    {
        if (_currentIndex >= _mainStory.mainQuests.Count)
        {
            return null;
        }


        return _mainStory.mainQuests[_currentIndex];
    }
}