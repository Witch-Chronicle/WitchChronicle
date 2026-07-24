using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 스킬 리스트 패널 전담. 슬라이드 애니메이션, Btns 숨김/표시까지 자체 처리.
/// - SkillBtn 클릭 시 우측에서 슬라이드 인 + Btns 숨김 + 현재 턴 유닛의 스킬 목록 채움
/// - CloseBtn 클릭 시 슬라이드 아웃 + Btns 표시
/// - 스킬 클릭 시: 슬라이드 아웃(Btns는 계속 숨김 유지) 후 BattleTargetCycler.EnterSkillMode() 호출.
///   단일/전체/자신 대상 모두 BattleTargetCycler가 아웃라인 + 확인/취소 흐름을 담당.
/// - BattleTargetCycler에서 취소하면 Reopen()으로 다시 슬라이드 인
/// </summary>
public class SkillListController : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Button _skillBtn;

    [Header("Panel")]
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Button _closeBtn;
    [SerializeField] private float _duration = 0.25f;
    [SerializeField] private Ease _openEase = Ease.OutQuad;
    [SerializeField] private Ease _closeEase = Ease.InQuad;

    [Header("Content")]
    [SerializeField] private Transform _contentParent;
    [SerializeField] private BattleSkillView _skillPrefab;

    [Header("Action Bar (열고 닫힐 때 같이 반응)")]
    [SerializeField] private BattleActionBarController _actionBar;

    [Header("Target Cycler (스킬 선택 시 넘어갈 대상)")]
    [SerializeField] private BattleTargetCycler _targetCycler;

    [Header("Camera")]
    [SerializeField] private BattleCameraDirector _cameraDirector;

    private readonly List<BattleSkillView> _spawnedSkills = new List<BattleSkillView>();

    public bool IsOpen { get; private set; }

    private float _visiblePosX;
    private float _hiddenPosX;
    private bool _isInitialized;

    private void Awake()
    {
        if (_skillBtn != null) _skillBtn.onClick.AddListener(Open);
        if (_closeBtn != null) _closeBtn.onClick.AddListener(Close);
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
    /// 스킬 리스트 열기
    /// </summary>
    public void Open()
    {
        BattleUnit currentUnit = BattleUIContext.Instance != null
            ? BattleUIContext.Instance.CurrentUnit
            : null;

        if (_cameraDirector == null || currentUnit == null)
        {
            OpenPanel();
            return;
        }

        _cameraDirector.PlaySkillLowAngle(
            currentUnit,
            OpenPanel);
    }

    /// <summary>
    /// SkillBtn 클릭 또는 BattleTargetCycler에서 취소하고 돌아올 때 호출.
    /// </summary>
    public void OpenPanel()
    {
        if (_rectTransform == null) return;

        EnsureInitialized();
        RefreshSkillList();

        IsOpen = true;
        gameObject.SetActive(true);
        _rectTransform.DOKill();
        SetPosXImmediate(_hiddenPosX);

        _rectTransform.DOAnchorPosX(_visiblePosX, _duration).SetEase(_openEase);

        if (_actionBar != null) _actionBar.Hide();
    }

    /// <summary>
    /// BattleTargetCycler에서 취소하고 돌아올 때 호출. Open()과 동일하지만 이름으로 의도를 명확히 함.
    /// </summary>
    public void Reopen()
    {
        Open();
    }

    /// <summary>
    /// 스킬 리스트 닫기
    /// </summary>
    private void Close()
    {
        if (_rectTransform == null)
        {
            return;
        }

        IsOpen = false;
        _rectTransform.DOKill();

        _rectTransform.DOAnchorPosX(_hiddenPosX, _duration)
            .SetEase(_closeEase)
            .OnComplete(() => gameObject.SetActive(false));

        BattleUnit currentUnit = BattleUIContext.Instance != null
            ? BattleUIContext.Instance.CurrentUnit
            : null;

        if (_cameraDirector != null && currentUnit != null)
        {
            _cameraDirector.PlayPlayerBackView(
                currentUnit,
                () =>
                {
                    if (_actionBar != null)
                    {
                        _actionBar.Show();
                    }
                });

            return;
        }

        if (_actionBar != null)
        {
            _actionBar.Show();
        }
    }

    /// <summary>
    /// 현재 턴 유닛의 스킬 목록을 Content에 채움. MP 부족한 스킬은 클릭 불가로 표시.
    /// </summary>
    private void RefreshSkillList()
    {
        ClearSpawnedSkills();

        if (_contentParent == null || _skillPrefab == null) return;
        if (BattleUIContext.Instance == null || BattleUIContext.Instance.CurrentUnit == null) return;

        BattleUnit currentUnit = BattleUIContext.Instance.CurrentUnit;

        foreach (var skillData in currentUnit.SkillList)
        {
            if (skillData == null) continue;

            bool canUse = currentUnit.CanUseSkill(skillData);

            BattleSkillView view = Instantiate(_skillPrefab, _contentParent);
            view.Bind(skillData, canUse, HandleSkillSelected);
            _spawnedSkills.Add(view);
        }
    }

    private void ClearSpawnedSkills()
    {
        foreach (var view in _spawnedSkills)
        {
            if (view != null) Destroy(view.gameObject);
        }
        _spawnedSkills.Clear();
    }

    /// <summary>
    /// 스킬 클릭 처리. 자기 자신은 슬라이드 아웃만 하고(Btns는 안 보여줌)
    /// BattleTargetCycler로 넘겨서 아웃라인 + 확인/취소 흐름을 진행.
    /// </summary>
    private void HandleSkillSelected(SkillData skillData)
    {
        if (skillData == null || BattleUIContext.Instance == null || _targetCycler == null) return;

        SlideOutForTargeting();

        BattleUnit currentUnit = BattleUIContext.Instance.CurrentUnit;

        if (_cameraDirector == null || currentUnit == null)
        {
            _targetCycler.EnterSkillMode(skillData);
            return;
        }

        _cameraDirector.PlayTargetOverview(
            currentUnit,
            () => _targetCycler.EnterSkillMode(skillData));
    }

    private void SlideOutForTargeting()
    {
        if (_rectTransform == null) return;

        IsOpen = false;
        _rectTransform.DOKill();

        _rectTransform.DOAnchorPosX(_hiddenPosX, _duration).SetEase(_closeEase)
            .OnComplete(() => gameObject.SetActive(false));
        // Btns는 계속 숨김 유지 (Show 호출 안 함)
    }

    private void SetPosXImmediate(float posX)
    {
        _rectTransform.anchoredPosition = new Vector2(posX, _rectTransform.anchoredPosition.y);
    }
}