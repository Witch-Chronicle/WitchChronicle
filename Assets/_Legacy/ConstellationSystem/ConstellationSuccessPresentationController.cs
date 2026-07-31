using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 별자리 패리 성공 화면 연출
/// 화면 섬광, 균열, 반사 파동, 별자리 잔상 관리
/// </summary>
public class ConstellationSuccessPresentationController :
    MonoBehaviour
{
    [Header("Constellation")]
    [SerializeField]
    private RectTransform _constellationVisualRoot;

    [SerializeField]
    private CanvasGroup _constellationVisualCanvasGroup;

    [Header("Screen Effect")]
    [SerializeField]
    private Image _screenFlashImage;

    [SerializeField]
    private Image _crackImage;

    [SerializeField]
    private Image _deflectWaveImage;

    [SerializeField]
    private Image _deflectCoreImage;

    [Header("Screen Flash")]
    [SerializeField, Min(0f)]
    private float _screenFlashDuration = 0.12f;

    [SerializeField, Range(0f, 1f)]
    private float _screenFlashMaxAlpha = 0.8f;

    [Header("Crack")]
    [SerializeField, Min(0f)]
    private float _crackFadeInDuration = 0.04f;

    [SerializeField, Min(0f)]
    private float _crackDuration = 0.34f;

    [SerializeField, Range(0f, 1f)]
    private float _crackMaxAlpha = 0.85f;

    [SerializeField, Min(0.1f)]
    private float _crackStartScale = 1.08f;

    [SerializeField, Min(0.1f)]
    private float _crackEndScale = 1f;

    [Header("Deflect Wave")]
    [SerializeField, Min(0f)]
    private float _deflectWaveDuration = 0.24f;

    [SerializeField, Min(0.01f)]
    private float _deflectWaveStartScale = 0.25f;

    [SerializeField, Min(0.01f)]
    private float _deflectWaveEndScale = 2.5f;

    [SerializeField, Range(0f, 1f)]
    private float _deflectWaveMaxAlpha = 0.9f;

    [SerializeField]
    private float _deflectWaveRotation = 20f;

    [Header("Deflect Core")]
    [SerializeField, Min(0f)]
    private float _deflectCoreDuration = 0.14f;

    [SerializeField, Min(0.01f)]
    private float _deflectCoreStartScale = 0.5f;

    [SerializeField, Min(0.01f)]
    private float _deflectCoreEndScale = 1.5f;

    [SerializeField, Range(0f, 1f)]
    private float _deflectCoreMaxAlpha = 1f;

    [Header("Constellation Afterimage")]
    [SerializeField, Min(0f)]
    private float _afterimageHoldDuration = 0.08f;

    [SerializeField, Min(0f)]
    private float _afterimageFadeDuration = 0.34f;

    [SerializeField, Min(1f)]
    private float _afterimageEndScale = 1.08f;

    private Coroutine _presentationRoutine;

    private Vector3 _visualBaseScale =
        Vector3.one;

    private Vector3 _crackBaseScale =
        Vector3.one;

    private Vector3 _deflectWaveBaseScale =
        Vector3.one;

    private Vector3 _deflectCoreBaseScale =
        Vector3.one;

    public bool IsPlaying =>
        _presentationRoutine != null;

    /// <summary>
    /// 기본 Transform 상태 저장과 초기화
    /// </summary>
    private void Awake()
    {
        if (_constellationVisualRoot != null)
        {
            _visualBaseScale =
                _constellationVisualRoot.localScale;
        }

        if (_crackImage != null)
        {
            _crackBaseScale =
                _crackImage.rectTransform.localScale;
        }

        if (_deflectWaveImage != null)
        {
            _deflectWaveBaseScale =
                _deflectWaveImage.rectTransform.localScale;
        }

        if (_deflectCoreImage != null)
        {
            _deflectCoreBaseScale =
                _deflectCoreImage.rectTransform.localScale;
        }

        ResetPresentation();
    }

    /// <summary>
    /// 비활성화 시 연출 초기화
    /// </summary>
    private void OnDisable()
    {
        ResetPresentation();
    }

    /// <summary>
    /// 패리 성공 화면 연출 시작
    /// </summary>
    public void PlaySuccessPresentation()
    {
        StopPresentationRoutine();
        PreparePresentation();

        _presentationRoutine =
            StartCoroutine(
                PlaySuccessPresentationRoutine());
    }

    /// <summary>
    /// 성공 화면 연출 초기화
    /// </summary>
    public void ResetPresentation()
    {
        StopPresentationRoutine();

        if (_constellationVisualRoot != null)
        {
            _constellationVisualRoot.localScale =
                _visualBaseScale;
        }

        if (_constellationVisualCanvasGroup != null)
        {
            _constellationVisualCanvasGroup.alpha = 1f;

            // 별자리 진행 중 별 클릭 허용
            _constellationVisualCanvasGroup.interactable = true;
            _constellationVisualCanvasGroup.blocksRaycasts = true;
        }

        ResetScreenFlash();
        ResetCrack();
        ResetDeflectWave();
        ResetDeflectCore();
    }

    /// <summary>
    /// 성공 연출 시작 상태 설정
    /// </summary>
    private void PreparePresentation()
    {
        if (_constellationVisualRoot != null)
        {
            _constellationVisualRoot.localScale =
                _visualBaseScale;
        }

        if (_constellationVisualCanvasGroup != null)
        {
            _constellationVisualCanvasGroup.alpha = 1f;

            // 모든 별 판정 완료 후 추가 입력 차단
            _constellationVisualCanvasGroup.interactable = false;
            _constellationVisualCanvasGroup.blocksRaycasts = false;
        }

        ResetScreenFlash();

        if (_crackImage != null)
        {
            _crackImage.rectTransform.localScale =
                _crackBaseScale *
                _crackStartScale;

            SetImageAlpha(
                _crackImage,
                0f);
        }

        if (_deflectWaveImage != null)
        {
            _deflectWaveImage.rectTransform.localScale =
                _deflectWaveBaseScale *
                _deflectWaveStartScale;

            _deflectWaveImage.rectTransform.localRotation =
                Quaternion.identity;

            SetImageAlpha(
                _deflectWaveImage,
                0f);
        }

        if (_deflectCoreImage != null)
        {
            _deflectCoreImage.rectTransform.localScale =
                _deflectCoreBaseScale *
                _deflectCoreStartScale;

            SetImageAlpha(
                _deflectCoreImage,
                _deflectCoreMaxAlpha);
        }
    }

    /// <summary>
    /// 패리 성공 화면 연출 진행
    /// </summary>
    private IEnumerator PlaySuccessPresentationRoutine()
    {
        float afterimageDuration =
            _afterimageHoldDuration +
            _afterimageFadeDuration;

        float totalDuration =
            Mathf.Max(
                _screenFlashDuration,
                _crackDuration,
                _deflectWaveDuration,
                _deflectCoreDuration,
                afterimageDuration);

        float elapsedTime = 0f;

        while (elapsedTime < totalDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            UpdateScreenFlash(
                elapsedTime);

            UpdateCrack(
                elapsedTime);

            UpdateDeflectWave(
                elapsedTime);

            UpdateDeflectCore(
                elapsedTime);

            UpdateConstellationAfterimage(
                elapsedTime);

            yield return null;
        }

        ResetScreenFlash();
        ResetCrack();
        ResetDeflectWave();
        ResetDeflectCore();

        if (_constellationVisualCanvasGroup != null)
        {
            _constellationVisualCanvasGroup.alpha = 0f;
        }

        _presentationRoutine = null;
    }

    /// <summary>
    /// 화면 섬광 갱신
    /// </summary>
    /// <param name="elapsedTime">연출 경과 시간</param>
    private void UpdateScreenFlash(
        float elapsedTime)
    {
        if (_screenFlashImage == null)
        {
            return;
        }

        if (_screenFlashDuration <= 0f ||
            elapsedTime >= _screenFlashDuration)
        {
            SetImageAlpha(
                _screenFlashImage,
                0f);

            return;
        }

        float progress =
            Mathf.Clamp01(
                elapsedTime /
                _screenFlashDuration);

        float flashValue =
            Mathf.Sin(
                progress *
                Mathf.PI);

        SetImageAlpha(
            _screenFlashImage,
            flashValue *
            _screenFlashMaxAlpha);
    }

    /// <summary>
    /// 화면 균열 갱신
    /// </summary>
    /// <param name="elapsedTime">연출 경과 시간</param>
    private void UpdateCrack(
        float elapsedTime)
    {
        if (_crackImage == null)
        {
            return;
        }

        if (_crackDuration <= 0f ||
            elapsedTime >= _crackDuration)
        {
            SetImageAlpha(
                _crackImage,
                0f);

            return;
        }

        float durationProgress =
            Mathf.Clamp01(
                elapsedTime /
                _crackDuration);

        float alpha;

        if (_crackFadeInDuration > 0f &&
            elapsedTime <
            _crackFadeInDuration)
        {
            alpha =
                elapsedTime /
                _crackFadeInDuration;
        }
        else
        {
            float fadeStartTime =
                Mathf.Max(
                    0f,
                    _crackFadeInDuration);

            float fadeDuration =
                Mathf.Max(
                    0.01f,
                    _crackDuration -
                    fadeStartTime);

            float fadeProgress =
                Mathf.Clamp01(
                    (elapsedTime -
                     fadeStartTime) /
                    fadeDuration);

            alpha =
                1f -
                fadeProgress;
        }

        SetImageAlpha(
            _crackImage,
            alpha *
            _crackMaxAlpha);

        float crackScale =
            Mathf.Lerp(
                _crackStartScale,
                _crackEndScale,
                EaseOutCubic(
                    durationProgress));

        _crackImage.rectTransform.localScale =
            _crackBaseScale *
            crackScale;
    }

    /// <summary>
    /// 마법 반사 파동 갱신
    /// </summary>
    /// <param name="elapsedTime">연출 경과 시간</param>
    private void UpdateDeflectWave(
        float elapsedTime)
    {
        if (_deflectWaveImage == null)
        {
            return;
        }

        if (_deflectWaveDuration <= 0f ||
            elapsedTime >= _deflectWaveDuration)
        {
            SetImageAlpha(
                _deflectWaveImage,
                0f);

            return;
        }

        float progress =
            Mathf.Clamp01(
                elapsedTime /
                _deflectWaveDuration);

        float easedProgress =
            EaseOutCubic(
                progress);

        float currentScale =
            Mathf.Lerp(
                _deflectWaveStartScale,
                _deflectWaveEndScale,
                easedProgress);

        _deflectWaveImage.rectTransform.localScale =
            _deflectWaveBaseScale *
            currentScale;

        _deflectWaveImage.rectTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                _deflectWaveRotation *
                easedProgress);

        float alpha =
            (1f - progress) *
            _deflectWaveMaxAlpha;

        SetImageAlpha(
            _deflectWaveImage,
            alpha);
    }

    /// <summary>
    /// 마법 반사 중심광 갱신
    /// </summary>
    /// <param name="elapsedTime">연출 경과 시간</param>
    private void UpdateDeflectCore(
        float elapsedTime)
    {
        if (_deflectCoreImage == null)
        {
            return;
        }

        if (_deflectCoreDuration <= 0f ||
            elapsedTime >= _deflectCoreDuration)
        {
            SetImageAlpha(
                _deflectCoreImage,
                0f);

            return;
        }

        float progress =
            Mathf.Clamp01(
                elapsedTime /
                _deflectCoreDuration);

        float easedProgress =
            EaseOutCubic(
                progress);

        float currentScale =
            Mathf.Lerp(
                _deflectCoreStartScale,
                _deflectCoreEndScale,
                easedProgress);

        _deflectCoreImage.rectTransform.localScale =
            _deflectCoreBaseScale *
            currentScale;

        float alpha =
            (1f - progress) *
            _deflectCoreMaxAlpha;

        SetImageAlpha(
            _deflectCoreImage,
            alpha);
    }

    /// <summary>
    /// 별자리 잔상 갱신
    /// </summary>
    /// <param name="elapsedTime">연출 경과 시간</param>
    private void UpdateConstellationAfterimage(
        float elapsedTime)
    {
        if (_constellationVisualRoot == null ||
            _constellationVisualCanvasGroup == null)
        {
            return;
        }

        if (elapsedTime <
            _afterimageHoldDuration)
        {
            _constellationVisualCanvasGroup.alpha =
                1f;

            _constellationVisualRoot.localScale =
                _visualBaseScale;

            return;
        }

        if (_afterimageFadeDuration <= 0f)
        {
            _constellationVisualCanvasGroup.alpha =
                0f;

            return;
        }

        float fadeProgress =
            Mathf.Clamp01(
                (elapsedTime -
                 _afterimageHoldDuration) /
                _afterimageFadeDuration);

        float easedProgress =
            EaseOutCubic(
                fadeProgress);

        _constellationVisualCanvasGroup.alpha =
            1f -
            easedProgress;

        float currentScale =
            Mathf.Lerp(
                1f,
                _afterimageEndScale,
                easedProgress);

        _constellationVisualRoot.localScale =
            _visualBaseScale *
            currentScale;
    }

    /// <summary>
    /// 화면 섬광 초기화
    /// </summary>
    private void ResetScreenFlash()
    {
        if (_screenFlashImage == null)
        {
            return;
        }

        SetImageAlpha(
            _screenFlashImage,
            0f);
    }

    /// <summary>
    /// 화면 균열 초기화
    /// </summary>
    private void ResetCrack()
    {
        if (_crackImage == null)
        {
            return;
        }

        _crackImage.rectTransform.localScale =
            _crackBaseScale;

        SetImageAlpha(
            _crackImage,
            0f);
    }

    /// <summary>
    /// 반사 파동 초기화
    /// </summary>
    private void ResetDeflectWave()
    {
        if (_deflectWaveImage == null)
        {
            return;
        }

        _deflectWaveImage.rectTransform.localScale =
            _deflectWaveBaseScale;

        _deflectWaveImage.rectTransform.localRotation =
            Quaternion.identity;

        SetImageAlpha(
            _deflectWaveImage,
            0f);
    }

    /// <summary>
    /// 반사 중심광 초기화
    /// </summary>
    private void ResetDeflectCore()
    {
        if (_deflectCoreImage == null)
        {
            return;
        }

        _deflectCoreImage.rectTransform.localScale =
            _deflectCoreBaseScale;

        SetImageAlpha(
            _deflectCoreImage,
            0f);
    }

    /// <summary>
    /// 이미지 투명도 변경
    /// </summary>
    /// <param name="image">대상 이미지</param>
    /// <param name="alpha">투명도</param>
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

        color.a =
            Mathf.Clamp01(alpha);

        image.color =
            color;
    }

    /// <summary>
    /// Ease Out Cubic 값 반환
    /// </summary>
    /// <param name="progress">진행도</param>
    /// <returns>보간 결과</returns>
    private float EaseOutCubic(
        float progress)
    {
        float inverseProgress =
            1f -
            Mathf.Clamp01(progress);

        return
            1f -
            inverseProgress *
            inverseProgress *
            inverseProgress;
    }

    /// <summary>
    /// 현재 화면 연출 코루틴 정지
    /// </summary>
    private void StopPresentationRoutine()
    {
        if (_presentationRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _presentationRoutine);

        _presentationRoutine = null;
    }
}