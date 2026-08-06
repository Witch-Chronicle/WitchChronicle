using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;

public class ResultController : MonoBehaviour
{


    [Header("Content")]
    [SerializeField] private TMP_Text _resultTxt;
    [SerializeField] private Color _winColor = Color.blue;
    [SerializeField] private Color _loseColor = Color.red;

    [Header("ResultWrap Fade")]
    [SerializeField] private CanvasGroup _resultWrapCanvasGroup;
    [SerializeField] private float _fadeDuration = 0.3f;

    [Header("Result 표시 중 숨길 HUD 오브젝트")]
    [SerializeField] private GameObject _charactersObject;
    [SerializeField] private GameObject _turnObject;

    [Header("패배 시 복귀 씬")]
    [SerializeField] private SceneId _defeatReturnScene = SceneId.Main; // 거점 씬

    [Header("Reward UI")]
    [SerializeField] private BattleRewardManager _battleRewardManager;
    [SerializeField] private TMP_Text _goldTxt;
    [SerializeField] private List<CharacterXpRow> _characterXpRows = new List<CharacterXpRow>();

    [Header("Reward Item")]
    [SerializeField] private DropItemRow _dropItemRowPrefab;
    [SerializeField] private Transform _dropItemContent; // Content 오브젝트
    [SerializeField] private GameObject _noGainTxt;

    [Header("Confirm (XP 애니메이션이 전부 끝나면 페이드인, Enter로 진행)")]
    [SerializeField] private GameObject _confirmObject;
    [SerializeField] private float _confirmFadeDuration = 0.3f;

    [Header("Battle Scene (Additive 언로드용)")]
    [SerializeField] private string _battleSceneName = "Battle";

    private readonly List<DropItemRow> _spawnedDropItemRows = new List<DropItemRow>();

    private CanvasGroup _confirmCanvasGroup;

    private Sequence _fadeSequence;
    private bool _isPlayerWin;

    private int _totalActiveRows;
    private int _completedRowCount;
    private bool _isXpAnimating;
    private bool _isConfirmVisible;

    private void Start()
    {
        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnBattleEnded += HandleBattleEnded;
        }
        else
        {
            Debug.LogWarning("[ResultController] BattleUIContext.Instance가 null입니다.");
        }

        if (_battleRewardManager != null)
        {
            _battleRewardManager.OnRewardsCalculated += HandleRewardsCalculated;
        }
        else
        {
            Debug.LogWarning("[ResultController] BattleRewardManager 참조가 없습니다.");
        }

        if (_confirmObject != null)
        {
            _confirmCanvasGroup = _confirmObject.GetComponent<CanvasGroup>();

            if (_confirmCanvasGroup == null)
            {
                Debug.LogWarning("[ResultController] Confirm 오브젝트에 CanvasGroup이 없어 페이드 없이 즉시 표시됩니다.");
            }
        }

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnBattleEnded -= HandleBattleEnded;
        }

        if (_battleRewardManager != null)
        {
            _battleRewardManager.OnRewardsCalculated -= HandleRewardsCalculated;
        }

        UnsubscribeAllRows();

        ClearDropItems();

        _fadeSequence?.Kill();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.enterKey.wasPressedThisFrame == false) return;

        if (_isXpAnimating)
        {
            SkipXpAnimations();
        }
        else if (_isConfirmVisible)
        {
            HandleConfirmClicked();
        }
    }

    private void HandleBattleEnded(BattleTeamType winner)
    {
        gameObject.SetActive(true);

        _isPlayerWin = winner == BattleTeamType.Player;

        if (_resultTxt != null)
        {
            _resultTxt.text = _isPlayerWin ? "VICTORY" : "DEFEAT";
            _resultTxt.color = _isPlayerWin ? _winColor : _loseColor;
        }

        HideConfirmImmediate();

        if (_charactersObject != null) _charactersObject.SetActive(false);
        if (_turnObject != null) _turnObject.SetActive(false);

        PlayFadeInSequence();
    }

    /// <summary>
    /// BattleRewardManager가 보상 계산/지급을 마치면 호출됨. Result 패널에 골드/캐릭터별 결과/획득 아이템 반영.
    /// 캐릭터별 XP 애니메이션이 전부 끝나야 Confirm이 나타남.
    /// </summary>
    private void HandleRewardsCalculated(int totalGold, List<CharacterRewardResult> results, List<DropResult> drops)
    {
        if (_goldTxt != null)
        {
            _goldTxt.text = $"+ {totalGold:N0} G";
        }

        UnsubscribeAllRows();

        _totalActiveRows = 0;
        _completedRowCount = 0;
        _isXpAnimating = true;
        _isConfirmVisible = false;
        HideConfirmImmediate();

        for (int i = 0; i < _characterXpRows.Count; i++)
        {
            CharacterXpRow row = _characterXpRows[i];

            if (row == null)
            {
                continue;
            }

            if (results == null || i >= results.Count)
            {
                row.gameObject.SetActive(false);
                continue;
            }

            row.gameObject.SetActive(true);
            row.OnCompleted += HandleRowCompleted;
            _totalActiveRows++;

            row.SetData(results[i]);
        }

        RefreshDropItems(drops);

        // 표시할 캐릭터가 하나도 없으면(예외적 상황) 바로 Confirm 노출
        if (_totalActiveRows == 0)
        {
            _isXpAnimating = false;
            RevealConfirm();
        }
    }

    private void HandleRowCompleted()
    {
        _completedRowCount++;

        if (_completedRowCount < _totalActiveRows)
        {
            return;
        }

        _isXpAnimating = false;
        RevealConfirm();
    }

    /// <summary>
    /// 진행 중인 모든 Row 애니메이션을 즉시 최종 상태로 완료시킴 (Enter 스킵).
    /// </summary>
    private void SkipXpAnimations()
    {
        for (int i = 0; i < _characterXpRows.Count; i++)
        {
            CharacterXpRow row = _characterXpRows[i];

            if (row == null || row.gameObject.activeSelf == false)
            {
                continue;
            }

            row.CompleteImmediately();
        }
    }

    private void UnsubscribeAllRows()
    {
        for (int i = 0; i < _characterXpRows.Count; i++)
        {
            CharacterXpRow row = _characterXpRows[i];

            if (row == null)
            {
                continue;
            }

            row.OnCompleted -= HandleRowCompleted;
        }
    }

    private void RevealConfirm()
    {
        _isConfirmVisible = true;

        if (_confirmObject != null && _confirmObject.activeSelf == false)
        {
            _confirmObject.SetActive(true);
        }

        if (_confirmCanvasGroup == null) return;

        _confirmCanvasGroup.DOKill();
        _confirmCanvasGroup.alpha = 0f;
        _confirmCanvasGroup.interactable = true;
        _confirmCanvasGroup.blocksRaycasts = true;

        _confirmCanvasGroup.DOFade(1f, _confirmFadeDuration);
    }

    private void HideConfirmImmediate()
    {
        if (_confirmCanvasGroup != null)
        {
            _confirmCanvasGroup.DOKill();
            _confirmCanvasGroup.alpha = 0f;
            _confirmCanvasGroup.interactable = false;
            _confirmCanvasGroup.blocksRaycasts = false;
        }

        if (_confirmObject != null)
        {
            _confirmObject.SetActive(false);
        }
    }

    /// <summary>
    /// 획득 아이템 목록을 Content 하위에 동적으로 생성/갱신.
    /// 드롭 아이템이 하나도 없으면 NoGainTxt를 활성화.
    /// </summary>
    private void RefreshDropItems(List<DropResult> drops)
    {
        ClearDropItems();

        bool hasDrops = drops != null && drops.Count > 0;

        if (_noGainTxt != null)
        {
            _noGainTxt.SetActive(hasDrops == false);
        }

        if (hasDrops == false || _dropItemRowPrefab == null || _dropItemContent == null)
        {
            return;
        }

        for (int i = 0; i < drops.Count; i++)
        {
            DropItemRow row = Instantiate(_dropItemRowPrefab, _dropItemContent);
            row.SetData(drops[i]);
            _spawnedDropItemRows.Add(row);
        }
    }

    private void ClearDropItems()
    {
        for (int i = 0; i < _spawnedDropItemRows.Count; i++)
        {
            if (_spawnedDropItemRows[i] != null)
            {
                Destroy(_spawnedDropItemRows[i].gameObject);
            }
        }

        _spawnedDropItemRows.Clear();
    }

    /// <summary>
    /// Result 패널 등장 시 페이드인만 수행 (자동으로 사라지지 않음, XP 애니메이션 종료 후 Confirm이 Enter를 기다림).
    /// </summary>
    private void PlayFadeInSequence()
    {
        if (_resultWrapCanvasGroup == null) return;

        _fadeSequence?.Kill();

        _resultWrapCanvasGroup.alpha = 0f;
        _resultWrapCanvasGroup.blocksRaycasts = true;
        _resultWrapCanvasGroup.interactable = true;

        _fadeSequence = DOTween.Sequence();
        _fadeSequence.Append(_resultWrapCanvasGroup.DOFade(1f, _fadeDuration));
    }

    /// <summary>
    /// Confirm이 보이는 상태에서 Enter 입력 시: Result 페이드아웃 -> 씬 전환.
    /// </summary>
    private void HandleConfirmClicked()
    {
        if (_isConfirmVisible == false) return;

        _isConfirmVisible = false; // 중복 트리거 방지

        PlayFadeOutSequence();
    }

    private void PlayFadeOutSequence()
    {
        if (_resultWrapCanvasGroup == null)
        {
            HandlePanelFadeOutComplete();
            return;
        }

        _fadeSequence?.Kill();

        _fadeSequence = DOTween.Sequence();
        _fadeSequence.Append(_resultWrapCanvasGroup.DOFade(0f, _fadeDuration));
        _fadeSequence.OnComplete(HandlePanelFadeOutComplete);
    }

    private void HandlePanelFadeOutComplete()
    {
        gameObject.SetActive(false);

        if (_isPlayerWin)
        {
            HandleVictoryTransition();
        }
        else
        {
            HandleDefeatTransition();
        }
    }

    /// <summary>
    /// 승리: 전투 씬만 Unload. 던전은 파괴된 적 없으니 파티만 재활성화하면 끝.
    /// </summary>
    private void HandleVictoryTransition()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[ResultController] SceneTransitionManager.Instance가 없습니다.");
            return;
        }

        SceneTransitionManager.Instance.UnloadScene(_battleSceneName, () =>
        {
            BattleEncounterContext.Instance.DestroyEncounter();

            if (Party.Instance != null)
            {
                Party.Instance.gameObject.SetActive(true);
            }

            if (DungeonPartyQueueController.Instance != null)
            {
                DungeonPartyQueueController.Instance.gameObject.SetActive(true);
            }

            if (BattleEncounterContext.Instance != null)
            {
                BattleEncounterContext.Instance.ClearEncounter();
            }

            if (CursorLocker.Instance != null)
            {
                CursorLocker.Instance.ExitUIMode();
            }
        });
    }

    /// <summary>
    /// 패배: 거점 씬으로 Single 로드 — 던전+전투 씬이 한 번에 정리됨. 기존 로직 그대로.
    /// </summary>
    private void HandleDefeatTransition()
    {
        RestoreAllPartyVitals();

        SceneTransitionManager.Instance.LoadScene(
            _defeatReturnScene,
            delayBeforeLoad: 0f,
            onBeforeLoad: null,
            onLoaded: () =>
            {
                if (BattleEncounterContext.Instance != null)
                {
                    BattleEncounterContext.Instance.ClearEncounter();
                }

                FieldPartySpawner spawner = FindAnyObjectByType<FieldPartySpawner>();

                if (spawner != null)
                {
                    spawner.SpawnAtSpawnPoint();
                }
            });
    }

    /// <summary>
    /// 현재 파티 전원의 HP/MP를 최대치로 초기화 (패배 페널티 없이 거점 복귀)
    /// </summary>
    private void RestoreAllPartyVitals()
    {
        if (PersistentCharacterManager.Instance == null)
        {
            Debug.LogWarning("[ResultController] PersistentCharacterManager.Instance가 없습니다.");
            return;
        }

        List<PersistentCharacterUnit> activeParty = new List<PersistentCharacterUnit>();
        PersistentCharacterManager.Instance.GetActivePartyMembers(activeParty);

        for (int i = 0; i < activeParty.Count; i++)
        {
            PersistentCharacterUnit unit = activeParty[i];

            if (unit == null || unit.CharacterVitals == null)
            {
                continue;
            }

            unit.CharacterVitals.InitializeFullVitals();
        }
    }
}