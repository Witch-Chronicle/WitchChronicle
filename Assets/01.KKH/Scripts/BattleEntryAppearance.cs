using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 전투 유닛 진입 등장 연출
/// </summary>
public class BattleEntryAppearance : MonoBehaviour
{
    [Header("References")]
    [Tooltip("등장 이동을 적용할 Actor 루트")]
    [SerializeField] private Transform _movementRoot;
    [Tooltip("선택적 등장 애니메이터")]
    [SerializeField] private Animator _animator;
    [Tooltip("선택적 월드 스페이스 UI")]
    [SerializeField] private CanvasGroup _worldSpaceCanvasGroup;
    [Tooltip("표시 상태를 제어할 실제 캐릭터 비주얼 루트")]
    [SerializeField] private Transform _visualRoot;

    [Header("Entry Vfx")]
    [SerializeField] private GameObject _entryVfxPrefab;
    [SerializeField] private Vector3 _vfxPositionOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Vector3 _vfxRotationOffset;
    [SerializeField] private float _vfxLifetime = 1.5f;

    [Header("Entry Timing")]
    [Tooltip("등장 연출 시작 후 VFX 생성까지의 시간")]
    [SerializeField, Min(0f)] private float _vfxStartDelay = 0f;
    [Tooltip("등장 연출 시작 후 적 모델 표시까지의 시간")]
    [SerializeField, Min(0f)] private float _visualRevealDelay = 0.25f;
    [Tooltip("등장 연출 시작 후 등장 애니메이션 시작까지의 시간")]
    [SerializeField, Min(0f)] private float _animationStartDelay = 0.25f;
    [Tooltip("등장 연출 시작 후 위치 이동 시작까지의 시간")]
    [SerializeField, Min(0f)] private float _movementStartDelay = 0.25f;
    [Tooltip("모든 등장 동작 완료 후 추가 정착 시간")]
    [SerializeField, Min(0f)] private float _entryCompletionPadding = 0.05f;

    [Header("Entry Movement")]
    [Tooltip("최종 위치보다 뒤에서 시작할 거리")]
    [SerializeField] private float _backOffset = 0.8f;
    [Tooltip("최종 위치보다 아래에서 시작할 거리")]
    [SerializeField] private float _verticalOffset = 0.25f;
    [Tooltip("등장 이동 시간")]
    [SerializeField] private float _moveDuration = 0.4f;
    [SerializeField] private Ease _moveEase = Ease.OutCubic;

    [Header("Optional Animation")]
    [Tooltip("등장 애니메이션 Trigger")]
    [SerializeField] private string _entryTrigger = "Entry";

    private Renderer[] _renderers;

    private Vector3 _finalPosition;
    private Quaternion _finalRotation;

    private Sequence _entrySequence;

    private bool _isPrepared;

    /// <summary>
    /// 등장 전 숨김 상태 준비
    /// </summary>
    public void PrepareHidden()
    {
        ResolveRuntimeReferences();
        StopEntryTween();

        if (_movementRoot == null)
        {
            return;
        }

        _finalPosition =
            _movementRoot.position;

        _finalRotation =
            _movementRoot.rotation;

        SetRenderersVisible(
            false);

        SetWorldSpaceCanvasVisible(
            false);

        _isPrepared = true;
    }

    /// <summary>
    /// 등장 연출 재생
    /// </summary>
    /// <param name="onComplete">완료 콜백</param>
    public void PlayEntry(
        Action onComplete = null)
    {
        if (_isPrepared == false)
        {
            PrepareHidden();
        }

        if (_movementRoot == null)
        {
            onComplete?.Invoke();
            return;
        }

        StopEntryTween();

        Vector3 startPosition =
            _finalPosition -
            _movementRoot.forward *
            _backOffset -
            Vector3.up *
            _verticalOffset;

        _movementRoot.SetPositionAndRotation(
            startPosition,
            _finalRotation);

        // 각 등장 타이밍 전까지 숨김 유지
        SetRenderersVisible(
            false);

        SetWorldSpaceCanvasVisible(
            false);

        _entrySequence =
            DOTween.Sequence();

        // VFX 생성 시점
        _entrySequence.InsertCallback(
            Mathf.Max(
                0f,
                _vfxStartDelay),
            PlayEntryVfx);

        // 실제 적 모델 표시 시점
        _entrySequence.InsertCallback(
            Mathf.Max(
                0f,
                _visualRevealDelay),
            () =>
            {
                SetRenderersVisible(
                    true);

                SetWorldSpaceCanvasVisible(
                    true);
            });

        // 등장 애니메이션 시작 시점
        _entrySequence.InsertCallback(
            Mathf.Max(
                0f,
                _animationStartDelay),
            PlayEntryAnimation);

        float movementStartTime =
            Mathf.Max(
                0f,
                _movementStartDelay);

        float movementDuration =
            Mathf.Max(
                0f,
                _moveDuration);

        if (movementDuration <= 0f)
        {
            _entrySequence.InsertCallback(
                movementStartTime,
                () =>
                {
                    _movementRoot
                        .SetPositionAndRotation(
                            _finalPosition,
                            _finalRotation);
                });
        }
        else
        {
            _entrySequence.Insert(
                movementStartTime,
                _movementRoot
                    .DOMove(
                        _finalPosition,
                        movementDuration)
                    .SetEase(
                        _moveEase));
        }

        float movementEndTime =
            movementStartTime +
            movementDuration;

        float entryEndTime =
            Mathf.Max(
                _vfxStartDelay,
                _visualRevealDelay,
                _animationStartDelay,
                movementEndTime);

        entryEndTime +=
            Mathf.Max(
                0f,
                _entryCompletionPadding);

        // 가장 늦은 연출까지 Sequence 유지
        _entrySequence.InsertCallback(
            entryEndTime,
            () =>
            {
                if (_movementRoot != null)
                {
                    _movementRoot
                        .SetPositionAndRotation(
                            _finalPosition,
                            _finalRotation);
                }
            });

        _entrySequence
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _entrySequence = null;
                _isPrepared = false;

                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 등장 상태 즉시 완료
    /// </summary>
    public void ShowImmediate()
    {
        ResolveRuntimeReferences();
        StopEntryTween();

        if (_movementRoot != null &&
            _isPrepared)
        {
            _movementRoot.SetPositionAndRotation(
                _finalPosition,
                _finalRotation);
        }

        SetRenderersVisible(
            true);

        SetWorldSpaceCanvasVisible(
            true);

        _isPrepared = false;
    }

    /// <summary>
    /// 런타임 생성 비주얼 참조 갱신
    /// </summary>
    private void ResolveRuntimeReferences()
    {
        if (_movementRoot == null)
        {
            _movementRoot =
                transform;
        }

        if (_visualRoot == null)
        {
            _visualRoot =
                _movementRoot;
        }

        _renderers =
            _visualRoot
                .GetComponentsInChildren<
                    Renderer>(true);

        if (_animator == null)
        {
            _animator =
                _visualRoot
                    .GetComponentInChildren<
                        Animator>(true);
        }
    }

    /// <summary>
    /// Renderer 표시 상태 설정
    /// </summary>
    /// <param name="isVisible">표시 여부</param>
    private void SetRenderersVisible(
        bool isVisible)
    {
        if (_renderers == null)
        {
            return;
        }

        for (int i = 0;
             i < _renderers.Length;
             i++)
        {
            Renderer targetRenderer =
                _renderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.enabled =
                isVisible;
        }
    }

    /// <summary>
    /// 월드 스페이스 UI 표시 상태 설정
    /// </summary>
    /// <param name="isVisible">표시 여부</param>
    private void SetWorldSpaceCanvasVisible(
        bool isVisible)
    {
        if (_worldSpaceCanvasGroup == null)
        {
            return;
        }

        _worldSpaceCanvasGroup.alpha =
            isVisible
                ? 1f
                : 0f;

        _worldSpaceCanvasGroup.interactable =
            false;

        _worldSpaceCanvasGroup.blocksRaycasts =
            false;
    }

    /// <summary>
    /// 등장 VFX 재생
    /// </summary>
    private void PlayEntryVfx()
    {
        if (_entryVfxPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition =
            _finalPosition +
            _vfxPositionOffset;

        Quaternion spawnRotation =
            _finalRotation *
            Quaternion.Euler(
                _vfxRotationOffset);

        GameObject entryVfx =
            Instantiate(
                _entryVfxPrefab,
                spawnPosition,
                spawnRotation);

        if (_vfxLifetime > 0f)
        {
            Destroy(
                entryVfx,
                _vfxLifetime);
        }
    }

    /// <summary>
    /// 선택적 등장 애니메이션 재생
    /// </summary>
    private void PlayEntryAnimation()
    {
        if (_animator == null ||
            string.IsNullOrEmpty(
                _entryTrigger) ||
            HasAnimatorParameter(
                _entryTrigger) == false)
        {
            return;
        }

        _animator.ResetTrigger(
            _entryTrigger);

        _animator.SetTrigger(
            _entryTrigger);
    }

    /// <summary>
    /// Animator 파라미터 존재 여부 반환
    /// </summary>
    /// <param name="parameterName">파라미터 이름</param>
    /// <returns>존재 여부</returns>
    private bool HasAnimatorParameter(
        string parameterName)
    {
        if (_animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters =
            _animator.parameters;

        for (int i = 0;
             i < parameters.Length;
             i++)
        {
            if (parameters[i].name ==
                parameterName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 등장 연출 Tween 정지
    /// </summary>
    private void StopEntryTween()
    {
        if (_entrySequence == null)
        {
            return;
        }

        _entrySequence.Kill();
        _entrySequence = null;
    }

    /// <summary>
    /// 비활성화 시 등장 상태 복구
    /// </summary>
    private void OnDisable()
    {
        ShowImmediate();
    }
}