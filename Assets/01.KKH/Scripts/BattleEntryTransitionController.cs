using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 진입 화면 깨짐 연출 제어
/// </summary>
public class BattleEntryTransitionController : MonoBehaviour
{
    public static BattleEntryTransitionController Instance
    {
        get;
        private set;
    }

    [Header("References")]
    [SerializeField] private CanvasGroup _rootCanvasGroup;
    [SerializeField] private RectTransform _crackRoot;
    [SerializeField] private Image _crackImage;
    [SerializeField] private RectTransform _shardRoot;
    [SerializeField]
    private List<RectTransform> _shards =
        new List<RectTransform>();
    [SerializeField] private Image _flashImage;
    [SerializeField] private Image _blackoutImage;

    [Header("Crack Timing")]
    [Tooltip("균열 등장 시간")]
    [SerializeField] private float _crackShowDuration = 0.08f;
    [Tooltip("균열이 화면에 유지되는 시간")]
    [SerializeField] private float _freezeDuration = 1f;

    [Header("Camera Pull Back")]
    [Tooltip("카메라가 뒤로 빠지는 시간")]
    [SerializeField] private float _pullBackDuration = 0.35f;
    [Tooltip("카메라가 뒤로 빠지는 거리")]
    [SerializeField] private float _pullBackDistance = 1.5f;
    [Tooltip("뒤로 빠질 때 카메라 기울기")]
    [SerializeField] private float _pullBackRoll = 4f;

    [Header("Suction")]
    [Tooltip("화면 안으로 빨려 들어가는 시간")]
    [SerializeField] private float _suctionDuration = 0.5f;
    [Tooltip("카메라 전진 거리")]
    [SerializeField] private float _suctionDistance = 8f;
    [Tooltip("빨려 들어갈 때 최종 FOV")]
    [SerializeField] private float _suctionFov = 90f;

    [Header("Glass Shards")]
    [Tooltip("유리 조각 화면 밖 이동 거리")]
    [SerializeField] private float _shardFlyDistance = 1100f;
    [Tooltip("유리 조각 회전량")]
    [SerializeField] private float _shardRotation = 180f;
    [Tooltip("유리 조각 최종 확대 비율")]
    [SerializeField] private float _shardEndScale = 1.35f;

    [Header("Screen Effect")]
    [SerializeField] private float _flashInDuration = 0.04f;
    [SerializeField] private float _flashOutDuration = 0.1f;
    [SerializeField] private float _blackoutDuration = 0.25f;
    [SerializeField] private float _revealDuration = 0.3f;

    private readonly List<Vector2> _shardStartPositions =
        new List<Vector2>();

    private readonly List<Quaternion> _shardStartRotations =
        new List<Quaternion>();

    private readonly List<Vector3> _shardStartScales =
        new List<Vector3>();

    private Camera _mainCamera;
    private Transform _cameraTransform;
    private CinemachineBrain _cinemachineBrain;
    private CinemachineInputAxisController _cameraInputController;
    private PlayerController _playerController;

    private Vector3 _cameraStartPosition;
    private Quaternion _cameraStartRotation;
    private float _cameraStartFov;

    private bool _brainWasEnabled;
    private bool _cameraInputWasEnabled;
    private float _previousTimeScale = 1f;

    private Sequence _transitionSequence;
    private Action _onBlackoutReached;

    public bool IsPlaying { get; private set; }

    /// <summary>
    /// 싱글톤 등록 및 초기 상태 설정
    /// </summary>
    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CacheShardTransforms();
        HideImmediate();
    }

    /// <summary>
    /// 비활성화 정리
    /// </summary>
    private void OnDisable()
    {
        _transitionSequence?.Kill();
        _transitionSequence = null;

        RestoreTimeScale();
        RestoreFieldControl();

        IsPlaying = false;
    }

    /// <summary>
    /// 싱글톤 해제
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 전투 진입 연출 재생
    /// </summary>
    /// <param name="onBlackoutReached">검은 화면 완료 콜백</param>
    public void PlayEntry(
        Action onBlackoutReached = null)
    {
        if (IsPlaying)
        {
            return;
        }

        IsPlaying = true;
        _onBlackoutReached = onBlackoutReached;

        FindFieldReferences();
        CacheCameraState();
        LockFieldControl();
        PrepareVisuals();

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        PlayEntrySequence();
    }

    /// <summary>
    /// 검은 화면 공개
    /// </summary>
    /// <param name="onComplete">공개 완료 콜백</param>
    public void RevealFromBlack(
        Action onComplete = null)
    {
        _transitionSequence?.Kill();

        if (_rootCanvasGroup == null ||
            _blackoutImage == null)
        {
            HideImmediate();
            IsPlaying = false;
            onComplete?.Invoke();
            return;
        }

        _rootCanvasGroup.alpha = 1f;
        _rootCanvasGroup.blocksRaycasts = true;

        SetImageAlpha(
            _blackoutImage,
            1f);

        _transitionSequence = DOTween.Sequence();
        _transitionSequence.SetUpdate(true);

        _transitionSequence.Append(
            _blackoutImage
                .DOFade(
                    0f,
                    _revealDuration)
                .SetEase(Ease.OutQuad));

        _transitionSequence.OnComplete(() =>
        {
            HideImmediate();
            IsPlaying = false;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 전투 진입 연출 구성
    /// </summary>
    private void PlayEntrySequence()
    {
        _transitionSequence?.Kill();

        _transitionSequence = DOTween.Sequence();
        _transitionSequence.SetUpdate(true);

        _transitionSequence.Append(
            _crackImage
                .DOFade(
                    1f,
                    _crackShowDuration)
                .SetEase(Ease.OutQuad));

        _transitionSequence.AppendInterval(
            _freezeDuration);

        _transitionSequence.AppendCallback(
            ShowShards);

        _transitionSequence.Append(
            _flashImage
                .DOFade(
                    1f,
                    _flashInDuration)
                .SetEase(Ease.OutQuad));

        AppendPullBackAnimation();

        _transitionSequence.Append(
            _flashImage
                .DOFade(
                    0f,
                    _flashOutDuration)
                .SetEase(Ease.OutQuad));

        _transitionSequence.AppendCallback(
            HideCrack);

        AppendSuctionAnimation();

        _transitionSequence.OnComplete(
            HandleBlackoutReached);
    }

    /// <summary>
    /// 카메라 후퇴 연출 추가
    /// </summary>
    private void AppendPullBackAnimation()
    {
        if (_cameraTransform == null)
        {
            _transitionSequence.AppendInterval(
                _pullBackDuration);

            return;
        }

        Vector3 pullBackPosition =
            _cameraStartPosition -
            (_cameraStartRotation *
             Vector3.forward) *
            _pullBackDistance;

        Quaternion pullBackRotation =
            _cameraStartRotation *
            Quaternion.Euler(
                0f,
                0f,
                _pullBackRoll);

        _transitionSequence.Append(
            _cameraTransform
                .DOMove(
                    pullBackPosition,
                    _pullBackDuration)
                .SetEase(Ease.OutCubic));

        _transitionSequence.Join(
            _cameraTransform
                .DORotateQuaternion(
                    pullBackRotation,
                    _pullBackDuration)
                .SetEase(Ease.OutCubic));
    }

    /// <summary>
    /// 화면 흡입 연출 추가
    /// </summary>
    private void AppendSuctionAnimation()
    {
        float suctionStartTime =
            _transitionSequence.Duration();

        if (_cameraTransform != null)
        {
            Vector3 suctionPosition =
                _cameraStartPosition +
                (_cameraStartRotation *
                 Vector3.forward) *
                _suctionDistance;

            _transitionSequence.Append(
                _cameraTransform
                    .DOMove(
                        suctionPosition,
                        _suctionDuration)
                    .SetEase(Ease.InCubic));

            _transitionSequence.Join(
                _cameraTransform
                    .DORotateQuaternion(
                        _cameraStartRotation,
                        _suctionDuration)
                    .SetEase(Ease.InCubic));
        }
        else
        {
            _transitionSequence.AppendInterval(
                _suctionDuration);
        }

        if (_mainCamera != null)
        {
            _transitionSequence.Join(
                DOTween.To(
                        () => _mainCamera.fieldOfView,
                        value =>
                            _mainCamera.fieldOfView = value,
                        _suctionFov,
                        _suctionDuration)
                    .SetEase(Ease.InCubic));
        }

        AppendShardAnimations();

        float blackoutStartTime =
            suctionStartTime +
            Mathf.Max(
                0f,
                _suctionDuration -
                _blackoutDuration);

        _transitionSequence.Insert(
            blackoutStartTime,
            _blackoutImage
                .DOFade(
                    1f,
                    _blackoutDuration)
                .SetEase(Ease.InQuad));
    }

    /// <summary>
    /// 유리 조각 이탈 연출 추가
    /// </summary>
    private void AppendShardAnimations()
    {
        for (int i = 0; i < _shards.Count; i++)
        {
            RectTransform shard = _shards[i];

            if (shard == null ||
                i >= _shardStartPositions.Count)
            {
                continue;
            }

            Vector2 direction =
                _shardStartPositions[i];

            if (direction.sqrMagnitude <= 0.001f)
            {
                float angle =
                    360f / Mathf.Max(
                        1,
                        _shards.Count) * i;

                direction =
                    new Vector2(
                        Mathf.Cos(
                            angle *
                            Mathf.Deg2Rad),
                        Mathf.Sin(
                            angle *
                            Mathf.Deg2Rad));
            }

            direction.Normalize();

            Vector2 targetPosition =
                _shardStartPositions[i] +
                direction *
                _shardFlyDistance;

            float rotationDirection =
                i % 2 == 0
                    ? 1f
                    : -1f;

            _transitionSequence.Join(
                shard
                    .DOAnchorPos(
                        targetPosition,
                        _suctionDuration)
                    .SetEase(Ease.InCubic));

            _transitionSequence.Join(
                shard
                    .DORotate(
                        new Vector3(
                            0f,
                            0f,
                            _shardRotation *
                            rotationDirection),
                        _suctionDuration,
                        RotateMode.FastBeyond360)
                    .SetRelative()
                    .SetEase(Ease.InCubic));

            _transitionSequence.Join(
                shard
                    .DOScale(
                        _shardStartScales[i] *
                        _shardEndScale,
                        _suctionDuration)
                    .SetEase(Ease.InCubic));
        }
    }

    /// <summary>
    /// 필드 참조 검색
    /// </summary>
    private void FindFieldReferences()
    {
        _mainCamera = Camera.main;

        if (_mainCamera != null)
        {
            _cameraTransform =
                _mainCamera.transform;

            _cinemachineBrain =
                _mainCamera.GetComponent<
                    CinemachineBrain>();
        }

        _cameraInputController =
            FindFirstObjectByType<
                CinemachineInputAxisController>();

        _playerController =
            FindFirstObjectByType<
                PlayerController>();
    }

    /// <summary>
    /// 카메라 시작 상태 저장
    /// </summary>
    private void CacheCameraState()
    {
        if (_mainCamera == null ||
            _cameraTransform == null)
        {
            return;
        }

        _cameraStartPosition =
            _cameraTransform.position;

        _cameraStartRotation =
            _cameraTransform.rotation;

        _cameraStartFov =
            _mainCamera.fieldOfView;
    }

    /// <summary>
    /// 필드 조작 잠금
    /// </summary>
    private void LockFieldControl()
    {
        if (_playerController != null)
        {
            _playerController.SetInputEnabled(
                false);
        }

        if (_cameraInputController != null)
        {
            _cameraInputWasEnabled =
                _cameraInputController.enabled;

            _cameraInputController.enabled =
                false;
        }

        if (_cinemachineBrain != null)
        {
            _brainWasEnabled =
                _cinemachineBrain.enabled;

            _cinemachineBrain.enabled =
                false;
        }
    }

    /// <summary>
    /// 필드 조작 복구
    /// </summary>
    private void RestoreFieldControl()
    {
        if (_cameraTransform != null)
        {
            _cameraTransform.SetPositionAndRotation(
                _cameraStartPosition,
                _cameraStartRotation);
        }

        if (_mainCamera != null)
        {
            _mainCamera.fieldOfView =
                _cameraStartFov;
        }

        if (_cinemachineBrain != null)
        {
            _cinemachineBrain.enabled =
                _brainWasEnabled;
        }

        if (_cameraInputController != null)
        {
            _cameraInputController.enabled =
                _cameraInputWasEnabled;
        }

        if (_playerController != null)
        {
            _playerController.SetInputEnabled(
                true);
        }
    }

    /// <summary>
    /// 시간 배율 복구
    /// </summary>
    private void RestoreTimeScale()
    {
        Time.timeScale =
            _previousTimeScale;
    }

    /// <summary>
    /// 연출 시작 시각 상태 설정
    /// </summary>
    private void PrepareVisuals()
    {
        if (_rootCanvasGroup != null)
        {
            _rootCanvasGroup.alpha = 1f;
            _rootCanvasGroup.interactable = false;
            _rootCanvasGroup.blocksRaycasts = true;
        }

        SetImageAlpha(
            _crackImage,
            0f);

        SetImageAlpha(
            _flashImage,
            0f);

        SetImageAlpha(
            _blackoutImage,
            0f);

        if (_crackRoot != null)
        {
            _crackRoot.localScale =
                Vector3.one;
        }

        ResetShards();
    }

    /// <summary>
    /// 유리 조각 표시
    /// </summary>
    private void ShowShards()
    {
        for (int i = 0; i < _shards.Count; i++)
        {
            RectTransform shard =
                _shards[i];

            if (shard == null)
            {
                continue;
            }

            Image shardImage =
                shard.GetComponent<Image>();

            SetImageAlpha(
                shardImage,
                1f);
        }
    }

    /// <summary>
    /// 균열 이미지 숨김
    /// </summary>
    private void HideCrack()
    {
        SetImageAlpha(
            _crackImage,
            0f);
    }

    /// <summary>
    /// 유리 조각 시작 Transform 저장
    /// </summary>
    private void CacheShardTransforms()
    {
        _shardStartPositions.Clear();
        _shardStartRotations.Clear();
        _shardStartScales.Clear();

        for (int i = 0; i < _shards.Count; i++)
        {
            RectTransform shard =
                _shards[i];

            if (shard == null)
            {
                _shardStartPositions.Add(
                    Vector2.zero);

                _shardStartRotations.Add(
                    Quaternion.identity);

                _shardStartScales.Add(
                    Vector3.one);

                continue;
            }

            _shardStartPositions.Add(
                shard.anchoredPosition);

            _shardStartRotations.Add(
                shard.localRotation);

            _shardStartScales.Add(
                shard.localScale);
        }
    }

    /// <summary>
    /// 유리 조각 시작 상태 복구
    /// </summary>
    private void ResetShards()
    {
        for (int i = 0; i < _shards.Count; i++)
        {
            RectTransform shard =
                _shards[i];

            if (shard == null ||
                i >= _shardStartPositions.Count)
            {
                continue;
            }

            shard.DOKill();

            shard.anchoredPosition =
                _shardStartPositions[i];

            shard.localRotation =
                _shardStartRotations[i];

            shard.localScale =
                _shardStartScales[i];

            SetImageAlpha(
                shard.GetComponent<Image>(),
                0f);
        }
    }

    /// <summary>
    /// 검은 화면 완료 처리
    /// </summary>
    private void HandleBlackoutReached()
    {
        RestoreTimeScale();
        RestoreFieldControl();

        _onBlackoutReached?.Invoke();
        _onBlackoutReached = null;
    }

    /// <summary>
    /// UI 즉시 숨김
    /// </summary>
    private void HideImmediate()
    {
        _transitionSequence?.Kill();
        _transitionSequence = null;

        if (_rootCanvasGroup != null)
        {
            _rootCanvasGroup.alpha = 0f;
            _rootCanvasGroup.interactable = false;
            _rootCanvasGroup.blocksRaycasts = false;
        }

        SetImageAlpha(
            _crackImage,
            0f);

        SetImageAlpha(
            _flashImage,
            0f);

        SetImageAlpha(
            _blackoutImage,
            0f);

        ResetShards();
    }

    /// <summary>
    /// 이미지 Alpha 설정
    /// </summary>
    /// <param name="image">대상 이미지</param>
    /// <param name="alpha">Alpha 값</param>
    private void SetImageAlpha(
        Image image,
        float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color =
            image.color;

        color.a = alpha;
        image.color = color;
    }
}