using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 커맨드 UI 제어
/// </summary>
public class BattleCommandUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleCycleController _battleCycleController;

    [Header("Command UI")]
    [SerializeField] private GameObject _commandRoot;
    [SerializeField] private Button _attackButton;
    [SerializeField] private Button _skillButton;

    [Header("Target UI")]
    [SerializeField] private GameObject _targetRoot;
    [SerializeField] private Transform _targetButtonParent;
    [SerializeField] private BattleTargetButtonUI _targetButtonPrefab;

    [Header("Skill UI")]
    [SerializeField] private GameObject _skillRoot;
    [SerializeField] private Transform _skillButtonParent;
    [SerializeField] private BattleSkillButtonUI _skillButtonPrefab;

    private readonly List<BattleUnit> _targetCandidates = new List<BattleUnit>();
    private readonly List<BattleTargetButtonUI> _spawnedTargetButtons = new List<BattleTargetButtonUI>();
    private readonly List<BattleSkillButtonUI> _spawnedSkillButtons = new List<BattleSkillButtonUI>();

    private BattleUnit _currentUnit;
    private SkillData _selectedSkill;

    /// <summary>
    /// 참조 자동 연결 및 버튼 이벤트 등록
    /// </summary>
    private void Awake()
    {
        if (_battleCycleController == null)
        {
            _battleCycleController = FindFirstObjectByType<BattleCycleController>();
        }

        if (_attackButton != null)
        {
            _attackButton.onClick.AddListener(OnClickAttack);
        }

        if (_skillButton != null)
        {
            _skillButton.onClick.AddListener(OnClickSkill);
        }

        Hide();
    }

    /// <summary>
    /// 전투 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (_battleCycleController == null)
        {
            return;
        }

        _battleCycleController.OnTurnStarted += HandleTurnStarted;
        _battleCycleController.OnTurnEnded += HandleTurnEnded;
        _battleCycleController.OnBattleEnded += HandleBattleEnded;
    }

    /// <summary>
    /// 전투 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_battleCycleController == null)
        {
            return;
        }

        _battleCycleController.OnTurnStarted -= HandleTurnStarted;
        _battleCycleController.OnTurnEnded -= HandleTurnEnded;
        _battleCycleController.OnBattleEnded -= HandleBattleEnded;
    }

    /// <summary>
    /// 턴 시작 처리
    /// </summary>
    /// <param name="unit">턴 유닛</param>
    /// <param name="actionCount">행동 가능 횟수</param>
    private void HandleTurnStarted(BattleUnit unit, int actionCount)
    {
        _currentUnit = unit;
        _selectedSkill = null;

        if (_currentUnit == null)
        {
            Hide();
            return;
        }

        if (_currentUnit.TeamType == BattleTeamType.Player)
        {
            ShowCommandRoot();
            HideTargetRoot();
            HideSkillRoot();
            return;
        }

        Hide();
    }

    /// <summary>
    /// 턴 종료 처리
    /// </summary>
    /// <param name="unit">턴 종료 유닛</param>
    private void HandleTurnEnded(BattleUnit unit)
    {
        if (unit == _currentUnit)
        {
            _currentUnit = null;
        }

        _selectedSkill = null;
        Hide();
    }

    /// <summary>
    /// 전투 종료 처리
    /// </summary>
    /// <param name="winner">승리 팀</param>
    private void HandleBattleEnded(BattleTeamType winner)
    {
        _currentUnit = null;
        _selectedSkill = null;
        Hide();
    }

    /// <summary>
    /// 공격 버튼 클릭 처리
    /// </summary>
    private void OnClickAttack()
    {
        if (_battleCycleController == null)
        {
            return;
        }

        if (_currentUnit == null || _currentUnit.IsAlive == false)
        {
            return;
        }

        _selectedSkill = null;

        HideSkillRoot();
        OpenTargetSelectionForAttack();
    }

    /// <summary>
    /// 스킬 버튼 클릭 처리
    /// </summary>
    private void OnClickSkill()
    {
        if (_currentUnit == null || _currentUnit.IsAlive == false)
        {
            return;
        }

        HideTargetRoot();
        OpenSkillSelection();
    }

    /// <summary>
    /// 공격 대상 선택 UI 열기
    /// </summary>
    private void OpenTargetSelectionForAttack()
    {
        ClearTargetButtons();

        _battleCycleController.GetAliveOpponents(_currentUnit, _targetCandidates);

        if (_targetCandidates.Count == 0)
        {
            Debug.LogWarning("공격 가능한 대상 없음");
            return;
        }

        ShowTargetRoot();

        for (int i = 0; i < _targetCandidates.Count; i++)
        {
            BattleUnit target = _targetCandidates[i];

            BattleTargetButtonUI targetButton = Instantiate(
                _targetButtonPrefab,
                _targetButtonParent);

            targetButton.Initialize(target, HandleClickAttackTarget);

            _spawnedTargetButtons.Add(targetButton);
        }
    }

    /// <summary>
    /// 공격 대상 클릭 처리
    /// </summary>
    /// <param name="target">선택 대상</param>
    private void HandleClickAttackTarget(BattleUnit target)
    {
        if (_currentUnit == null)
        {
            return;
        }

        if (target == null || target.IsAlive == false)
        {
            return;
        }

        BattleActionRequest actionRequest =
            BattleActionRequest.CreateAttack(_currentUnit, target);

        _battleCycleController.SubmitAction(actionRequest);

        Hide();
    }

    /// <summary>
    /// 스킬 선택 UI 열기
    /// </summary>
    private void OpenSkillSelection()
    {
        ClearSkillButtons();

        if (_currentUnit == null)
        {
            return;
        }

        IReadOnlyList<SkillData> skillList = _currentUnit.SkillList;

        if (skillList == null || skillList.Count == 0)
        {
            Debug.LogWarning("사용 가능한 스킬 없음");
            return;
        }

        ShowSkillRoot();

        for (int i = 0; i < skillList.Count; i++)
        {
            SkillData skillData = skillList[i];

            if (skillData == null)
            {
                continue;
            }

            BattleSkillButtonUI skillButton = Instantiate(
                _skillButtonPrefab,
                _skillButtonParent);

            bool canUse = _currentUnit.CanUseSkill(skillData);

            skillButton.Initialize(
                skillData,
                canUse,
                HandleClickSkill);

            _spawnedSkillButtons.Add(skillButton);
        }
    }

    /// <summary>
    /// 스킬 클릭 처리
    /// </summary>
    /// <param name="skillData">선택 스킬</param>
    private void HandleClickSkill(SkillData skillData)
    {
        if (_currentUnit == null)
        {
            return;
        }

        if (skillData == null)
        {
            return;
        }

        if (_currentUnit.CanUseSkill(skillData) == false)
        {
            Debug.LogWarning("MP 부족 또는 사용 불가 스킬");
            return;
        }

        _selectedSkill = skillData;

        HideSkillRoot();
        OpenTargetSelectionForSkill();
    }

    /// <summary>
    /// 스킬 대상 선택 UI 열기
    /// </summary>
    private void OpenTargetSelectionForSkill()
    {
        ClearTargetButtons();

        if (_currentUnit == null || _selectedSkill == null)
        {
            return;
        }

        if (_battleCycleController.DoesSkillRequireTargetSelection(_selectedSkill) == false)
        {
            SubmitSkillAction(null);
            return;
        }

        _battleCycleController.GetSelectableSkillTargets(
            _currentUnit,
            _selectedSkill,
            _targetCandidates);

        if (_targetCandidates.Count == 0)
        {
            Debug.LogWarning("스킬 대상 없음");
            return;
        }

        ShowTargetRoot();

        for (int i = 0; i < _targetCandidates.Count; i++)
        {
            BattleUnit target = _targetCandidates[i];

            BattleTargetButtonUI targetButton = Instantiate(
                _targetButtonPrefab,
                _targetButtonParent);

            targetButton.Initialize(target, HandleClickSkillTarget);

            _spawnedTargetButtons.Add(targetButton);
        }
    }

    /// <summary>
    /// 스킬 대상 클릭 처리
    /// </summary>
    /// <param name="target">선택 대상</param>
    private void HandleClickSkillTarget(BattleUnit target)
    {
        if (target == null || target.IsAlive == false)
        {
            return;
        }

        SubmitSkillAction(target);
    }

    /// <summary>
    /// 스킬 행동 요청 제출
    /// </summary>
    /// <param name="target">선택 대상</param>
    private void SubmitSkillAction(BattleUnit target)
    {
        if (_currentUnit == null || _selectedSkill == null)
        {
            return;
        }

        BattleActionRequest actionRequest =
            BattleActionRequest.CreateSkill(_currentUnit, _selectedSkill, target);

        _battleCycleController.SubmitAction(actionRequest);

        _selectedSkill = null;

        Hide();
    }

    /// <summary>
    /// 대상 버튼 정리
    /// </summary>
    private void ClearTargetButtons()
    {
        for (int i = 0; i < _spawnedTargetButtons.Count; i++)
        {
            BattleTargetButtonUI targetButton = _spawnedTargetButtons[i];

            if (targetButton == null)
            {
                continue;
            }

            Destroy(targetButton.gameObject);
        }

        _spawnedTargetButtons.Clear();
        _targetCandidates.Clear();
    }

    /// <summary>
    /// 스킬 버튼 정리
    /// </summary>
    private void ClearSkillButtons()
    {
        for (int i = 0; i < _spawnedSkillButtons.Count; i++)
        {
            BattleSkillButtonUI skillButton = _spawnedSkillButtons[i];

            if (skillButton == null)
            {
                continue;
            }

            Destroy(skillButton.gameObject);
        }

        _spawnedSkillButtons.Clear();
    }

    /// <summary>
    /// 커맨드 UI 표시
    /// </summary>
    private void ShowCommandRoot()
    {
        if (_commandRoot != null)
        {
            _commandRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 대상 UI 표시
    /// </summary>
    private void ShowTargetRoot()
    {
        if (_targetRoot != null)
        {
            _targetRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 스킬 UI 표시
    /// </summary>
    private void ShowSkillRoot()
    {
        if (_skillRoot != null)
        {
            _skillRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 대상 UI 숨김
    /// </summary>
    private void HideTargetRoot()
    {
        ClearTargetButtons();

        if (_targetRoot != null)
        {
            _targetRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 스킬 UI 숨김
    /// </summary>
    private void HideSkillRoot()
    {
        ClearSkillButtons();

        if (_skillRoot != null)
        {
            _skillRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 전체 전투 커맨드 UI 숨김
    /// </summary>
    private void Hide()
    {
        if (_commandRoot != null)
        {
            _commandRoot.SetActive(false);
        }

        HideTargetRoot();
        HideSkillRoot();
    }
}