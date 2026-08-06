using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 전투 시작 카메라 연출 제어
/// </summary>
public class BattleIntroDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private BattleCameraDirector _battleCameraDirector;

    [Header("Battle HUD")]
    [SerializeField] private CanvasGroup _battleHudCanvasGroup;
    [SerializeField] private float _hudShowDuration = 0.2f;

    [Header("Constellation UI")]
    [SerializeField] private ConstellationPathUIController _constellationPathUiController;

    [Header("Timing")]
    [Tooltip("전투 씬 공개 연출 대기 시간")]
    [SerializeField] private float _startDelay = 0.7f;
    [Tooltip("적 진영 구도 유지 시간")]
    [SerializeField] private float _enemyViewHoldDuration = 0.5f;
    [Tooltip("기본 전투 구도 정착 시간")]
    [SerializeField] private float _defaultViewHoldDuration = 0.15f;

    [Header("Enemy Entry")]
    [Tooltip("Entry Camera 시작 후 첫 적 등장까지의 시간")]
    [SerializeField] private float _enemyEntryStartDelay = 0.45f;
    [Tooltip("적이 여러 마리일 때 등장 간격")]
    [SerializeField] private float _enemyEntryInterval = 0.08f;
    [Tooltip("마지막 적 등장 후 정착 시간")]
    [SerializeField] private float _enemyEntrySettleDuration = 0.25f;

    private Coroutine _introRoutine;

    private readonly List<BattleEntryAppearance> _enemyEntryAppearances = new List<BattleEntryAppearance>();

    /// <summary>
    /// 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_battleManager == null)
        {
            _battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (_battleCameraDirector == null)
        {
            _battleCameraDirector = FindFirstObjectByType<BattleCameraDirector>();
        }

        if (_constellationPathUiController == null)
        {
            _constellationPathUiController = FindFirstObjectByType<ConstellationPathUIController>();
        }

        HideBattleHudImmediate();
        StopConstellationPresentation();
    }

    /// <summary>
    /// 전투 시작 연출 재생
    /// </summary>
    /// <param name="onComplete">연출 완료 콜백</param>
    public void PlayIntro(Action onComplete = null)
    {
        StopIntro();

        HideBattleHudImmediate();
        StopConstellationPresentation();

        _introRoutine = StartCoroutine(PlayIntroRoutine(onComplete));
    }

    /// <summary>
    /// 전투 시작 연출 중단
    /// </summary>
    public void StopIntro()
    {
        if (_introRoutine != null)
        {
            StopCoroutine(
                _introRoutine);

            _introRoutine = null;
        }

        if (_battleHudCanvasGroup != null)
        {
            _battleHudCanvasGroup.DOKill();
        }

        ShowEnemyEntriesImmediate();
    }

    /// <summary>
    /// 전투 시작 연출 순서 진행
    /// </summary>
    /// <param name="onComplete">연출 완료 콜백</param>
    private IEnumerator PlayIntroRoutine(
        Action onComplete)
    {
        if (_battleManager == null ||
            _battleCameraDirector == null ||
            _battleCameraDirector
                .isActiveAndEnabled == false)
        {
            yield return CompleteIntro(
                onComplete);

            yield break;
        }

        BattleUnit playerUnit =
            GetFirstAlivePlayer();

        if (playerUnit == null)
        {
            yield return CompleteIntro(
                onComplete);

            yield break;
        }

        // Entry Camera 최초 화면 전에 적 숨김
        PrepareEnemyEntries();

        bool isEntryViewCompleted =
            false;

        // 전투씬 진입 직후 Entry Camera 활성화
        _battleCameraDirector
            .PlayBattleEntryView(
                () =>
                {
                    isEntryViewCompleted =
                        true;
                });

        // Cinemachine 화면 반영 대기
        yield return new WaitForEndOfFrame();

        if (_startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                _startDelay);
        }

        // Entry Camera 이동 중 적 순차 등장
        yield return PlayEnemyEntriesRoutine();

        // 카메라 이동이 아직 끝나지 않았다면 완료 대기
        while (isEntryViewCompleted == false)
        {
            yield return null;
        }

        if (_enemyViewHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                _enemyViewHoldDuration);
        }

        bool isDefaultViewCompleted =
            false;

        _battleCameraDirector
            .PlayDefaultBattleView(
                () =>
                {
                    isDefaultViewCompleted =
                        true;
                });

        while (isDefaultViewCompleted == false)
        {
            yield return null;
        }

        if (_defaultViewHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                _defaultViewHoldDuration);
        }

        yield return CompleteIntro(
            onComplete);
    }

    /// <summary>
    /// 첫 번째 생존 플레이어 반환
    /// </summary>
    /// <returns>생존 플레이어 유닛</returns>
    private BattleUnit GetFirstAlivePlayer()
    {
        if (_battleManager == null ||
            _battleManager.ActiveBattleUnits == null)
        {
            return null;
        }

        for (int i = 0;
             i < _battleManager.ActiveBattleUnits.Count;
             i++)
        {
            BattleUnit unit =
                _battleManager.ActiveBattleUnits[i];

            if (unit == null ||
                unit.IsAlive == false ||
                unit.TeamType != BattleTeamType.Player)
            {
                continue;
            }

            return unit;
        }

        return null;
    }

    /// <summary>
    /// 적 등장 연출 목록 준비
    /// </summary>
    private void PrepareEnemyEntries()
    {
        _enemyEntryAppearances.Clear();

        if (_battleManager == null ||
            _battleManager.SpawnedActors == null)
        {
            return;
        }

        for (int i = 0;
             i < _battleManager
                 .SpawnedActors.Count;
             i++)
        {
            BattleActor actor =
                _battleManager
                    .SpawnedActors[i];

            if (actor == null ||
                actor.TeamType !=
                BattleTeamType.Enemy ||
                actor.HasBattleUnit == false ||
                actor.BattleUnit.IsAlive == false)
            {
                continue;
            }

            BattleEntryAppearance appearance =
                actor.GetComponent<
                    BattleEntryAppearance>();

            if (appearance == null)
            {
                Debug.LogWarning(
                    $"[BattleIntro] {actor.name}에 " +
                    "BattleEntryAppearance 없음");

                continue;
            }

            appearance.PrepareHidden();

            _enemyEntryAppearances.Add(
                appearance);
        }
    }

    /// <summary>
    /// 적 순차 등장 연출 진행
    /// </summary>
    private IEnumerator PlayEnemyEntriesRoutine()
    {
        if (_enemyEntryAppearances.Count <= 0)
        {
            yield break;
        }

        if (_enemyEntryStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                _enemyEntryStartDelay);
        }

        for (int i = 0;
             i < _enemyEntryAppearances.Count;
             i++)
        {
            BattleEntryAppearance appearance =
                _enemyEntryAppearances[i];

            if (appearance != null)
            {
                appearance.PlayEntry();
            }

            bool hasNextEnemy =
                i <
                _enemyEntryAppearances.Count - 1;

            if (hasNextEnemy &&
                _enemyEntryInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    _enemyEntryInterval);
            }
        }

        if (_enemyEntrySettleDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                _enemyEntrySettleDuration);
        }
    }

    /// <summary>
    /// 적 등장 상태 즉시 완료
    /// </summary>
    private void ShowEnemyEntriesImmediate()
    {
        for (int i = 0;
             i < _enemyEntryAppearances.Count;
             i++)
        {
            BattleEntryAppearance appearance =
                _enemyEntryAppearances[i];

            if (appearance == null)
            {
                continue;
            }

            appearance.ShowImmediate();
        }

        _enemyEntryAppearances.Clear();
    }

    /// <summary>
    /// 전투 HUD 즉시 숨김
    /// </summary>
    private void HideBattleHudImmediate()
    {
        if (_battleHudCanvasGroup == null)
        {
            return;
        }

        _battleHudCanvasGroup.DOKill();
        _battleHudCanvasGroup.alpha = 0f;
        _battleHudCanvasGroup.interactable = false;
        _battleHudCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 전투 HUD 표시
    /// </summary>
    /// <param name="onComplete">표시 완료 콜백</param>
    private void ShowBattleHud(Action onComplete)
    {
        if (_battleHudCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        _battleHudCanvasGroup.DOKill();
        _battleHudCanvasGroup.interactable = false;
        _battleHudCanvasGroup.blocksRaycasts = false;

        _battleHudCanvasGroup
            .DOFade(1f, _hudShowDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _battleHudCanvasGroup.interactable = true;
                _battleHudCanvasGroup.blocksRaycasts = true;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 전투 시작 연출 완료 처리
    /// </summary>
    /// <param name="onComplete">연출 완료 콜백</param>
    private IEnumerator CompleteIntro(Action onComplete)
    {
        bool isHudShown = false;

        ShowBattleHud(
            () => isHudShown = true);

        while (isHudShown == false)
        {
            yield return null;
        }

        _introRoutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 별자리 UI 및 판정 연출 정지
    /// </summary>
    private void StopConstellationPresentation()
    {
        if (_constellationPathUiController == null)
        {
            return;
        }

        _constellationPathUiController
            .StopPathPresentation();
    }
}