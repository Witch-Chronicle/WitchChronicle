using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 강화 결과를 간단한 UI Image와 DOTween만으로 연출합니다.
/// 필요한 효과 Sprite: Glow 1장, MagicCircle 1장.
///
/// 흐름:
/// Overlay Fade In -> 빛 응축 -> 성공/실패 분기 -> 결과 텍스트 -> 클릭하여 닫기
/// </summary>
public sealed class EnhancementResultController : MonoBehaviour
{
    public static EnhancementResultController Instance { get; private set; }

    [Header("Overlay")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private Image dimmedBackground;
    [SerializeField] private Button overlayButton;

    [Header("Center Effect")]
    [SerializeField] private RectTransform effectRoot;
    [SerializeField] private RectTransform glowRect;
    [SerializeField] private Image glowImage;
    [SerializeField] private RectTransform magicCircleRect;
    [SerializeField] private Image magicCircleImage;
    [SerializeField] private RectTransform itemRoot;
    [SerializeField] private Image itemIcon;

    [Header("Result Text")]
    [SerializeField] private CanvasGroup resultTextGroup;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text continueGuideText;

    [Header("Colors")]
    [SerializeField] private Color successGlowColor = new Color32(233, 231, 221, 255);
    [SerializeField] private Color failureGlowColor = new Color32(135, 132, 140, 255);
    [SerializeField] private Color successTextColor = new Color32(233, 231, 221, 255);
    [SerializeField] private Color failureTextColor = new Color32(162, 160, 159, 255);
    [Tooltip("강화 실패 시 아이템 아이콘에 곱해지는 어두운 틴트 색상")]
    [SerializeField] private Color failureIconTintColor = new Color32(140, 140, 140, 255);

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float overlayFadeTime = 0.22f;
    [SerializeField, Min(0.1f)] private float gatheringTime = 0.9f;
    [SerializeField, Min(0.1f)] private float resultEffectTime = 0.55f;
    [SerializeField, Min(0.05f)] private float textFadeTime = 0.25f;

    public bool IsOpen => gameObject.activeSelf;
    public bool IsResultPresented => canClose;

    public event Action ResultPresented;
    public event Action Closed;

    private Sequence sequence;
    private bool canClose;
    private bool isClosing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (overlayButton != null)
            overlayButton.onClick.AddListener(OnOverlayClicked);

        // 처음부터 활성 상태여도 화면과 입력을 가리지 않습니다.
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        sequence?.Kill();

        if (overlayButton != null)
            overlayButton.onClick.RemoveListener(OnOverlayClicked);
    }

    /// <summary>
    /// 강화 결과 연출을 시작합니다.
    /// </summary>
    /// <param name="icon">강화한 장비의 아이콘 Sprite</param>
    /// <param name="isSuccess">강화 성공 여부</param>
    /// <param name="beforeLevel">강화 전 단계</param>
    /// <param name="afterLevel">강화 후 단계. 실패 시 beforeLevel과 동일하게 전달</param>
    /// <param name="pointBefore">실패 전 강화 포인트(%)</param>
    /// <param name="pointAfter">실패 후 강화 포인트(%)</param>
    public void Play(
        Sprite icon,
        bool isSuccess,
        int beforeLevel,
        int afterLevel,
        float pointBefore = 0f,
        float pointAfter = 0f)
    {
        gameObject.SetActive(true);
        KillCurrentTweens();
        ResetVisuals();
        SetResultData(icon, isSuccess, beforeLevel, afterLevel, pointBefore, pointAfter);

        // ★ 강화 결과 사운드 (성공/실패)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySfx(isSuccess ? SfxType.EnhanceSuccess : SfxType.EnhanceFail);
        }

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        // 1. 검은 Overlay와 장비 아이콘 등장
        sequence.Append(overlayGroup.DOFade(1f, overlayFadeTime));
        sequence.Join(itemIcon.DOFade(1f, 0.2f));
        sequence.Join(itemRoot.DOScale(1f, 0.32f).SetEase(Ease.OutBack));

        // 2. 마법진 등장
        sequence.Append(magicCircleImage.DOFade(0.72f, 0.18f));
        sequence.Join(magicCircleRect.DOScale(1f, 0.25f).SetEase(Ease.OutCubic));

        // 3. 바깥의 빛이 장비로 응축되는 느낌
        sequence.Append(glowImage.DOFade(0.9f, 0.18f));
        sequence.Join(glowRect.DOScale(0.28f, gatheringTime).SetEase(Ease.InCubic));
        sequence.Join(magicCircleRect
            .DORotate(new Vector3(0f, 0f, -150f), gatheringTime, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutSine));
        sequence.Join(magicCircleRect.DOScale(0.82f, gatheringTime).SetEase(Ease.InOutSine));

        // 4. 성공 또는 실패 결과 연출
        sequence.AppendCallback(() => ApplyResultColors(isSuccess));

        if (isSuccess)
            AppendSuccessAnimation(sequence);
        else
            AppendFailureAnimation(sequence);

        // 5. 결과 텍스트 표시
        sequence.Append(resultTextGroup.DOFade(1f, textFadeTime));
        sequence.Join(resultTextGroup.transform
            .DOScale(1f, textFadeTime + 0.08f)
            .SetEase(Ease.OutBack));
        sequence.AppendCallback(() =>
        {
            canClose = true;
            ResultPresented?.Invoke();
        });
    }

    /// <summary>
    /// 결과 화면을 닫습니다. 강화 UI 갱신은 Closed 이벤트에서 처리하면 됩니다.
    /// </summary>
    public void Close()
    {
        if (!gameObject.activeSelf || isClosing)
            return;

        if (!canClose)
        {
            CompleteAnimationImmediately();
            return;
        }

        isClosing = true;
        sequence?.Kill();

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .Append(resultTextGroup.DOFade(0f, 0.12f))
            .Join(effectRoot.DOScale(0.92f, 0.16f).SetEase(Ease.InCubic))
            .Append(overlayGroup.DOFade(0f, 0.18f))
            .OnComplete(() =>
            {
                overlayGroup.interactable = false;
                overlayGroup.blocksRaycasts = false;
                isClosing = false;
                gameObject.SetActive(false);
                Closed?.Invoke();
            });
    }

    /// <summary>
    /// 연출 도중 클릭했을 때 결과 상태까지 즉시 진행합니다.
    /// </summary>
    public void CompleteAnimationImmediately()
    {
        if (sequence == null || !sequence.IsActive())
            return;

        sequence.Complete(true);
    }

    private void OnOverlayClicked()
    {
        if (canClose)
            Close();
        else
            CompleteAnimationImmediately();
    }

    private void AppendSuccessAnimation(Sequence targetSequence)
    {
        targetSequence.Append(glowRect
            .DOScale(1.7f, resultEffectTime)
            .SetEase(Ease.OutExpo));

        targetSequence.Join(glowImage.DOFade(0.32f, resultEffectTime));
        targetSequence.Join(magicCircleRect.DOScale(1.18f, resultEffectTime));
        targetSequence.Join(magicCircleImage.DOFade(0.35f, resultEffectTime));

        targetSequence.Join(itemRoot
            .DOPunchScale(Vector3.one * 0.16f, 0.42f, 5, 0.45f));
    }

    private void AppendFailureAnimation(Sequence targetSequence)
    {
        targetSequence.Append(glowRect
            .DOScale(0.95f, resultEffectTime)
            .SetEase(Ease.OutCubic));
        targetSequence.Join(glowImage.DOFade(0.08f, resultEffectTime));
        targetSequence.Join(magicCircleImage.DOFade(0f, resultEffectTime * 0.75f));
        targetSequence.Join(itemIcon
            .DOColor(failureIconTintColor, resultEffectTime)
            .SetEase(Ease.OutCubic));
        targetSequence.Join(itemRoot
            .DOShakeAnchorPos(
                duration: 0.42f,
                strength: new Vector2(12f, 2f),
                vibrato: 11,
                randomness: 45f,
                snapping: false,
                fadeOut: true));
    }

    private void ApplyResultColors(bool isSuccess)
    {
        Color glowColor = isSuccess ? successGlowColor : failureGlowColor;
        glowColor.a = glowImage.color.a;
        glowImage.color = glowColor;

        Color circleColor = isSuccess ? successGlowColor : failureGlowColor;
        circleColor.a = magicCircleImage.color.a;
        magicCircleImage.color = circleColor;
    }

    private void SetResultData(
        Sprite icon,
        bool isSuccess,
        int beforeLevel,
        int afterLevel,
        float pointBefore,
        float pointAfter)
    {
        itemIcon.sprite = icon;
        itemIcon.enabled = icon != null;

        resultTitleText.text = isSuccess ? "강화 성공" : "강화 실패";
        resultTitleText.color = isSuccess ? successTextColor : failureTextColor;

        levelText.text = isSuccess
            ? $"+{beforeLevel}  →  +{afterLevel}"
            : $"+{beforeLevel}";

        descriptionText.text = isSuccess
            ? "장비에 새로운 마력이 깃들었습니다."
            : $"강화 단계가 유지되었습니다.\n강화 포인트  {pointBefore:0.#}%  →  {pointAfter:0.#}%";

        continueGuideText.text = "클릭하여 계속";
    }

    private void ResetVisuals()
    {
        canClose = false;
        isClosing = false;

        overlayGroup.alpha = 0f;
        overlayGroup.interactable = true;
        overlayGroup.blocksRaycasts = true;

        dimmedBackground.color = new Color(0f, 0f, 0f, 0.78f);

        effectRoot.localScale = Vector3.one;

        itemRoot.anchoredPosition = Vector2.zero;
        itemRoot.localScale = Vector3.one * 0.72f;
        itemIcon.color = Color.white;
        SetImageAlpha(itemIcon, 0f);

        glowRect.anchoredPosition = Vector2.zero;
        glowRect.localScale = Vector3.one * 1.85f;
        SetImageAlpha(glowImage, 0f);

        magicCircleRect.anchoredPosition = Vector2.zero;
        magicCircleRect.localScale = Vector3.one * 1.18f;
        magicCircleRect.localRotation = Quaternion.identity;
        SetImageAlpha(magicCircleImage, 0f);

        resultTextGroup.alpha = 0f;
        resultTextGroup.transform.localScale = Vector3.one * 0.88f;
    }

    private void KillCurrentTweens()
    {
        sequence?.Kill();
        sequence = null;

        DOTween.Kill(effectRoot);
        DOTween.Kill(glowRect);
        DOTween.Kill(glowImage);
        DOTween.Kill(magicCircleRect);
        DOTween.Kill(magicCircleImage);
        DOTween.Kill(itemRoot);
        DOTween.Kill(itemIcon);
        DOTween.Kill(resultTextGroup);
        DOTween.Kill(resultTextGroup.transform);
        DOTween.Kill(overlayGroup);
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}