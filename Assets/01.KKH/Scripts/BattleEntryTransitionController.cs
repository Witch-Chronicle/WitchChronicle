using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

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
    [SerializeField] private Image _blackoutImage;
    [SerializeField] private Volume _entryVolume;

    [Header("Sound")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _suctionSfx;
    [SerializeField, Range(0f, 1f)] private float _suctionSfxVolume = 1f;
    [SerializeField, Range(0.5f, 2f)] private float _suctionSfxPitch = 1f;

    [Header("Hit Stop")]
    [Tooltip("전투 진입 직후 화면이 정지된 채 유지되는 시간")]
    [SerializeField] private float _hitStopDuration = 0.1f;

    [Header("Slow Push In")]
    [Tooltip("카메라가 천천히 접근하는 시간")]
    [SerializeField] private float _pushInDuration = 0.55f;
    [Tooltip("카메라가 천천히 접근하는 거리")]
    [SerializeField] private float _pushInDistance = 1.2f;
    [Tooltip("천천히 접근한 뒤의 FOV")]
    [SerializeField] private float _pushInFov = 52f;

    [Header("Suction")]
    [Tooltip("화면 안으로 빨려 들어가는 시간")]
    [SerializeField] private float _suctionDuration = 0.5f;
    [Tooltip("카메라 전진 거리")]
    [SerializeField] private float _suctionDistance = 8f;
    [Tooltip("빨려 들어갈 때 최종 FOV")]
    [SerializeField] private float _suctionFov = 90f;

    [Header("Screen Effect")]
    [SerializeField] private float _blackoutDuration = 0.18f;
    [SerializeField] private float _revealDuration = 0.3f;

    [Header("Post Processing")]
    [Tooltip("느린 접근 종료 시 후처리 강도")]
    [Range(0f, 1f)]
    [SerializeField] private float _pushInEffectWeight = 0.25f;

    [Tooltip("흡입 종료 시 후처리 강도")]
    [Range(0f, 1f)]
    [SerializeField] private float _suctionEffectWeight = 1f;

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

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

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

        if (_hitStopDuration > 0f)
        {
            _transitionSequence.AppendInterval(
                _hitStopDuration);
        }

        AppendSlowPushInAnimation();
        AppendSuctionAnimation();

        _transitionSequence.OnComplete(
            HandleBlackoutReached);
    }

    /// <summary>
    /// 카메라 느린 접근 연출 추가
    /// </summary>
    private void AppendSlowPushInAnimation()
    {
        if (_cameraTransform == null)
        {
            _transitionSequence.AppendInterval(
                _pushInDuration);

            return;
        }

        Vector3 pushInPosition =
            _cameraStartPosition +
            (_cameraStartRotation *
             Vector3.forward) *
            _pushInDistance;

        _transitionSequence.Append(
            _cameraTransform
                .DOMove(
                    pushInPosition,
                    _pushInDuration)
                .SetEase(Ease.OutSine));

        if (_mainCamera != null)
        {
            _transitionSequence.Join(
                DOTween.To(
                        () => _mainCamera.fieldOfView,
                        value =>
                            _mainCamera.fieldOfView = value,
                        _pushInFov,
                        _pushInDuration)
                    .SetEase(Ease.OutSine));
        }

        if (_entryVolume != null)
        {
            _transitionSequence.Join(
                DOTween.To(
                        () => _entryVolume.weight,
                        value =>
                            _entryVolume.weight = value,
                        _pushInEffectWeight,
                        _pushInDuration)
                    .SetEase(Ease.InOutSine));
        }
    }

    /// <summary>
    /// 화면 흡입 연출 추가
    /// </summary>
    private void AppendSuctionAnimation()
    {
        float suctionStartTime = _transitionSequence.Duration();

        _transitionSequence.AppendCallback(PlaySuctionSfx);

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
                    .SetEase(Ease.InExpo));
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
                    .SetEase(Ease.InExpo));
        }

        if (_entryVolume != null)
        {
            _transitionSequence.Join(
                DOTween.To(
                        () => _entryVolume.weight,
                        value =>
                            _entryVolume.weight = value,
                        _suctionEffectWeight,
                        _suctionDuration)
                    .SetEase(Ease.InExpo));
        }

        float blackoutStartTime =
            suctionStartTime +
            Mathf.Max(
                0f,
                _suctionDuration -
                _blackoutDuration);

        if (_blackoutImage != null)
        {
            _transitionSequence.Insert(
                blackoutStartTime,
                _blackoutImage
                    .DOFade(
                        1f,
                        _blackoutDuration)
                    .SetEase(Ease.InQuad));
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
            FindFirstObjectByType<CinemachineInputAxisController>();

        _playerController =
            FindFirstObjectByType<PlayerController>();
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

        SetImageAlpha(_blackoutImage, 0f);
        SetEntryVolumeWeight(0f);
    }

    /// <summary>
    /// 검은 화면 완료 처리
    /// </summary>
    private void HandleBlackoutReached()
    {
        SetEntryVolumeWeight(0f);

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

        SetImageAlpha(_blackoutImage, 0f);
        SetEntryVolumeWeight(0f);
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

    /// <summary>
    /// 전투 진입 후처리 강도 설정
    /// </summary>
    /// <param name="weight">후처리 강도</param>
    private void SetEntryVolumeWeight(
        float weight)
    {
        if (_entryVolume == null)
        {
            return;
        }

        _entryVolume.weight =
            Mathf.Clamp01(weight);
    }

    /// <summary>
    /// 화면 흡입 사운드 재생
    /// </summary>
    private void PlaySuctionSfx()
    {
        if (_audioSource == null || _suctionSfx == null)
        {
            return;
        }

        _audioSource.pitch = _suctionSfxPitch;
        _audioSource.PlayOneShot(_suctionSfx, _suctionSfxVolume);
    }
}