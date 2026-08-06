using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마도서 사용 결과를 간단한 UI Image와 DOTween만으로 연출한다.
/// Dimmed FadeIn → 아이콘 감속 스핀 + 빛 응축 → 결과 확정 → 텍스트 표시.
///
/// 연출 중 배경 클릭: 결과까지 즉시 진행
/// 결과 표시 후 배경 클릭: Overlay 닫기
/// </summary>
public sealed class SkillGachaResultOverlayController : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private GameObject _overlayRoot;
    [SerializeField] private CanvasGroup _overlayGroup;
    [SerializeField] private Image _dimmedBackground;
    [SerializeField] private Button _dimmedBackgroundButton;

    [Header("Center Effect")]
    [SerializeField] private RectTransform _effectRoot;
    [SerializeField] private RectTransform _glowRect;
    [SerializeField] private Image _glowImage;
    [SerializeField] private RectTransform _magicCircleRect;
    [SerializeField] private Image _magicCircleImage;
    [SerializeField] private RectTransform _iconRoot;
    [SerializeField] private Image _spinIcon;
    [Tooltip("중앙 아이콘 슬롯의 테두리 Image")]
    [SerializeField] private Image _frameImage;

    [Header("Result Text")]
    [SerializeField] private CanvasGroup _resultTextGroup;
    [SerializeField] private TMP_Text _resultNameText;
    [SerializeField] private TMP_Text _resultTierText;
    [SerializeField] private TMP_Text _resultDescText;
    [SerializeField] private GameObject _duplicateRewardRoot;
    [SerializeField] private TMP_Text _duplicateText;
    [SerializeField] private TMP_Text _rewardGoldText;
    [SerializeField] private TMP_Text _continueGuideText;

    [Header("Colors")]
    [Tooltip("스핀 중 사용하는 차분한 아이보리색")]
    [SerializeField] private Color _spinningColor = new Color32(205, 201, 190, 255);
    [SerializeField] private Color _duplicateGlowColor = new Color32(226, 187, 101, 255);
    [SerializeField] private Color _normalTextColor = new Color32(232, 229, 219, 255);
    [SerializeField]
    private Color[] _tierColors =
    {
        new Color(1f, 0.85f, 0.3f),
        new Color(0.8f, 0.5f, 1f),
        new Color(0.4f, 0.7f, 1f),
        new Color(0.8f, 0.8f, 0.8f)
    };

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float _overlayFadeTime = 0.20f;
    [SerializeField, Min(0.1f)] private float _spinDuration = 1.35f;
    [SerializeField, Min(0.01f)] private float _fastInterval = 0.045f;
    [SerializeField, Min(0.05f)] private float _slowInterval = 0.22f;
    [SerializeField, Min(0.1f)] private float _gatheringTime = 1.35f;
    [SerializeField, Min(0.1f)] private float _resultEffectTime = 0.48f;
    [SerializeField, Min(0.05f)] private float _textFadeTime = 0.25f;

    public bool IsOpen { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool IsResultPresented => _canClose;

    public event Action ResultPresented;
    public event Action Closed;

    private readonly List<Sprite> _iconPool = new List<Sprite>();
    private Sequence _sequence;
    private Action _onClosed;
    private bool _canClose;
    private bool _isClosing;

    private void Awake()
    {
        if (_dimmedBackgroundButton != null)
        {
            _dimmedBackgroundButton.onClick.AddListener(OnOverlayClicked);
        }

        if (_overlayGroup != null)
        {
            _overlayGroup.alpha = 0f;
            _overlayGroup.interactable = false;
            _overlayGroup.blocksRaycasts = false;
        }

        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        KillCurrentTweens();

        if (_dimmedBackgroundButton != null)
        {
            _dimmedBackgroundButton.onClick.RemoveListener(OnOverlayClicked);
        }
    }

    /// <summary>
    /// 이미 확정된 마도서 결과의 연출을 시작한다.
    /// SkillBookResult.RolledSkill에는 신규/중복과 무관하게 실제 당첨 스킬이 들어 있어야 한다.
    /// </summary>
    public bool Play(SkillBookItemData book, SkillBookResult result, Action onClosed = null)
    {
        if (IsOpen || IsPlaying)
        {
            return false;
        }

        if (book == null || result.Success == false || result.RolledSkill == null)
        {
            Debug.LogWarning("[SkillGachaResultOverlay] 표시할 결과가 올바르지 않습니다.");
            return false;
        }

        _onClosed = onClosed;
        BuildIconPool(book);
        Open();
        ResetVisuals();
        SetSpinIcon(_iconPool.Count > 0 ? _iconPool[0] : result.RolledSkill.SkillIcon);
        SetResultData(result);
        BuildAnimation(result);
        return true;
    }

    public void Close()
    {
        if (IsOpen == false || _isClosing)
        {
            return;
        }

        if (_canClose == false)
        {
            CompleteAnimationImmediately();
            return;
        }

        _isClosing = true;
        _canClose = false;
        IsPlaying = false;
        _sequence?.Kill();

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (_resultTextGroup != null)
        {
            _sequence.Append(_resultTextGroup.DOFade(0f, 0.12f));
        }

        if (_effectRoot != null)
        {
            _sequence.Join(_effectRoot.DOScale(0.92f, 0.16f).SetEase(Ease.InCubic));
        }

        if (_overlayGroup != null)
        {
            _sequence.Append(_overlayGroup.DOFade(0f, 0.18f));
        }

        _sequence.OnComplete(() =>
        {
            IsOpen = false;
            _isClosing = false;

            if (_overlayGroup != null)
            {
                _overlayGroup.interactable = false;
                _overlayGroup.blocksRaycasts = false;
            }

            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(false);
            }

            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
            Closed?.Invoke();
        });
    }

    /// <summary>연출 도중 클릭했을 때 모든 콜백을 실행하며 결과 상태까지 즉시 진행한다.</summary>
    public void CompleteAnimationImmediately()
    {
        if (_sequence == null || _sequence.IsActive() == false)
        {
            return;
        }

        _sequence.Complete(true);
    }

    private void Open()
    {
        IsOpen = true;
        IsPlaying = true;
        _canClose = false;
        _isClosing = false;

        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(true);
            _overlayRoot.transform.SetAsLastSibling();
        }

        if (_overlayGroup != null)
        {
            _overlayGroup.interactable = true;
            _overlayGroup.blocksRaycasts = true;
        }
    }

    private void OnOverlayClicked()
    {
        if (_canClose)
        {
            Close();
        }
        else
        {
            CompleteAnimationImmediately();
        }
    }

    private void BuildIconPool(SkillBookItemData book)
    {
        _iconPool.Clear();

        if (book == null || book.CandidateSkills == null)
        {
            return;
        }

        for (int i = 0; i < book.CandidateSkills.Length; i++)
        {
            SkillData skill = book.CandidateSkills[i];

            if (skill != null && skill.SkillIcon != null)
            {
                _iconPool.Add(skill.SkillIcon);
            }
        }
    }

    private void BuildAnimation(SkillBookResult result)
    {
        Color tierColor = GetTierColor(result.RolledTier);
        Color resultEffectColor = result.IsDuplicate ? _duplicateGlowColor : tierColor;

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        // 1. Dimmed와 중앙 아이콘 등장
        if (_overlayGroup != null)
        {
            _sequence.Append(_overlayGroup.DOFade(1f, _overlayFadeTime));
        }
        else
        {
            _sequence.AppendInterval(_overlayFadeTime);
        }

        if (_spinIcon != null)
        {
            _sequence.Join(_spinIcon.DOFade(1f, 0.18f));
        }

        if (_iconRoot != null)
        {
            _sequence.Join(_iconRoot.DOScale(1f, 0.30f).SetEase(Ease.OutBack));
        }

        // 2. 마법진과 Glow 등장
        if (_magicCircleImage != null)
        {
            _sequence.Append(_magicCircleImage.DOFade(0.58f, 0.18f));
        }

        if (_magicCircleRect != null)
        {
            _sequence.Join(_magicCircleRect.DOScale(1f, 0.25f).SetEase(Ease.OutCubic));
        }

        if (_glowImage != null)
        {
            _sequence.Join(_glowImage.DOFade(0.72f, 0.18f));
        }

        // 3. 아이콘 감속 스핀. 색상은 바꾸지 않고 차분한 아이보리 Frame만 유지한다.
        Sequence spinSequence = CreateSpinSequence();
        _sequence.Append(spinSequence);

        if (_glowRect != null)
        {
            _sequence.Join(_glowRect
                .DOScale(0.38f, _gatheringTime)
                .SetEase(Ease.InCubic));
        }

        if (_magicCircleRect != null)
        {
            _sequence.Join(_magicCircleRect
                .DORotate(new Vector3(0f, 0f, -135f), _gatheringTime, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutSine));
            _sequence.Join(_magicCircleRect
                .DOScale(0.84f, _gatheringTime)
                .SetEase(Ease.InOutSine));
        }

        // 4. 실제 당첨 스킬에서 정지
        _sequence.AppendCallback(() => ApplyFinalResult(result, resultEffectColor));

        if (_frameImage != null)
        {
            _sequence.Append(_frameImage.DOColor(resultEffectColor, 0.12f));
        }

        if (_glowImage != null)
        {
            _sequence.Join(_glowImage.DOColor(WithAlpha(resultEffectColor, 0.9f), 0.12f));
        }

        if (_glowRect != null)
        {
            _sequence.Append(_glowRect
                .DOScale(1.55f, _resultEffectTime)
                .SetEase(Ease.OutExpo));
        }

        if (_glowImage != null)
        {
            _sequence.Join(_glowImage.DOFade(0.26f, _resultEffectTime));
        }

        if (_magicCircleRect != null)
        {
            _sequence.Join(_magicCircleRect.DOScale(1.12f, _resultEffectTime));
        }

        if (_magicCircleImage != null)
        {
            _sequence.Join(_magicCircleImage.DOFade(0.30f, _resultEffectTime));
        }

        if (_iconRoot != null)
        {
            _sequence.Join(_iconRoot
                .DOPunchScale(Vector3.one * 0.13f, 0.38f, 5, 0.45f));
        }

        // 5. 결과 텍스트 등장
        if (_resultTextGroup != null)
        {
            _sequence.Append(_resultTextGroup.DOFade(1f, _textFadeTime));
            _sequence.Join(_resultTextGroup.transform
                .DOScale(1f, _textFadeTime + 0.06f)
                .SetEase(Ease.OutBack));
        }

        _sequence.AppendCallback(() =>
        {
            IsPlaying = false;
            _canClose = true;
            ResultPresented?.Invoke();
        });
    }

    private Sequence CreateSpinSequence()
    {
        Sequence spin = DOTween.Sequence();

        if (_iconPool.Count == 0)
        {
            spin.AppendInterval(_spinDuration);
            return spin;
        }

        float elapsed = 0f;
        int index = 0;

        while (elapsed < _spinDuration)
        {
            Sprite icon = _iconPool[index % _iconPool.Count];
            spin.AppendCallback(() => SetSpinIcon(icon));

            float progress = _spinDuration <= 0f ? 1f : elapsed / _spinDuration;
            float interval = Mathf.Lerp(_fastInterval, _slowInterval, progress * progress);
            interval = Mathf.Min(interval, _spinDuration - elapsed);

            spin.AppendInterval(Mathf.Max(0.001f, interval));
            elapsed += interval;
            index++;
        }

        return spin;
    }

    private void ApplyFinalResult(SkillBookResult result, Color effectColor)
    {
        SetSpinIcon(result.RolledSkill.SkillIcon);

        if (_spinIcon != null)
        {
            // 스킬 아이콘 자체는 티어색을 곱하지 않고 원본 색상을 유지한다.
            _spinIcon.color = Color.white;
        }

        if (_frameImage != null)
        {
            _frameImage.color = effectColor;
        }
    }

    private void SetResultData(SkillBookResult result)
    {
        SkillData rolledSkill = result.RolledSkill;
        Color tierColor = GetTierColor(result.RolledTier);

        if (_resultNameText != null)
        {
            _resultNameText.text = rolledSkill.SkillName;
            _resultNameText.color = tierColor;
        }

        if (_resultTierText != null)
        {
            _resultTierText.text = $"Tier {result.RolledTier}";
            _resultTierText.color = tierColor;
        }

        if (_resultDescText != null)
        {
            _resultDescText.text = rolledSkill.Description;
            _resultDescText.color = _normalTextColor;
        }

        if (_duplicateRewardRoot != null)
        {
            _duplicateRewardRoot.SetActive(result.IsDuplicate);
        }

        if (_duplicateText != null)
        {
            _duplicateText.text = "이미 보유한 스킬입니다";
        }

        if (_rewardGoldText != null)
        {
            _rewardGoldText.text = $"+{result.RewardGold:N0} G";
            _rewardGoldText.color = _duplicateGlowColor;
        }

        if (_continueGuideText != null)
        {
            _continueGuideText.text = "클릭하여 계속";
        }
    }

    private void ResetVisuals()
    {
        KillCurrentTweens();

        _canClose = false;
        _isClosing = false;
        IsPlaying = true;

        if (_overlayGroup != null)
        {
            _overlayGroup.alpha = 0f;
            _overlayGroup.interactable = true;
            _overlayGroup.blocksRaycasts = true;
        }

        if (_dimmedBackground != null)
        {
            _dimmedBackground.color = new Color(0f, 0f, 0f, 0.78f);
        }

        if (_effectRoot != null)
        {
            _effectRoot.localScale = Vector3.one;
        }

        if (_iconRoot != null)
        {
            _iconRoot.anchoredPosition = Vector2.zero;
            _iconRoot.localScale = Vector3.one * 0.72f;
        }

        if (_spinIcon != null)
        {
            _spinIcon.color = Color.white;
            SetImageAlpha(_spinIcon, 0f);
        }

        if (_frameImage != null)
        {
            _frameImage.color = _spinningColor;
        }

        if (_glowRect != null)
        {
            _glowRect.anchoredPosition = Vector2.zero;
            _glowRect.localScale = Vector3.one * 1.75f;
        }

        if (_glowImage != null)
        {
            _glowImage.color = _spinningColor;
            SetImageAlpha(_glowImage, 0f);
        }

        if (_magicCircleRect != null)
        {
            _magicCircleRect.anchoredPosition = Vector2.zero;
            _magicCircleRect.localScale = Vector3.one * 1.12f;
            _magicCircleRect.localRotation = Quaternion.identity;
        }

        if (_magicCircleImage != null)
        {
            _magicCircleImage.color = _spinningColor;
            SetImageAlpha(_magicCircleImage, 0f);
        }

        if (_resultTextGroup != null)
        {
            _resultTextGroup.alpha = 0f;
            _resultTextGroup.transform.localScale = Vector3.one * 0.9f;
        }
    }

    private void KillCurrentTweens()
    {
        _sequence?.Kill();
        _sequence = null;

        if (_overlayGroup != null) DOTween.Kill(_overlayGroup);
        if (_effectRoot != null) DOTween.Kill(_effectRoot);
        if (_glowRect != null) DOTween.Kill(_glowRect);
        if (_glowImage != null) DOTween.Kill(_glowImage);
        if (_magicCircleRect != null) DOTween.Kill(_magicCircleRect);
        if (_magicCircleImage != null) DOTween.Kill(_magicCircleImage);
        if (_iconRoot != null) DOTween.Kill(_iconRoot);
        if (_spinIcon != null) DOTween.Kill(_spinIcon);
        if (_frameImage != null) DOTween.Kill(_frameImage);
        if (_resultTextGroup != null)
        {
            DOTween.Kill(_resultTextGroup);
            DOTween.Kill(_resultTextGroup.transform);
        }
    }

    private void SetSpinIcon(Sprite icon)
    {
        if (_spinIcon == null)
        {
            return;
        }

        _spinIcon.sprite = icon;
        _spinIcon.enabled = icon != null;
    }

    private Color GetTierColor(int tier)
    {
        if (_tierColors == null || _tierColors.Length == 0)
        {
            return Color.white;
        }

        return _tierColors[Mathf.Clamp(tier - 1, 0, _tierColors.Length - 1)];
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}