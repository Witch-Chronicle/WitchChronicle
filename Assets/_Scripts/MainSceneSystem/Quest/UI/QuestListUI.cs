using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 현재 퀘스트 목록 UI 관리
/// 진행 중인 퀘스트 표시 및 목록 갱신 처리
/// Tab 키로 우측 슬라이드 아웃/인 토글 (UITestInputReader에서 호출)
/// </summary>
public class QuestListUI : MonoBehaviour
{
    public static QuestListUI Instance { get; private set; }

    [SerializeField] private GameObject _panel;

    [Header("Quest List")]
    [SerializeField] private Transform _content;
    [SerializeField] private QuestItem _questItemPrefab;

    [Header("Slide Animation")]
    [SerializeField] private RectTransform _panelRect; // _panel의 RectTransform
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private Ease _slideEase = Ease.OutQuad;

    private float _visiblePosX;
    private float _hiddenPosX;
    private bool _isInitialized;
    private bool _isOpen = true; // 기본 상태: 보임

    public bool IsOpen => _isOpen;

    /// <summary>
    /// 싱글톤 생성 및 중복 객체 제거
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

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        if (_panelRect == null && _panel != null)
        {
            _panelRect = _panel.GetComponent<RectTransform>();
        }

        if (_panelRect != null)
        {
            _visiblePosX = _panelRect.anchoredPosition.x;
            _hiddenPosX = _visiblePosX + _panelRect.rect.width;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// 현재 진행 퀘스트 목록 갱신
    /// </summary>
    public void Refresh()
    {
        Clear();

        List<QuestRuntime> quests = QuestManager.Instance.GetRunningQuests();

        foreach (QuestRuntime quest in quests)
        {
            if (quest.State == QuestState.Rewarded)
            {
                continue;
            }

            CreateItem(quest);
        }
    }

    private void CreateItem(QuestRuntime runtime)
    {
        QuestItem item = Instantiate(_questItemPrefab, _content);
        item.Setup(runtime);
    }

    private void Clear()
    {
        foreach (Transform child in _content)
        {
            Destroy(child.gameObject);
        }
    }

    public void Close()
    {
        _panel.SetActive(false);
    }

    public void Open()
    {
        _panel.SetActive(true);
    }

    /// <summary>
    /// Tab 키 토글: 우측으로 슬라이드 아웃 <-> 제자리로 슬라이드 인.
    /// SetActive를 쓰지 않고 위치만 이동시켜서 애니메이션이 끊기지 않게 함.
    /// </summary>
    public void ToggleSlide()
    {
        EnsureInitialized();

        if (_panelRect == null) return;

        _isOpen = !_isOpen;

        float targetX = _isOpen ? _visiblePosX : _hiddenPosX;

        _panelRect.DOKill();
        _panelRect.DOAnchorPosX(targetX, _slideDuration).SetEase(_slideEase);
    }
}