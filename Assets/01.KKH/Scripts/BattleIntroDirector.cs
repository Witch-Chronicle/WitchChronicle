using System;
using System.Collections;
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

    [Header("Timing")]
    [Tooltip("전투 씬 공개 연출 대기 시간")]
    [SerializeField] private float _startDelay = 0.7f;
    [Tooltip("적 진영 구도 유지 시간")]
    [SerializeField] private float _enemyViewHoldDuration = 0.5f;
    [Tooltip("기본 전투 구도 정착 시간")]
    [SerializeField] private float _defaultViewHoldDuration = 0.15f;

    private Coroutine _introRoutine;

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

        HideBattleHudImmediate();
    }

    /// <summary>
    /// 전투 시작 연출 재생
    /// </summary>
    /// <param name="onComplete">연출 완료 콜백</param>
    public void PlayIntro(Action onComplete = null)
    {
        StopIntro();
        HideBattleHudImmediate();

        _introRoutine = StartCoroutine(
            PlayIntroRoutine(onComplete));
    }

    /// <summary>
    /// 전투 시작 연출 중단
    /// </summary>
    public void StopIntro()
    {
        if (_introRoutine != null)
        {
            StopCoroutine(_introRoutine);
            _introRoutine = null;
        }

        if (_battleHudCanvasGroup != null)
        {
            _battleHudCanvasGroup.DOKill();
        }
    }

    /// <summary>
    /// 전투 시작 연출 순서 진행
    /// </summary>
    /// <param name="onComplete">연출 완료 콜백</param>
    private IEnumerator PlayIntroRoutine(Action onComplete)
    {
        if (_startDelay > 0f)
        {
            yield return new WaitForSeconds(_startDelay);
        }

        if (_battleManager == null ||
            _battleCameraDirector == null ||
            _battleCameraDirector.isActiveAndEnabled == false)
        {
            yield return CompleteIntro(onComplete);

            yield break;
        }

        BattleUnit playerUnit = GetFirstAlivePlayer();

        if (playerUnit == null)
        {
            yield return CompleteIntro(onComplete);

            yield break;
        }

        bool isEnemyViewCompleted = false;

        _battleCameraDirector.PlayGroupTargetOverview(
            playerUnit,
            BattleTeamType.Enemy,
            () => isEnemyViewCompleted = true);

        while (isEnemyViewCompleted == false)
        {
            yield return null;
        }

        if (_enemyViewHoldDuration > 0f)
        {
            yield return new WaitForSeconds(
                _enemyViewHoldDuration);
        }

        bool isDefaultViewCompleted = false;

        _battleCameraDirector.PlayDefaultBattleView(
            () => isDefaultViewCompleted = true);

        while (isDefaultViewCompleted == false)
        {
            yield return null;
        }

        if (_defaultViewHoldDuration > 0f)
        {
            yield return new WaitForSeconds(
                _defaultViewHoldDuration);
        }

        yield return CompleteIntro(onComplete);
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
}