using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// TargetList 전담. 슬라이드 애니메이션, Content 채우기, 대상 선택 시 액션 제출까지 담당.
/// - AtkController가 OpenForAttack() 호출 -> 살아있는 적 전체 표시 -> 취소 시 Btns로 복귀
/// - SkillListController가 OpenForSkill(skillData) 호출 -> 스킬의 TargetType에 맞는 대상만 표시
///   (BattleUIContext.GetSelectableSkillTargets 경유) -> 취소 시 SkillList로 복귀
/// - 각 대상 카드에 이번 라운드 전체 턴 순서 상 몇 번째인지(OrderTxt)도 같이 표시.
/// * 대상 클릭 시 BattleUIContext.SubmitAction()으로 공격/스킬 요청 제출 후 항상 Btns로 복귀
/// </summary>
public class TargetListController : MonoBehaviour
{
    private enum ReturnMode { None, Attack, Skill }

    [Header("Panel")]
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private TMP_Text _headerTxt;
    [SerializeField] private Button _closeBtn;
    [SerializeField] private float _duration = 0.25f;
    [SerializeField] private Ease _openEase = Ease.OutQuad;
    [SerializeField] private Ease _closeEase = Ease.InQuad;

    [Header("Content")]
    [SerializeField] private Transform _contentParent;
    [SerializeField] private BattleTargetView _targetPrefab;

    [Header("Action Bar")]
    [SerializeField] private BattleActionBarController _actionBar;

    [Header("Skill List (취소 시 되돌아갈 대상)")]
    [SerializeField] private SkillListController _skillListController;

    [Header("Camera")]
    [SerializeField] private BattleCameraDirector _cameraDirector;

    private readonly List<BattleTargetView> _spawnedTargets = new List<BattleTargetView>();
    private ReturnMode _returnMode = ReturnMode.None;
    private SkillData _pendingSkill;

    public bool IsOpen { get; private set; }

    private float _visiblePosX;
    private float _hiddenPosX;
    private bool _isInitialized;

    private void Awake()
    {
        if (_closeBtn != null) _closeBtn.onClick.AddListener(HandleCloseClicked);
    }

    private void Start()
    {
        EnsureInitialized();

        IsOpen = false;
        SetPosXImmediate(_hiddenPosX);
        gameObject.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (_isInitialized || _rectTransform == null) return;

        _visiblePosX = _rectTransform.anchoredPosition.x;
        _hiddenPosX = _visiblePosX + _rectTransform.rect.width;
        _isInitialized = true;
    }

    /// <summary>
    /// AtkBtn에서 호출. 살아있는 적 전체를 대상으로 표시. 취소하면 Btns로 복귀.
    /// </summary>
    public void OpenForAttack()
    {
        _returnMode = ReturnMode.Attack;
        _pendingSkill = null;

        PopulateEnemyTargets();
        Open("공격 대상");
    }

    /// <summary>
    /// SkillListController에서 스킬 선택 후 호출. 스킬의 TargetType에 맞는 대상만 채움.
    /// 취소하면 SkillList로 복귀.
    /// </summary>
    public void OpenForSkill(SkillData skillData)
    {
        _returnMode = ReturnMode.Skill;
        _pendingSkill = skillData;

        PopulateSkillTargets(skillData);
        Open("스킬 대상");
    }

    private void Open(string headerText)
    {
        if (_rectTransform == null) return;

        EnsureInitialized();

        if (_headerTxt != null) _headerTxt.text = headerText;

        IsOpen = true;
        gameObject.SetActive(true);
        _rectTransform.DOKill();
        SetPosXImmediate(_hiddenPosX);
        _rectTransform.DOAnchorPosX(_visiblePosX, _duration).SetEase(_openEase);

        if (_actionBar != null) _actionBar.Hide();
    }

    private void HandleCloseClicked()
    {
        SlideOut();

        BattleUnit actor = BattleUIContext.Instance != null
            ? BattleUIContext.Instance.CurrentUnit
            : null;

        if (_returnMode == ReturnMode.Attack)
        {
            if (_cameraDirector != null && actor != null)
            {
                _cameraDirector.PlayPlayerBackView(
                    actor,
                    () =>
                    {
                        if (_actionBar != null)
                        {
                            _actionBar.Show();
                        }
                    });
            }
            else
            {
                if (_actionBar != null)
                {
                    _actionBar.Show();
                }
            }
        }
        else if (_returnMode == ReturnMode.Skill)
        {
            if (_skillListController != null)
            {
                _skillListController.Reopen();
            }
        }

        _returnMode = ReturnMode.None;
        _pendingSkill = null;
    }

    private void HandleTargetSelected(BattleUnit target)
    {
        if (target == null || BattleUIContext.Instance == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;

        if (actor == null) return;

        BattleActionRequest request = _returnMode == ReturnMode.Skill && _pendingSkill != null
            ? BattleActionRequest.CreateSkill(actor, _pendingSkill, target)
            : BattleActionRequest.CreateAttack(actor, target);

        BattleUIContext.Instance.SubmitAction(request);

        SlideOut();

        _returnMode = ReturnMode.None;
        _pendingSkill = null;

        if (_cameraDirector == null)
        {
            SubmitSelectedAction(request);
            return;
        }

        _cameraDirector.PlayPlayerBackView(
            actor,
            () => SubmitSelectedAction(request));
    }

    private void SlideOut()
    {
        if (_rectTransform == null) return;

        IsOpen = false;
        _rectTransform.DOKill();

        _rectTransform.DOAnchorPosX(_hiddenPosX, _duration).SetEase(_closeEase)
            .OnComplete(() => gameObject.SetActive(false));
    }

    /// <summary>
    /// 지금 턴 유닛 기준 생존한 적 전체를 Content에 채움 (기본 공격용).
    /// </summary>
    private void PopulateEnemyTargets()
    {
        ClearSpawnedTargets();

        if (BattleUIContext.Instance == null || _contentParent == null || _targetPrefab == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;
        if (actor == null) return;

        List<BattleUnit> opponents = new List<BattleUnit>();
        BattleUIContext.Instance.GetAliveOpponents(actor, opponents);

        SpawnTargetViews(opponents);
    }

    /// <summary>
    /// 스킬의 TargetType에 맞는 대상만 Content에 채움 (팀원 쪽 GetSelectableSkillTargets 경유).
    /// </summary>
    private void PopulateSkillTargets(SkillData skillData)
    {
        ClearSpawnedTargets();

        if (BattleUIContext.Instance == null || _contentParent == null || _targetPrefab == null) return;

        BattleUnit actor = BattleUIContext.Instance.CurrentUnit;
        if (actor == null || skillData == null) return;

        List<BattleUnit> candidates = new List<BattleUnit>();
        BattleUIContext.Instance.GetSelectableSkillTargets(actor, skillData, candidates);

        SpawnTargetViews(candidates);
    }

    /// <summary>
    /// 대상 목록을 채우면서, 이번 라운드 전체 턴 순서(아군+적)에서 각자 몇 번째인지도 같이 계산해서 넘김.
    /// </summary>
    private void SpawnTargetViews(List<BattleUnit> units)
    {
        Dictionary<BattleUnit, int> orderLookup = BuildTurnOrderLookup();

        foreach (var unit in units)
        {
            BattleTargetView view = Instantiate(_targetPrefab, _contentParent);

            int roundOrderNumber = orderLookup.TryGetValue(unit, out var order) ? order : 0;
            view.Bind(unit, roundOrderNumber, HandleTargetSelected);

            _spawnedTargets.Add(view);
        }
    }

    /// <summary>
    /// 이번 라운드 전체 턴 순서를 조회해서 유닛 -> 순번(1-based) 딕셔너리로 변환.
    /// </summary>
    private Dictionary<BattleUnit, int> BuildTurnOrderLookup()
    {
        var lookup = new Dictionary<BattleUnit, int>();

        if (BattleUIContext.Instance == null) return lookup;

        List<BattleUnit> fullOrder = new List<BattleUnit>();
        BattleUIContext.Instance.GetCurrentTurnOrder(fullOrder, true);

        for (int i = 0; i < fullOrder.Count; i++)
        {
            if (fullOrder[i] != null)
            {
                lookup[fullOrder[i]] = i + 1;
            }
        }

        return lookup;
    }

    private void ClearSpawnedTargets()
    {
        foreach (var view in _spawnedTargets)
        {
            if (view != null) Destroy(view.gameObject);
        }
        _spawnedTargets.Clear();
    }

    private void SetPosXImmediate(float posX)
    {
        _rectTransform.anchoredPosition = new Vector2(posX, _rectTransform.anchoredPosition.y);
    }

    /// <summary>
    /// 선택 행동 제출
    /// </summary>
    /// <param name="request">행동 요청</param>
    private void SubmitSelectedAction(BattleActionRequest request)
    {
        if (request == null || BattleUIContext.Instance == null)
        {
            return;
        }

        BattleUIContext.Instance.SubmitAction(request);
    }
}