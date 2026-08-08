using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC 하위 World Space Canvas에 부착하는 상호작용 안내 + 퀘스트 상태 표시 UI입니다.
///
/// [상호작용 안내] 부모 NPC의 NPCData에서 이름과 Prompt를 가져오고,
/// 플레이어가 표시 범위 안에 들어오면 CanvasGroup을 부드럽게 표시합니다.
///
/// [퀘스트 표시] 진행 중인 퀘스트의 대화/영입 대상 여부, 또는 NPC 자신이
/// 부여하는 퀘스트의 체인 상태(수락 가능 / 완료 가능)에 따라 단일 Image의
/// Sprite(!/?)와 Alpha(Main/Sub 구분)를 바꿉니다. 별도 CanvasGroup 없이
/// 상단 상호작용 CanvasGroup(거리 기반)을 그대로 상속받아 같은 범위에서만 보이며,
/// 퀘스트가 없는 상태(None)에서는 Image 오브젝트 자체를 비활성화합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class NPCWorldInteractionUI : MonoBehaviour
{
    private enum QuestIndicatorState
    {
        None,
        MainExclamation,
        MainQuestion,
        SubExclamation,
        SubQuestion
    }

    private enum QuestIconBounceType
    {
        None,
        LocalYYoyo,
        ScalePunch
    }

    [Header("Data Source")]
    [Tooltip("비워두면 부모 오브젝트에서 NPC를 자동으로 찾습니다.")]
    [SerializeField] private NPC _npc;

    [Header("UI References")]
    [Tooltip("NPCData.NpcName을 표시합니다.")]
    [SerializeField] private TMP_Text _roleText;
    [Tooltip("NPCData.Prompt를 표시합니다.")]
    [SerializeField] private TMP_Text _actionText;
    [Tooltip("비워두면 이 오브젝트의 CanvasGroup을 자동으로 사용합니다. (상호작용 안내 전용)")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Player")]
    [Tooltip("직접 연결하는 것이 가장 안전합니다. 비워두면 Player Tag로 자동 탐색합니다.")]
    [SerializeField] private Transform _player;
    [SerializeField] private string _playerTag = "Player";
    [Tooltip("플레이어가 나중에 생성되는 경우 재탐색하는 간격입니다.")]
    [SerializeField, Min(0.1f)] private float _playerSearchInterval = 0.5f;

    [Header("Visibility")]
    [Tooltip("이 거리 안에 플레이어가 들어오면 UI를 표시합니다.")]
    [SerializeField, Min(0f)] private float _visibleRange = 3f;
    [Tooltip("경계에서 UI가 깜빡이지 않도록 숨김 거리에 더하는 여유 거리입니다.")]
    [SerializeField, Min(0f)] private float _hideRangePadding = 0.25f;
    [Tooltip("0이면 즉시 표시하며, 값이 있으면 해당 시간 동안 Fade됩니다.")]
    [SerializeField, Min(0f)] private float _fadeDuration = 0.18f;
    [Tooltip("체크하면 Y 높이 차이를 무시하고 평면 거리만 계산합니다.")]
    [SerializeField] private bool _useHorizontalDistance = true;

    [Header("Billboard")]
    [Tooltip("MainCamera를 바라보도록 World Canvas를 회전시킵니다.")]
    [SerializeField] private bool _useBillboard = true;
    [Tooltip("직접 연결하지 않으면 Camera.main을 사용합니다.")]
    [SerializeField] private Camera _mainCamera;
    [Tooltip("체크하면 상하로 기울지 않고 Y축으로만 카메라를 바라봅니다.")]
    [SerializeField] private bool _lockYAxis;

    [Header("Quest Indicator")]
    [Tooltip("퀘스트 상태에 따라 Sprite와 Alpha가 바뀌는 단일 Image입니다. 표시 여부(거리)는 상단 _canvasGroup을 그대로 따릅니다.")]
    [SerializeField] private Image _questIconImage;
    [Tooltip("수락 가능 상태 (!)에 사용할 Sprite입니다.")]
    [SerializeField] private Sprite _exclamationSprite;
    [Tooltip("완료 가능 / 대화 대상 상태 (?)에 사용할 Sprite입니다.")]
    [SerializeField] private Sprite _questionSprite;
    [Tooltip("메인 퀘스트일 때 아이콘 Alpha 값입니다.")]
    [SerializeField, Range(0f, 1f)] private float _mainAlpha = 1f;
    [Tooltip("서브 퀘스트일 때 아이콘 Alpha 값입니다.")]
    [SerializeField, Range(0f, 1f)] private float _subAlpha = 0.6f;

    [Header("Quest Icon Bounce")]
    [SerializeField] private QuestIconBounceType _bounceType = QuestIconBounceType.LocalYYoyo;
    [SerializeField] private Ease _bounceEase = Ease.InOutSine;
    [Tooltip("LocalYYoyo: 왕복 이동 거리")]
    [SerializeField, Min(0f)] private float _bounceMoveDistance = 0.15f;
    [Tooltip("LocalYYoyo: 편도 이동 시간")]
    [SerializeField, Min(0.01f)] private float _bounceMoveDuration = 0.5f;
    [Tooltip("ScalePunch: 펀치 강도")]
    [SerializeField, Min(0f)] private float _bouncePunchScale = 0.25f;
    [Tooltip("ScalePunch: 한 번의 펀치 시간")]
    [SerializeField, Min(0.01f)] private float _bouncePunchDuration = 0.5f;
    [Tooltip("ScalePunch: 펀치 사이의 대기 시간")]
    [SerializeField, Min(0f)] private float _bouncePunchInterval = 0.2f;

    private bool _isInRange;
    private bool _suppressed;
    private float _nextPlayerSearchTime;

    private QuestIndicatorState _currentQuestState = QuestIndicatorState.None;
    private Tween _questBounceTween;
    private Vector3 _questIconOriginalLocalPos;
    private Vector3 _questIconOriginalLocalScale;
    private bool _questIconTransformCached;

    public bool IsInRange => _isInRange;
    public float VisibleRange => _visibleRange;

    private void Reset()
    {
        _npc = GetComponentInParent<NPC>();
        _canvasGroup = GetComponent<CanvasGroup>();
        AutoAssignTexts();
    }

    private void Awake()
    {
        ResolveReferences();
        RefreshData();
        SetCanvasAlphaImmediate(0f);
        CacheQuestIconOriginalTransforms();
        ApplyQuestState(DetermineQuestState());
    }

    private void OnEnable()
    {
        _isInRange = false;
        SetCanvasAlphaImmediate(0f);
        TryFindPlayer();

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        // 재활성화 시 즉시 최신 퀘스트 상태로 맞춥니다.
        ApplyQuestState(DetermineQuestState());
    }

    private void OnDisable()
    {
        KillQuestBounceTween();
    }

    private void OnDestroy()
    {
        KillQuestBounceTween();
    }

    private void Update()
    {
        if (_player == null && Time.unscaledTime >= _nextPlayerSearchTime)
        {
            TryFindPlayer();
        }

        UpdateRangeState();
        UpdateCanvasAlpha();

        RefreshQuestIndicator();
    }

    private void LateUpdate()
    {
        UpdateBillboard();
    }

    /// <summary>NPCData 값이 런타임에 바뀌었다면 호출하여 텍스트를 다시 채웁니다.</summary>
    public void RefreshData()
    {
        if (_npc == null)
        {
            _npc = GetComponentInParent<NPC>();
        }

        NPCData data = _npc != null ? _npc.Data : null;

        if (_roleText != null)
        {
            _roleText.text = data != null ? data.NpcName : string.Empty;
        }

        if (_actionText != null)
        {
            _actionText.text = data != null ? data.Prompt : string.Empty;
        }
    }

    /// <summary>퀘스트 진행 상태가 외부 이벤트로 바뀌었을 때 즉시 재평가하고 싶다면 호출합니다.</summary>
    public void RefreshQuestIndicator()
    {
        QuestIndicatorState newState = DetermineQuestState();

        if (newState == _currentQuestState)
        {
            return;
        }

        ApplyQuestState(newState);
    }

    /// <summary>플레이어 Transform을 외부에서 명시적으로 지정할 때 사용합니다.</summary>
    public void SetPlayer(Transform player)
    {
        _player = player;
        UpdateRangeState(forceEvaluate: true);
    }

    /// <summary>
    /// 대화·상점 UI가 열렸을 때 World Canvas를 임시로 숨길 수 있습니다.
    /// 닫힌 후 false를 전달하면 거리 조건에 따라 다시 표시됩니다.
    /// </summary>
    public void SetSuppressed(bool suppressed)
    {
        _suppressed = suppressed;
    }

    /// <summary>거리와 관계없이 즉시 숨깁니다. (상호작용 안내 UI 한정, 퀘스트 표시는 영향받지 않습니다)</summary>
    public void HideImmediate()
    {
        _suppressed = true;
        SetCanvasAlphaImmediate(0f);
    }

    private void ResolveReferences()
    {
        if (_npc == null)
        {
            _npc = GetComponentInParent<NPC>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        AutoAssignTexts();

        if (_npc == null)
        {
            Debug.LogWarning($"[{name}] 부모에서 NPC 컴포넌트를 찾지 못했습니다.", this);
        }

        if (_canvasGroup == null)
        {
            Debug.LogError($"[{name}] CanvasGroup이 필요합니다.", this);
        }
    }

    private void AutoAssignTexts()
    {
        // Hierarchy 이름이 일치할 때만 편의를 위해 자동 연결합니다.
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (_roleText == null && text.gameObject.name == "RoleText")
            {
                _roleText = text;
            }
            else if (_actionText == null && text.gameObject.name == "ActionText")
            {
                _actionText = text;
            }
        }
    }

    private void TryFindPlayer()
    {
        _nextPlayerSearchTime = Time.unscaledTime + _playerSearchInterval;

        if (_player != null || string.IsNullOrWhiteSpace(_playerTag))
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
    }

    private void UpdateRangeState(bool forceEvaluate = false)
    {
        if (_player == null || _npc == null)
        {
            _isInRange = false;
            return;
        }

        Vector3 delta = _player.position - _npc.transform.position;

        if (_useHorizontalDistance)
        {
            delta.y = 0f;
        }

        float sqrDistance = delta.sqrMagnitude;
        float showRange = _visibleRange;
        float hideRange = _visibleRange + _hideRangePadding;

        if (forceEvaluate)
        {
            _isInRange = sqrDistance <= showRange * showRange;
            return;
        }

        if (_isInRange)
        {
            if (sqrDistance > hideRange * hideRange)
            {
                _isInRange = false;
            }
        }
        else if (sqrDistance <= showRange * showRange)
        {
            _isInRange = true;
        }
    }

    private void UpdateCanvasAlpha()
    {
        if (_canvasGroup == null)
        {
            return;
        }

        float targetAlpha = _isInRange && _suppressed == false ? 1f : 0f;

        if (_fadeDuration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
        }
        else
        {
            float speed = 1f / _fadeDuration;
            _canvasGroup.alpha = Mathf.MoveTowards(
                _canvasGroup.alpha,
                targetAlpha,
                speed * Time.unscaledDeltaTime);
        }

        // 이 UI는 마우스로 클릭하지 않으므로 항상 입력을 막지 않습니다.
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void UpdateBillboard()
    {
        if (_useBillboard == false)
        {
            return;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;

            if (_mainCamera == null)
            {
                return;
            }
        }

        if (_lockYAxis)
        {
            // 카메라에서 Canvas로 향하는 평면 방향을 사용하면
            // 기본 World Canvas의 앞면이 카메라를 향하는 방향으로 정렬됩니다.
            Vector3 forward = transform.position - _mainCamera.transform.position;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }
        else
        {
            // 카메라 화면과 평행하게 맞춰 상하 시점 변화에서도 정면을 유지합니다.
            transform.rotation = _mainCamera.transform.rotation;
        }
    }

    private void SetCanvasAlphaImmediate(float alpha)
    {
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = alpha;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    // ------------------------------------------------------------
    // Quest Indicator
    // ------------------------------------------------------------

    private void CacheQuestIconOriginalTransforms()
    {
        if (_questIconImage == null || _questIconTransformCached)
        {
            return;
        }

        _questIconOriginalLocalPos = _questIconImage.rectTransform.localPosition;
        _questIconOriginalLocalScale = _questIconImage.rectTransform.localScale;
        _questIconTransformCached = true;
    }

    private QuestIndicatorState DetermineQuestState()
    {
        if (_npc == null || _npc.Data == null || QuestManager.Instance == null)
        {
            return QuestIndicatorState.None;
        }

        // 1. 진행 중인 퀘스트의 대화/영입 대상(TalkNPC / RecruitNPC)이면서
        //    아직 해당 objective를 완료하지 않은 경우 -> 물음표(?)
        List<QuestRuntime> runningList = QuestManager.Instance.GetRunningQuests();

        foreach (QuestRuntime running in runningList)
        {
            if (running.State != QuestState.Running)
            {
                continue;
            }

            for (int i = 0; i < running.Data.objectives.Count; i++)
            {
                QuestObjective obj = running.Data.objectives[i];

                if (obj.type != QuestObjectiveType.TalkNPC && obj.type != QuestObjectiveType.RecruitNPC)
                {
                    continue;
                }

                if (obj.targetID != _npc.Data.NpcId)
                {
                    continue;
                }

                // 이미 이 objective의 진행도를 채웠다면(완료) 더 이상 대상이 아님
                int progress = running.Progress.TryGetValue(i, out int value) ? value : 0;

                if (progress >= obj.requiredCount)
                {
                    continue;
                }

                bool isMainQuest = running.Data.type == QuestType.Main;
                return isMainQuest ? QuestIndicatorState.MainQuestion : QuestIndicatorState.SubQuestion;
            }
        }

        // 2. 영입 대상 NPC(RecruitQuestId가 설정됨)는 "퀘스트를 직접 주는 주체"가 아니므로
        //    1순위에서 매치되지 않았다면(아직 objective 대상이 아니거나 이미 완료됨) 표시하지 않음
        if (string.IsNullOrEmpty(_npc.Data.RecruitQuestId) == false)
        {
            return QuestIndicatorState.None;
        }

        // 3. 자신이 부여하는 퀘스트가 없는 일반 NPC는 끔
        if (string.IsNullOrEmpty(_npc.Data.QuestId))
        {
            return QuestIndicatorState.None;
        }

        // 4. 자신이 부여하는 퀘스트 상태 확인 (NextQuest 체인 포함)
        string currentQuestId = _npc.Data.QuestId;
        QuestRuntime questRuntime = QuestManager.Instance.GetQuest(currentQuestId);

        while (questRuntime != null && questRuntime.State == QuestState.Rewarded)
        {
            if (questRuntime.Data != null && questRuntime.Data.nextQuest != null)
            {
                string nextId = questRuntime.Data.nextQuest.id;
                QuestRuntime nextRuntime = QuestManager.Instance.GetQuest(nextId);

                if (nextRuntime != null)
                {
                    questRuntime = nextRuntime;
                    currentQuestId = nextId;
                }
                else
                {
                    questRuntime = null;
                    currentQuestId = nextId;
                    break;
                }
            }
            else
            {
                break;
            }
        }

        QuestData questData = questRuntime != null ? questRuntime.Data : QuestManager.Instance.GetQuestData(currentQuestId);

        if (questData == null)
        {
            return QuestIndicatorState.None;
        }

        bool isMain = questData.type == QuestType.Main;

        // A. 수락 가능 상태 -> 느낌표(!)
        if (questRuntime == null)
        {
            return isMain ? QuestIndicatorState.MainExclamation : QuestIndicatorState.SubExclamation;
        }

        // B. 완료 가능 상태 -> 물음표(?)
        if (questRuntime.State == QuestState.Completed)
        {
            return isMain ? QuestIndicatorState.MainQuestion : QuestIndicatorState.SubQuestion;
        }

        // C. 기타 (진행 중 / 보상 완료) -> 끔
        return QuestIndicatorState.None;
    }

    /// <summary>상태에 대응하는 Sprite를 반환합니다. null이면 표시하지 않습니다.</summary>
    private Sprite ResolveQuestSprite(QuestIndicatorState state)
    {
        switch (state)
        {
            case QuestIndicatorState.MainExclamation:
            case QuestIndicatorState.SubExclamation:
                return _exclamationSprite;
            case QuestIndicatorState.MainQuestion:
            case QuestIndicatorState.SubQuestion:
                return _questionSprite;
            default:
                return null;
        }
    }

    private bool IsMainQuestState(QuestIndicatorState state)
    {
        return state == QuestIndicatorState.MainExclamation || state == QuestIndicatorState.MainQuestion;
    }

    private void ApplyQuestState(QuestIndicatorState state)
    {
        _currentQuestState = state;

        KillQuestBounceTween();

        Sprite targetSprite = ResolveQuestSprite(state);

        if (_questIconImage == null)
        {
            return;
        }

        if (targetSprite == null || state == QuestIndicatorState.None)
        {
            _questIconImage.gameObject.SetActive(false);
            return;
        }

        _questIconImage.gameObject.SetActive(true);
        _questIconImage.sprite = targetSprite;

        float targetAlpha = IsMainQuestState(state) ? _mainAlpha : _subAlpha;
        Color color = _questIconImage.color;
        color.a = targetAlpha;
        _questIconImage.color = color;

        StartQuestBounceTween();
    }

    private void StartQuestBounceTween()
    {
        if (_questIconImage == null || _bounceType == QuestIconBounceType.None)
        {
            return;
        }

        CacheQuestIconOriginalTransforms();

        RectTransform iconTransform = _questIconImage.rectTransform;

        switch (_bounceType)
        {
            case QuestIconBounceType.LocalYYoyo:
                {
                    iconTransform.localPosition = _questIconOriginalLocalPos;

                    _questBounceTween = iconTransform
                        .DOLocalMoveY(_questIconOriginalLocalPos.y + _bounceMoveDistance, _bounceMoveDuration)
                        .SetEase(_bounceEase)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetLink(_questIconImage.gameObject);
                    break;
                }
            case QuestIconBounceType.ScalePunch:
                {
                    iconTransform.localScale = _questIconOriginalLocalScale;

                    Sequence sequence = DOTween.Sequence();
                    sequence.Append(iconTransform.DOPunchScale(Vector3.one * _bouncePunchScale, _bouncePunchDuration, 1, 0.5f));
                    sequence.AppendInterval(_bouncePunchInterval);
                    sequence.SetLoops(-1);
                    sequence.SetLink(_questIconImage.gameObject);

                    _questBounceTween = sequence;
                    break;
                }
        }
    }

    private void KillQuestBounceTween()
    {
        if (_questBounceTween != null && _questBounceTween.IsActive())
        {
            _questBounceTween.Kill();
        }

        _questBounceTween = null;

        if (_questIconImage != null && _questIconTransformCached)
        {
            _questIconImage.rectTransform.localPosition = _questIconOriginalLocalPos;
            _questIconImage.rectTransform.localScale = _questIconOriginalLocalScale;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _visibleRange = Mathf.Max(0f, _visibleRange);
        _hideRangePadding = Mathf.Max(0f, _hideRangePadding);
        _fadeDuration = Mathf.Max(0f, _fadeDuration);
        _playerSearchInterval = Mathf.Max(0.1f, _playerSearchInterval);

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_npc == null)
        {
            _npc = GetComponentInParent<NPC>();
        }

        AutoAssignTexts();

        if (Application.isPlaying)
        {
            RefreshData();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = _npc != null ? _npc.transform : transform;
        Gizmos.color = new Color(0.88f, 0.81f, 0.60f, 0.8f);
        Gizmos.DrawWireSphere(origin.position, _visibleRange);

        if (_hideRangePadding > 0f)
        {
            Gizmos.color = new Color(0.55f, 0.55f, 0.60f, 0.45f);
            Gizmos.DrawWireSphere(origin.position, _visibleRange + _hideRangePadding);
        }
    }
#endif
}