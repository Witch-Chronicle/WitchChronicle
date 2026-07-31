using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 전투 시작 카메라 연출 제어
/// </summary>
public class BattleIntroDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private BattleCameraDirector _battleCameraDirector;

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
    }

    /// <summary>
    /// 전투 시작 연출 재생
    /// </summary>
    /// <param name="onComplete">연출 완료 콜백</param>
    public void PlayIntro(Action onComplete = null)
    {
        StopIntro();

        _introRoutine = StartCoroutine(
            PlayIntroRoutine(onComplete));
    }

    /// <summary>
    /// 전투 시작 연출 중단
    /// </summary>
    public void StopIntro()
    {
        if (_introRoutine == null)
        {
            return;
        }

        StopCoroutine(_introRoutine);
        _introRoutine = null;
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
            _introRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        BattleUnit playerUnit = GetFirstAlivePlayer();

        if (playerUnit == null)
        {
            _introRoutine = null;
            onComplete?.Invoke();
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

        _introRoutine = null;
        onComplete?.Invoke();
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
}