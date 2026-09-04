using System;
using UnityEngine;

/// <summary>
/// 경로형 별자리 전투 매니저
/// 실행, 논리 결과, 최종 UI 연출 완료 관리
/// </summary>
public class ConstellationPathBattleManager :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ConstellationPathController _pathController;

    [SerializeField]
    private ConstellationPathUIController _uiController;

    private bool _isRunning;
    private bool _hasLogicalResult;
    private bool _hasResult;

    private ConstellationPathResult _logicalResult;
    private ConstellationPathResult _lastResult;

    public bool IsRunning => _isRunning;

    public bool HasResult => _hasResult;

    public ConstellationPathResult LastResult =>
        _lastResult;

    public event Action<ConstellationPathResult>
        OnConstellationCompleted;

    /// <summary>
    /// 내부 참조 자동 검색
    /// </summary>
    private void Awake()
    {
        if (_pathController == null)
        {
            _pathController =
                GetComponentInChildren<
                    ConstellationPathController>(
                    true);
        }

        if (_uiController == null)
        {
            _uiController =
                FindFirstObjectByType<
                    ConstellationPathUIController>();
        }
    }

    /// <summary>
    /// 별자리 결과 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (_pathController != null)
        {
            _pathController.OnPathCompleted +=
                HandlePathCompleted;
        }

        if (_uiController != null)
        {
            _uiController
                .OnResolutionPresentationFinished +=
                HandleResolutionPresentationFinished;
        }
    }

    /// <summary>
    /// 별자리 결과 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_pathController != null)
        {
            _pathController.OnPathCompleted -=
                HandlePathCompleted;
        }

        if (_uiController != null)
        {
            _uiController
                .OnResolutionPresentationFinished -=
                HandleResolutionPresentationFinished;
        }

        StopConstellationPath();
    }

    /// <summary>
    /// 경로형 별자리 시작
    /// </summary>
    /// <param name="sequenceData">실행 시퀀스 데이터</param>
    /// <returns>시작 성공 여부</returns>
    public bool StartConstellationPath(
        ConstellationPathSequenceData sequenceData)
    {
        StopConstellationPath();

        if (!ValidateReferences())
        {
            return false;
        }

        if (sequenceData == null)
        {
            Debug.LogWarning(
                "[ConstellationPath] 시퀀스 데이터 없음",
                this);

            return false;
        }

        if (!sequenceData.TryValidate(
                out string errorMessage))
        {
            Debug.LogWarning(
                $"[ConstellationPath] 데이터 오류: " +
                $"{errorMessage}",
                sequenceData);

            return false;
        }

        _isRunning = true;
        _hasLogicalResult = false;
        _hasResult = false;

        _logicalResult = default;
        _lastResult = default;

        bool isStarted =
            _pathController.StartPath(
                sequenceData);

        if (!isStarted)
        {
            _isRunning = false;

            Debug.LogWarning(
                "[ConstellationPath] 실행 시작 실패",
                this);

            return false;
        }

        return true;
    }

    /// <summary>
    /// 경로형 별자리 실행 중단
    /// </summary>
    public void StopConstellationPath()
    {
        _isRunning = false;
        _hasLogicalResult = false;
        _hasResult = false;

        _logicalResult = default;
        _lastResult = default;

        if (_pathController != null)
        {
            _pathController.StopPath();
        }

        if (_uiController != null)
        {
            _uiController.StopPathPresentation();
        }
    }

    /// <summary>
    /// 마지막 별자리 결과 반환
    /// </summary>
    /// <param name="result">마지막 결과</param>
    /// <returns>결과 존재 여부</returns>
    public bool TryGetLastResult(
        out ConstellationPathResult result)
    {
        result = _lastResult;

        return _hasResult;
    }

    /// <summary>
    /// 별자리 논리 판정 결과 저장
    /// </summary>
    /// <param name="result">논리 판정 결과</param>
    private void HandlePathCompleted(
        ConstellationPathResult result)
    {
        if (!_isRunning)
        {
            return;
        }

        _logicalResult = result;
        _hasLogicalResult = true;

        if (_uiController == null ||
            !_uiController.isActiveAndEnabled)
        {
            CompleteConstellation(
                result);
        }
    }

    /// <summary>
    /// 별자리 최종 UI 연출 완료 처리
    /// </summary>
    /// <param name="presentationResult">UI 연출 결과</param>
    private void HandleResolutionPresentationFinished(
        ConstellationPathResult presentationResult)
    {
        if (!_isRunning)
        {
            return;
        }

        ConstellationPathResult finalResult =
            _hasLogicalResult
                ? _logicalResult
                : presentationResult;

        CompleteConstellation(
            finalResult);
    }

    /// <summary>
    /// 별자리 전투 처리 완료
    /// </summary>
    /// <param name="result">최종 결과</param>
    private void CompleteConstellation(
        ConstellationPathResult result)
    {
        _lastResult = result;
        _hasResult = true;

        _isRunning = false;
        _hasLogicalResult = false;

        OnConstellationCompleted?.Invoke(
            result);
    }

    /// <summary>
    /// 실행 참조 유효성 검사
    /// </summary>
    /// <returns>유효 여부</returns>
    private bool ValidateReferences()
    {
        if (_pathController == null)
        {
            Debug.LogWarning("[ConstellationPath] PathController 참조 없음", this);
            return false;
        }

        if (_uiController == null)
        {
            Debug.LogWarning("[ConstellationPath] UIController 참조 없음", this);
            return false;
        }

        if (!_pathController.isActiveAndEnabled)
        {
            Debug.LogWarning(
                $"[ConstellationPath] PathController 비활성화 / " +
                $"Object: {_pathController.gameObject.name} / " +
                $"activeSelf: {_pathController.gameObject.activeSelf} / " +
                $"activeInHierarchy: {_pathController.gameObject.activeInHierarchy} / " +
                $"enabled: {_pathController.enabled}",
                _pathController);

            return false;
        }

        if (!_uiController.isActiveAndEnabled)
        {
            Debug.LogWarning(
                $"[ConstellationPath] UIController 비활성화 / " +
                $"Object: {_uiController.gameObject.name} / " +
                $"activeSelf: {_uiController.gameObject.activeSelf} / " +
                $"activeInHierarchy: {_uiController.gameObject.activeInHierarchy} / " +
                $"enabled: {_uiController.enabled}",
                _uiController);

            return false;
        }

        return true;
    }
}