using System;
using UnityEngine;

/// <summary>
/// 전투 별자리 시스템 통합 관리
/// 시퀀스 실행, 결과 보관, 완료 연출 관리
/// </summary>
public class ConstellationBattleManager : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField]
    private ConstellationSequenceController _sequenceController;

    [SerializeField]
    private ConstellationInputController _inputController;

    [SerializeField]
    private ConstellationUIController _uiController;

    [Header("UI")]
    [SerializeField]
    private CanvasGroup _constellationCanvasGroup;

    private bool _isRunning;
    private bool _hasResult;

    private ConstellationResult _lastResult;

    public bool IsRunning => _isRunning;
    public bool HasResult => _hasResult;
    public ConstellationResult LastResult => _lastResult;

    public event Action<ConstellationResult>
        OnConstellationCompleted;

    /// <summary>
    /// 내부 참조 자동 검색
    /// </summary>
    private void Awake()
    {
        if (_sequenceController == null)
        {
            _sequenceController =
                GetComponentInChildren<
                    ConstellationSequenceController>(
                    true);
        }

        if (_inputController == null)
        {
            _inputController =
                GetComponentInChildren<
                    ConstellationInputController>(
                    true);
        }
    }

    /// <summary>
    /// 결과 이벤트와 UI 연출 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (_inputController != null)
        {
            _inputController.OnConstellationCompleted +=
                HandleConstellationCompleted;
        }

        if (_uiController != null)
        {
            _uiController.OnCompletionPresentationFinished +=
                HandleCompletionPresentationFinished;
        }

        SetCanvasVisible(false);
    }

    /// <summary>
    /// 결과 이벤트와 UI 연출 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_inputController != null)
        {
            _inputController.OnConstellationCompleted -=
                HandleConstellationCompleted;
        }

        if (_uiController != null)
        {
            _uiController.OnCompletionPresentationFinished -=
                HandleCompletionPresentationFinished;
        }

        StopConstellation();
    }

    /// <summary>
    /// 별자리 시퀀스 실행
    /// </summary>
    /// <param name="sequenceData">실행 시퀀스 데이터</param>
    /// <returns>실행 성공 여부</returns>
    public bool StartConstellation(
        ConstellationSequenceData sequenceData)
    {
        if (!ValidateReferences())
        {
            return false;
        }

        if (sequenceData == null)
        {
            Debug.LogWarning(
                "[Constellation] 시퀀스 데이터 없음",
                this);

            return false;
        }

        if (!sequenceData.TryValidate(
                out string errorMessage))
        {
            Debug.LogWarning(
                $"[Constellation] 시퀀스 데이터 오류: " +
                $"{errorMessage}",
                sequenceData);

            return false;
        }

        StopConstellation();

        _hasResult = false;
        _lastResult = default;
        _isRunning = true;

        SetCanvasVisible(true);

        _sequenceController.StartSequence(
            sequenceData);

        if (!_sequenceController.IsRunning)
        {
            _isRunning = false;

            SetCanvasVisible(false);

            Debug.LogWarning(
                "[Constellation] 시퀀스 시작 실패",
                this);

            return false;
        }

        return true;
    }

    /// <summary>
    /// 별자리 실행 중단
    /// </summary>
    public void StopConstellation()
    {
        if (_sequenceController != null)
        {
            _sequenceController.StopSequence();
        }

        if (_uiController != null)
        {
            _uiController.StopCompletionPresentation();
        }

        _isRunning = false;
        _hasResult = false;
        _lastResult = default;

        SetCanvasVisible(false);
    }

    /// <summary>
    /// 최근 별자리 결과 반환
    /// </summary>
    /// <param name="result">최근 실행 결과</param>
    /// <returns>결과 존재 여부</returns>
    public bool TryGetLastResult(
        out ConstellationResult result)
    {
        result = _lastResult;
        return _hasResult;
    }

    /// <summary>
    /// 별자리 최종 판정 결과 저장
    /// </summary>
    /// <param name="result">최종 판정 결과</param>
    private void HandleConstellationCompleted(
        ConstellationResult result)
    {
        if (!_isRunning)
        {
            return;
        }

        _lastResult = result;
        _hasResult = true;

        if (_uiController == null ||
            !_uiController.isActiveAndEnabled)
        {
            CompleteConstellation();
            return;
        }

        _uiController.PlayCompletionPresentation(
            result);
    }

    /// <summary>
    /// 별자리 완료 연출 종료 처리
    /// </summary>
    private void HandleCompletionPresentationFinished()
    {
        CompleteConstellation();
    }

    /// <summary>
    /// 별자리 실행 완료
    /// </summary>
    private void CompleteConstellation()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        SetCanvasVisible(false);

        OnConstellationCompleted?.Invoke(
            _lastResult);
    }

    /// <summary>
    /// 필수 참조 유효성 검사
    /// </summary>
    /// <returns>참조 유효 여부</returns>
    private bool ValidateReferences()
    {
        if (_sequenceController == null)
        {
            Debug.LogWarning(
                "[Constellation] SequenceController 참조 없음",
                this);

            return false;
        }

        if (_inputController == null)
        {
            Debug.LogWarning(
                "[Constellation] InputController 참조 없음",
                this);

            return false;
        }

        if (_uiController == null)
        {
            Debug.LogWarning(
                "[Constellation] UIController 참조 없음",
                this);

            return false;
        }

        if (!_sequenceController.isActiveAndEnabled ||
            !_inputController.isActiveAndEnabled ||
            !_uiController.isActiveAndEnabled)
        {
            Debug.LogWarning(
                "[Constellation] 컨트롤러 비활성화 상태",
                this);

            return false;
        }

        return true;
    }

    /// <summary>
    /// 별자리 UI 표시 상태 변경
    /// </summary>
    /// <param name="isVisible">표시 여부</param>
    private void SetCanvasVisible(
        bool isVisible)
    {
        if (_constellationCanvasGroup == null)
        {
            return;
        }

        _constellationCanvasGroup.alpha =
            isVisible ? 1f : 0f;

        _constellationCanvasGroup.interactable =
            isVisible;

        _constellationCanvasGroup.blocksRaycasts =
            isVisible;
    }
}