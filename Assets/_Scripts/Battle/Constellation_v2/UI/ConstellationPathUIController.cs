using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경로형 별자리 UI 관리
/// 노드, 연결선, 타이머와 입력 피드백 관리
/// </summary>
public class ConstellationPathUIController :
    MonoBehaviour
{
    [Header("Controller")]
    [SerializeField]
    private ConstellationPathController _pathController;

    [Header("Panel")]
    [SerializeField]
    private CanvasGroup _panelCanvasGroup;

    [Header("Nodes")]
    [SerializeField]
    private RectTransform _nodeRoot;

    [SerializeField]
    private ConstellationPathNodeView _nodePrefab;

    [Header("Lines")]
    [SerializeField]
    private RectTransform _lineRoot;

    [SerializeField]
    private ConstellationPathLineView _linePrefab;

    [Header("Timer")]
    [SerializeField]
    private CanvasGroup _timerCanvasGroup;

    [SerializeField]
    private Image _timerFillImage;

    [Header("Audio")]
    [SerializeField]
    private AudioSource _audioSource;

    [SerializeField]
    private AudioClip _acceptedClip;

    [SerializeField]
    private AudioClip _invalidClip;

    [Header("Resolution")]
    [SerializeField]
    private Image _screenFlashImage;

    [SerializeField, Min(0f)]
    private float _successStepInterval = 0.1f;

    [SerializeField, Min(0f)]
    private float _successFinalHoldDuration = 0.08f;

    [SerializeField, Min(0f)]
    private float _successDisappearWaitDuration = 0.4f;

    [SerializeField, Min(0f)]
    private float _failureNodeInterval = 0.035f;

    [SerializeField, Min(0f)]
    private float _failureDisappearWaitDuration = 0.5f;

    [SerializeField, Min(0f)]
    private float _screenFlashDuration = 0.14f;

    [SerializeField, Range(0f, 1f)]
    private float _screenFlashMaxAlpha = 0.75f;

    [SerializeField]
    private AudioClip _successResolutionClip;

    [SerializeField]
    private AudioClip _failureResolutionClip;

    private readonly Dictionary<
        string,
        ConstellationPathNodeView> _nodeViews =
            new Dictionary<
                string,
                ConstellationPathNodeView>();

    private readonly Dictionary<
        string,
        ConstellationPathLineView> _lineViews =
            new Dictionary<
                string,
                ConstellationPathLineView>();

    private bool _isInputEnabled;

    private Coroutine _resolutionRoutine;

    public event Action<ConstellationPathResult>
    OnResolutionPresentationFinished;

    /// <summary>
    /// 경로 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (_pathController == null)
        {
            return;
        }

        _pathController.OnPathStarted +=
            HandlePathStarted;

        _pathController.OnNodeRevealRequested +=
            HandleNodeRevealRequested;

        _pathController.OnAllNodesRevealed +=
            HandleAllNodesRevealed;

        _pathController.OnInputStarted +=
            HandleInputStarted;

        _pathController.OnTimerChanged +=
            HandleTimerChanged;

        _pathController.OnNodeInputResolved +=
            HandleNodeInputResolved;

        _pathController.OnNodeCompleted +=
            HandleNodeCompleted;

        _pathController.OnNodeStateChanged +=
            HandleNodeStateChanged;

        _pathController.OnPathCompleted +=
            HandlePathCompleted;
    }

    /// <summary>
    /// 경로 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_pathController != null)
        {
            _pathController.OnPathStarted -=
                HandlePathStarted;

            _pathController.OnNodeRevealRequested -=
                HandleNodeRevealRequested;

            _pathController.OnAllNodesRevealed -=
                HandleAllNodesRevealed;

            _pathController.OnInputStarted -=
                HandleInputStarted;

            _pathController.OnTimerChanged -=
                HandleTimerChanged;

            _pathController.OnNodeInputResolved -=
                HandleNodeInputResolved;

            _pathController.OnNodeCompleted -=
                HandleNodeCompleted;

            _pathController.OnNodeStateChanged -=
                HandleNodeStateChanged;

            _pathController.OnPathCompleted -=
                HandlePathCompleted;
        }

        StopResolutionRoutine();
        SetScreenFlashAlpha(0f);

        ClearNodes();
        ClearLines();

        SetPanelVisible(
            false);
    }

    /// <summary>
    /// 별자리 UI 숨김
    /// </summary>
    public void HidePathUI()
    {
        _isInputEnabled = false;

        SetAllNodeInteraction(
            false);

        SetPanelVisible(
            false);
    }

    /// <summary>
    /// 별자리 시작 UI 초기화
    /// </summary>
    /// <param name="sequenceData">시작 시퀀스 데이터</param>
    private void HandlePathStarted(
        ConstellationPathSequenceData sequenceData)
    {
        StopResolutionRoutine();
        SetScreenFlashAlpha(0f);

        ClearNodes();
        ClearLines();

        _isInputEnabled = false;

        SetPanelVisible(
            true);

        SetTimerVisible(
            false);

        SetTimerFill(
            1f);
    }

    /// <summary>
    /// 노드 등장 요청 처리
    /// </summary>
    /// <param name="nodeData">등장 노드 데이터</param>
    private void HandleNodeRevealRequested(
        ConstellationPathNodeData nodeData)
    {
        if (nodeData == null ||
            _nodeRoot == null ||
            _nodePrefab == null)
        {
            return;
        }

        if (_nodeViews.ContainsKey(
                nodeData.NodeId))
        {
            return;
        }

        ConstellationPathNodeView nodeView =
            Instantiate(
                _nodePrefab,
                _nodeRoot,
                false);

        nodeView.Initialize(
            nodeData);

        nodeView.OnClicked +=
            HandleNodeClicked;

        _nodeViews.Add(
            nodeData.NodeId,
            nodeView);

        nodeView.PlayReveal();
    }

    /// <summary>
    /// 전체 노드 등장 완료 처리
    /// </summary>
    private void HandleAllNodesRevealed()
    {
        SetTimerVisible(
            true);

        SetTimerFill(
            1f);
    }

    /// <summary>
    /// 입력 시작 처리
    /// </summary>
    private void HandleInputStarted()
    {
        _isInputEnabled = true;

        SetAllNodeInteraction(
            true);
    }

    /// <summary>
    /// 타이머 표시 갱신
    /// </summary>
    /// <param name="normalizedTime">정규화 남은 시간</param>
    /// <param name="remainingTime">실제 남은 시간</param>
    private void HandleTimerChanged(
        float normalizedTime,
        float remainingTime)
    {
        SetTimerFill(
            normalizedTime);
    }

    /// <summary>
    /// 노드 입력 결과 피드백 처리
    /// </summary>
    /// <param name="nodeId">입력 노드 ID</param>
    /// <param name="inputResult">입력 결과</param>
    private void HandleNodeInputResolved(
        string nodeId,
        ConstellationPathInputResult inputResult)
    {
        if (!_nodeViews.TryGetValue(
                nodeId,
                out ConstellationPathNodeView nodeView))
        {
            return;
        }

        switch (inputResult)
        {
            case ConstellationPathInputResult.Accepted:
                nodeView.PlayAcceptedFeedback();

                PlayClip(
                    _acceptedClip);
                break;

            case ConstellationPathInputResult.Locked:
            case ConstellationPathInputResult.AlreadyCompleted:
                nodeView.PlayInvalidFeedback();

                PlayClip(
                    _invalidClip);
                break;
        }
    }

    /// <summary>
    /// 노드 완료 연결선 생성
    /// </summary>
    /// <param name="nodeData">완료 노드 데이터</param>
    private void HandleNodeCompleted(
        ConstellationPathNodeData nodeData)
    {
        if (nodeData == null)
        {
            return;
        }

        for (int i = 0;
             i < nodeData.PrerequisiteNodeIds.Count;
             i++)
        {
            string prerequisiteNodeId =
                nodeData.PrerequisiteNodeIds[i];

            CreateConnectionLine(
                prerequisiteNodeId,
                nodeData.NodeId);
        }
    }

    /// <summary>
    /// 노드 상태 변경 처리
    /// </summary>
    /// <param name="nodeId">노드 ID</param>
    /// <param name="nodeState">변경 상태</param>
    private void HandleNodeStateChanged(
        string nodeId,
        ConstellationPathNodeState nodeState)
    {
        if (!_nodeViews.TryGetValue(
                nodeId,
                out ConstellationPathNodeView nodeView))
        {
            return;
        }

        nodeView.SetState(
            nodeState);

        bool canInteract =
            _isInputEnabled &&
            nodeState !=
            ConstellationPathNodeState.Completed;

        nodeView.SetInteractionEnabled(
            canInteract);
    }

    /// <summary>
    /// 노드 클릭 입력 전달
    /// </summary>
    /// <param name="nodeId">클릭 노드 ID</param>
    private void HandleNodeClicked(
        string nodeId)
    {
        if (!_isInputEnabled ||
            _pathController == null)
        {
            return;
        }

        _pathController.SubmitNodeInput(
            nodeId);
    }

    /// <summary>
    /// 별자리 판정 완료 처리
    /// </summary>
    /// <param name="result">최종 결과</param>
    private void HandlePathCompleted(
        ConstellationPathResult result)
    {
        _isInputEnabled = false;

        SetAllNodeInteraction(
            false);

        StopResolutionRoutine();

        _resolutionRoutine =
            StartCoroutine(
                PlayResolutionRoutine(
                    result));
    }

    /// <summary>
    /// 두 노드 사이 연결선 생성
    /// </summary>
    /// <param name="startNodeId">시작 노드 ID</param>
    /// <param name="endNodeId">종료 노드 ID</param>
    private void CreateConnectionLine(
        string startNodeId,
        string endNodeId)
    {
        if (_lineRoot == null ||
            _linePrefab == null)
        {
            return;
        }

        string lineKey =
            $"{startNodeId}->{endNodeId}";

        if (_lineViews.ContainsKey(
                lineKey))
        {
            return;
        }

        if (!_nodeViews.TryGetValue(
                startNodeId,
                out ConstellationPathNodeView startNode))
        {
            return;
        }

        if (!_nodeViews.TryGetValue(
                endNodeId,
                out ConstellationPathNodeView endNode))
        {
            return;
        }

        ConstellationPathLineView lineView =
            Instantiate(
                _linePrefab,
                _lineRoot,
                false);

        lineView.Initialize(
            startNodeId,
            endNodeId,
            startNode.RectTransform,
            endNode.RectTransform,
            _lineRoot);

        _lineViews.Add(
            lineKey,
            lineView);
    }

    /// <summary>
    /// 전체 노드 클릭 허용 상태 변경
    /// </summary>
    /// <param name="isEnabled">허용 여부</param>
    private void SetAllNodeInteraction(
        bool isEnabled)
    {
        foreach (
            ConstellationPathNodeView nodeView
            in _nodeViews.Values)
        {
            if (nodeView == null)
            {
                continue;
            }

            bool canInteract =
                isEnabled &&
                nodeView.State !=
                ConstellationPathNodeState.Completed;

            nodeView.SetInteractionEnabled(
                canInteract);
        }
    }

    /// <summary>
    /// 생성된 노드 전체 제거
    /// </summary>
    private void ClearNodes()
    {
        foreach (
            ConstellationPathNodeView nodeView
            in _nodeViews.Values)
        {
            if (nodeView == null)
            {
                continue;
            }

            nodeView.OnClicked -=
                HandleNodeClicked;

            Destroy(
                nodeView.gameObject);
        }

        _nodeViews.Clear();
    }

    /// <summary>
    /// 생성된 연결선 전체 제거
    /// </summary>
    private void ClearLines()
    {
        foreach (
            ConstellationPathLineView lineView
            in _lineViews.Values)
        {
            if (lineView == null)
            {
                continue;
            }

            Destroy(
                lineView.gameObject);
        }

        _lineViews.Clear();
    }

    /// <summary>
    /// 효과음 재생
    /// </summary>
    /// <param name="audioClip">재생 클립</param>
    private void PlayClip(
        AudioClip audioClip)
    {
        if (_audioSource == null ||
            audioClip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(
            audioClip);
    }

    /// <summary>
    /// 별자리 패널 표시 상태 변경
    /// </summary>
    /// <param name="isVisible">표시 여부</param>
    private void SetPanelVisible(
        bool isVisible)
    {
        if (_panelCanvasGroup == null)
        {
            return;
        }

        _panelCanvasGroup.alpha =
            isVisible ? 1f : 0f;

        _panelCanvasGroup.interactable =
            isVisible;

        _panelCanvasGroup.blocksRaycasts =
            isVisible;
    }

    /// <summary>
    /// 타이머 표시 상태 변경
    /// </summary>
    /// <param name="isVisible">표시 여부</param>
    private void SetTimerVisible(
        bool isVisible)
    {
        if (_timerCanvasGroup == null)
        {
            return;
        }

        _timerCanvasGroup.alpha =
            isVisible ? 1f : 0f;

        _timerCanvasGroup.interactable =
            false;

        _timerCanvasGroup.blocksRaycasts =
            false;
    }

    /// <summary>
    /// 타이머 Fill 갱신
    /// </summary>
    /// <param name="normalizedTime">정규화 남은 시간</param>
    private void SetTimerFill(
        float normalizedTime)
    {
        if (_timerFillImage == null)
        {
            return;
        }

        _timerFillImage.fillAmount =
            Mathf.Clamp01(
                normalizedTime);
    }

    /// <summary>
    /// 최종 성공·실패 연출 진행
    /// </summary>
    /// <param name="result">최종 결과</param>
    private IEnumerator PlayResolutionRoutine(
        ConstellationPathResult result)
    {
        SetTimerVisible(
            false);

        if (result.IsSuccess)
        {
            PlayClip(
                _successResolutionClip);

            yield return
                PlaySuccessResolutionRoutine();
        }
        else
        {
            PlayClip(
                _failureResolutionClip);

            yield return
                PlayFailureResolutionRoutine();
        }

        _resolutionRoutine = null;

        SetPanelVisible(
            false);

        ClearNodes();
        ClearLines();

        OnResolutionPresentationFinished?.Invoke(
            result);
    }

    /// <summary>
    /// 성공 판정 연출 진행
    /// </summary>
    private IEnumerator
        PlaySuccessResolutionRoutine()
    {
        ConstellationPathSequenceData sequenceData =
            _pathController != null
                ? _pathController.SequenceData
                : null;

        if (sequenceData != null)
        {
            List<ConstellationPathNodeData> sortedNodes =
                new List<ConstellationPathNodeData>(
                    sequenceData.Nodes);

            sortedNodes.Sort(
                CompareResolutionOrder);

            int currentIndex = 0;

            while (currentIndex <
                   sortedNodes.Count)
            {
                int revealOrder =
                    sortedNodes[currentIndex]
                        .RevealOrder;

                while (currentIndex <
                       sortedNodes.Count &&
                       sortedNodes[currentIndex]
                           .RevealOrder ==
                       revealOrder)
                {
                    ConstellationPathNodeData nodeData =
                        sortedNodes[currentIndex];

                    PlayNodeResolutionPulse(
                        nodeData.NodeId);

                    currentIndex++;
                }

                yield return WaitUnscaled(
                    _successStepInterval);
            }
        }

        yield return WaitUnscaled(
            _successFinalHoldDuration);

        StartCoroutine(
            PlayScreenFlashRoutine());

        foreach (
            ConstellationPathNodeView nodeView
            in _nodeViews.Values)
        {
            if (nodeView == null)
            {
                continue;
            }

            nodeView.PlaySuccessDisappear();
        }

        foreach (
            ConstellationPathLineView lineView
            in _lineViews.Values)
        {
            if (lineView == null)
            {
                continue;
            }

            lineView.PlaySuccessDisappear();
        }

        yield return WaitUnscaled(
            _successDisappearWaitDuration);
    }

    /// <summary>
    /// 실패 판정 연출 진행
    /// </summary>
    private IEnumerator
        PlayFailureResolutionRoutine()
    {
        int lineIndex = 0;

        foreach (
            ConstellationPathLineView lineView
            in _lineViews.Values)
        {
            if (lineView == null)
            {
                continue;
            }

            lineView.PlayFailureDisappear(
                lineIndex *
                _failureNodeInterval);

            lineIndex++;
        }

        int nodeIndex = 0;

        foreach (
            ConstellationPathNodeView nodeView
            in _nodeViews.Values)
        {
            if (nodeView == null ||
                nodeView.RectTransform == null)
            {
                continue;
            }

            Vector2 direction =
                new Vector2(
                    nodeView.RectTransform
                        .localPosition.x,
                    nodeView.RectTransform
                        .localPosition.y);

            if (direction.sqrMagnitude <
                0.001f)
            {
                direction =
                    UnityEngine.Random
                        .insideUnitCircle;
            }

            nodeView.PlayFailureDisappear(
                direction,
                nodeIndex *
                _failureNodeInterval);

            nodeIndex++;
        }

        yield return WaitUnscaled(
            _failureDisappearWaitDuration);
    }

    /// <summary>
    /// 노드와 해당 노드로 들어오는 선 발광
    /// </summary>
    /// <param name="nodeId">발광 노드 ID</param>
    private void PlayNodeResolutionPulse(
        string nodeId)
    {
        if (_nodeViews.TryGetValue(
                nodeId,
                out ConstellationPathNodeView nodeView))
        {
            nodeView.PlayResolutionPulse();
        }

        foreach (
            ConstellationPathLineView lineView
            in _lineViews.Values)
        {
            if (lineView == null ||
                lineView.EndNodeId != nodeId)
            {
                continue;
            }

            lineView.PlayResolutionPulse();
        }
    }

    /// <summary>
    /// 성공 판정 노드 순서 비교
    /// </summary>
    /// <param name="left">왼쪽 노드</param>
    /// <param name="right">오른쪽 노드</param>
    /// <returns>비교 결과</returns>
    private int CompareResolutionOrder(
        ConstellationPathNodeData left,
        ConstellationPathNodeData right)
    {
        int revealOrderComparison =
            left.RevealOrder.CompareTo(
                right.RevealOrder);

        if (revealOrderComparison != 0)
        {
            return revealOrderComparison;
        }

        return string.Compare(
            left.NodeId,
            right.NodeId,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// 화면 단발 섬광 진행
    /// </summary>
    private IEnumerator PlayScreenFlashRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime <
               _screenFlashDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _screenFlashDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _screenFlashDuration;

            progress =
                Mathf.Clamp01(progress);

            float flash =
                Mathf.Sin(
                    progress *
                    Mathf.PI);

            SetScreenFlashAlpha(
                flash *
                _screenFlashMaxAlpha);

            yield return null;
        }

        SetScreenFlashAlpha(
            0f);
    }

    /// <summary>
    /// 화면 섬광 투명도 변경
    /// </summary>
    /// <param name="alpha">투명도</param>
    private void SetScreenFlashAlpha(
        float alpha)
    {
        if (_screenFlashImage == null)
        {
            return;
        }

        Color color =
            _screenFlashImage.color;

        color.a =
            Mathf.Clamp01(alpha);

        _screenFlashImage.color =
            color;
    }

    /// <summary>
    /// 판정 연출 정지
    /// </summary>
    private void StopResolutionRoutine()
    {
        if (_resolutionRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _resolutionRoutine);

        _resolutionRoutine = null;
    }

    /// <summary>
    /// 시간 배율과 무관한 대기
    /// </summary>
    /// <param name="duration">대기 시간</param>
    private IEnumerator WaitUnscaled(
        float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }

    /// <summary>
    /// 별자리 UI와 판정 연출 강제 종료
    /// </summary>
    public void StopPathPresentation()
    {
        StopResolutionRoutine();

        _isInputEnabled = false;

        SetAllNodeInteraction(
            false);

        SetScreenFlashAlpha(
            0f);

        SetTimerVisible(
            false);

        ClearNodes();
        ClearLines();

        SetPanelVisible(
            false);
    }
}