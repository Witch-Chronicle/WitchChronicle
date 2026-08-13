using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리에서 마도서를 사용한 직후 열리는 전용 가챠 Overlay.
/// 결과 판정은 SkillBookUseService가 담당하고, 이 클래스는 연출과 닫기만 담당한다.
/// </summary>
public class SkillGachaOverlayController : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private GameObject _overlayRoot;
    [SerializeField] private CanvasGroup _overlayCanvasGroup;
    [SerializeField] private Button _dimmedCloseButton;

    [Header("Animation Root")]
    [SerializeField] private CanvasGroup _animationCanvasGroup;

    [Header("Book")]
    [SerializeField] private Image _bookImage;
    [SerializeField] private RectTransform _bookRect;
    [SerializeField] private Sprite _bookClosedSprite;
    [SerializeField] private Sprite _bookHalfOpenSprite;
    [SerializeField] private Sprite _bookOpenSprite;

    [Header("Magic Effects")]
    [SerializeField] private Image _magicCircleBack;
    [SerializeField] private RectTransform _magicCircleBackRect;
    [SerializeField] private Image _magicCircleFront;
    [SerializeField] private RectTransform _magicCircleFrontRect;
    [SerializeField] private Image _glowBook;
    [SerializeField] private RectTransform _glowBookRect;
    [SerializeField] private Image _gatherRays;
    [SerializeField] private RectTransform _gatherRaysRect;
    [SerializeField] private Image _revealBurst;
    [SerializeField] private RectTransform _revealBurstRect;
    [SerializeField] private Sprite _newSkillBurstSprite;
    [SerializeField] private Sprite _duplicateBurstSprite;
    [SerializeField] private Image _flashImage;

    [Header("Center Skill Reveal")]
    [SerializeField] private GameObject _skillRevealRoot;
    [SerializeField] private CanvasGroup _skillRevealCanvasGroup;
    [SerializeField] private RectTransform _skillRevealRect;
    [SerializeField] private Image _skillGlow;
    [SerializeField] private Image _resultHalo;
    [SerializeField] private Sprite _newSkillHaloSprite;
    [SerializeField] private Sprite _duplicateHaloSprite;
    [SerializeField] private Image _revealSkillIcon;

    [Header("Result Panel")]
    [SerializeField] private GameObject _resultRoot;
    [SerializeField] private CanvasGroup _resultCanvasGroup;
    [SerializeField] private RectTransform _resultRect;
    [SerializeField] private TMP_Text _resultTitleText;
    [SerializeField] private Image _resultSkillIcon;
    [SerializeField] private TMP_Text _resultSkillNameText;
    [SerializeField] private TMP_Text _resultTierText;
    [SerializeField] private TMP_Text _resultDescriptionText;
    [SerializeField] private GameObject _duplicateRewardRoot;
    [SerializeField] private TMP_Text _duplicateText;
    [SerializeField] private TMP_Text _rewardGoldText;
    [SerializeField] private GameObject _closeGuideObject;

    [Header("Preplaced UI Particles")]
    [Tooltip("중앙으로 모여드는 흰색/아이보리 Image들")]
    [SerializeField] private List<Image> _gatherParticles = new List<Image>();
    [Tooltip("신규 스킬 공개 시 중앙에서 퍼지는 별/다이아 Image들")]
    [SerializeField] private List<Image> _resultParticles = new List<Image>();
    [Tooltip("중복 결과에서 사용하는 Coin/Spark Image들")]
    [SerializeField] private List<Image> _goldParticles = new List<Image>();

    [Header("Tier Colors - index 0 = Tier 1")]
    [SerializeField]
    private Color[] _tierColors =
    {
        new Color(1f, 0.84f, 0.34f),
        new Color(0.76f, 0.55f, 0.94f),
        new Color(0.44f, 0.68f, 0.95f),
        new Color(0.82f, 0.82f, 0.82f)
    };

    [Header("Timing")]
    [Min(0f)][SerializeField] private float _overlayFadeDuration = 0.18f;
    [Min(0f)][SerializeField] private float _closedBookTime = 0.55f;
    [Min(0f)][SerializeField] private float _halfOpenTime = 0.32f;
    [Min(0f)][SerializeField] private float _openBookTime = 0.30f;
    [Min(0f)][SerializeField] private float _resultHoldTime = 0.48f;
    [Min(0f)][SerializeField] private float _resultPanelFadeDuration = 0.30f;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _bookAppearClip;
    [SerializeField] private AudioClip _bookOpenClip;
    [SerializeField] private AudioClip _revealClip;
    [SerializeField] private AudioClip _duplicateClip;
    [Range(0f, 1f)][SerializeField] private float _sfxVolume = 0.7f;

    private Sequence _sequence;
    private Action _onClosed;
    private SkillBookResult _currentResult;
    private bool _canClose;

    public bool IsOpen => _overlayRoot != null && _overlayRoot.activeSelf;
    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (_dimmedCloseButton != null)
        {
            _dimmedCloseButton.onClick.AddListener(RequestClose);
        }

        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
        KillParticleTweens(_gatherParticles);
        KillParticleTweens(_resultParticles);
        KillParticleTweens(_goldParticles);

        if (_dimmedCloseButton != null)
        {
            _dimmedCloseButton.onClick.RemoveListener(RequestClose);
        }
    }

    /// <summary>이미 확정된 결과를 받아 Overlay 연출을 시작한다.</summary>
    public bool Play(SkillBookItemData book, SkillBookResult result, Action onClosed = null)
    {
        if (book == null || result.Success == false || result.RolledSkill == null)
        {
            Debug.LogWarning("[SkillGachaOverlay] 표시할 마도서 결과가 올바르지 않습니다.");
            return false;
        }

        if (IsOpen || IsPlaying)
        {
            return false;
        }

        _currentResult = result;
        _onClosed = onClosed;
        IsPlaying = true;
        _canClose = false;

        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(true);
        }

        ResetVisuals(result);
        BuildSequence(result);
        return true;
    }

    public void RequestClose()
    {
        if (_canClose == false || IsOpen == false)
        {
            return;
        }

        _canClose = false;

        if (_dimmedCloseButton != null)
        {
            _dimmedCloseButton.interactable = false;
        }

        _sequence?.Kill();
        _sequence = DOTween.Sequence().SetUpdate(true);

        if (_resultCanvasGroup != null)
        {
            _sequence.Join(_resultCanvasGroup.DOFade(0f, 0.18f));
        }

        if (_animationCanvasGroup != null)
        {
            _sequence.Join(_animationCanvasGroup.DOFade(0f, 0.18f));
        }

        if (_overlayCanvasGroup != null)
        {
            _sequence.Join(_overlayCanvasGroup.DOFade(0f, 0.20f));
        }

        _sequence.OnComplete(() =>
        {
            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(false);
            }

            IsPlaying = false;
            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        });
    }

    private void BuildSequence(SkillBookResult result)
    {
        Color tierColor = GetTierColor(result.RolledTier);
        _sequence?.Kill();
        _sequence = DOTween.Sequence().SetUpdate(true);

        if (_overlayCanvasGroup != null)
        {
            _sequence.Append(_overlayCanvasGroup.DOFade(1f, _overlayFadeDuration));
        }
        else
        {
            _sequence.AppendInterval(_overlayFadeDuration);
        }

        _sequence.AppendCallback(() => PlayClip(_bookAppearClip));

        if (_bookImage != null)
        {
            _sequence.Join(_bookImage.DOFade(1f, 0.24f));
        }

        if (_bookRect != null)
        {
            _sequence.Join(_bookRect.DOScale(1f, 0.30f).SetEase(Ease.OutBack));
            _sequence.Join(_bookRect.DORotate(Vector3.zero, 0.30f));
        }

        if (_magicCircleBack != null)
        {
            _sequence.Join(_magicCircleBack.DOFade(0.75f, 0.38f));
        }

        if (_magicCircleBackRect != null)
        {
            _sequence.Join(_magicCircleBackRect.DOScale(1f, 0.45f).SetEase(Ease.OutSine));
            _sequence.Join(_magicCircleBackRect.DORotate(new Vector3(0f, 0f, 170f), 1.65f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        }

        _sequence.AppendInterval(_closedBookTime);
        _sequence.AppendCallback(PlayGatherParticles);

        if (_gatherRays != null)
        {
            _sequence.Join(_gatherRays.DOFade(0.85f, 0.20f));
        }

        if (_gatherRaysRect != null)
        {
            _sequence.Join(_gatherRaysRect.DOScale(0.68f, 0.55f).SetEase(Ease.InQuad));
        }

        _sequence.AppendCallback(() =>
        {
            if (_bookImage != null) _bookImage.sprite = _bookHalfOpenSprite;
            PlayClip(_bookOpenClip);
        });
        AppendBookPulse(_halfOpenTime);

        _sequence.AppendCallback(() =>
        {
            if (_bookImage != null) _bookImage.sprite = _bookOpenSprite;
        });
        AppendBookPulse(_openBookTime);

        if (_magicCircleFront != null)
        {
            _sequence.Join(_magicCircleFront.DOFade(0.9f, 0.25f));
        }

        if (_magicCircleFrontRect != null)
        {
            _sequence.Join(_magicCircleFrontRect.DORotate(new Vector3(0f, 0f, -190f), 0.80f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
        }

        if (_glowBook != null)
        {
            _glowBook.color = WithAlpha(tierColor, 0f);
            _sequence.Join(_glowBook.DOFade(0.92f, 0.22f));
        }

        if (_glowBookRect != null)
        {
            _sequence.Join(_glowBookRect.DOScale(1.12f, 0.32f).SetEase(Ease.OutQuad));
        }

        _sequence.AppendCallback(() => RevealSkill(result, tierColor));

        if (_flashImage != null)
        {
            _sequence.Append(_flashImage.DOFade(0.92f, 0.07f));
            _sequence.Append(_flashImage.DOFade(0f, 0.18f));
        }

        _sequence.AppendInterval(_resultHoldTime);
        _sequence.AppendCallback(() => ShowResultPanel(result, tierColor));

        if (_resultCanvasGroup != null)
        {
            _sequence.Append(_resultCanvasGroup.DOFade(1f, _resultPanelFadeDuration));
        }

        if (_resultRect != null)
        {
            _sequence.Join(_resultRect.DOScale(1f, _resultPanelFadeDuration).SetEase(Ease.OutBack));
        }

        _sequence.AppendCallback(EnableClose);
    }

    private void AppendBookPulse(float duration)
    {
        if (_bookRect == null)
        {
            _sequence.AppendInterval(duration);
            return;
        }

        float half = Mathf.Max(0.01f, duration * 0.5f);
        _sequence.Append(_bookRect.DOScale(1.045f, half).SetEase(Ease.OutQuad));
        _sequence.Append(_bookRect.DOScale(1f, half).SetEase(Ease.InQuad));
    }

    private void RevealSkill(SkillBookResult result, Color tierColor)
    {
        if (_skillRevealRoot != null)
        {
            _skillRevealRoot.SetActive(true);
        }

        if (_skillRevealCanvasGroup != null)
        {
            _skillRevealCanvasGroup.alpha = 1f;
        }

        if (_revealSkillIcon != null)
        {
            _revealSkillIcon.sprite = result.RolledSkill.SkillIcon;
            _revealSkillIcon.enabled = result.RolledSkill.SkillIcon != null;
            _revealSkillIcon.color = Color.white;
        }

        if (_resultHalo != null)
        {
            _resultHalo.sprite = result.IsDuplicate ? _duplicateHaloSprite : _newSkillHaloSprite;
            _resultHalo.color = result.IsDuplicate ? Color.white : tierColor;
            _resultHalo.rectTransform.localRotation = Quaternion.identity;
            _resultHalo.rectTransform
                .DORotate(new Vector3(0f, 0f, 70f), 1.6f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetUpdate(true);
        }

        if (_skillGlow != null)
        {
            _skillGlow.color = WithAlpha(result.IsDuplicate ? new Color(1f, 0.76f, 0.25f) : tierColor, 0f);
            _skillGlow.DOFade(0.95f, 0.22f).SetUpdate(true);
        }

        if (_revealBurst != null)
        {
            _revealBurst.sprite = result.IsDuplicate ? _duplicateBurstSprite : _newSkillBurstSprite;
            _revealBurst.color = WithAlpha(result.IsDuplicate ? Color.white : tierColor, 0f);
            _revealBurst.DOFade(0.9f, 0.18f).SetUpdate(true);
        }

        if (_revealBurstRect != null)
        {
            _revealBurstRect.localScale = Vector3.one * 0.2f;
            _revealBurstRect.DOScale(1.18f, 0.42f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        if (_skillRevealRect != null)
        {
            _skillRevealRect.localScale = Vector3.zero;
            _skillRevealRect.DOScale(1f, 0.34f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        if (result.IsDuplicate)
        {
            PlayBurstParticles(_goldParticles, true);
            PlayClip(_duplicateClip != null ? _duplicateClip : _revealClip);
        }
        else
        {
            PlayBurstParticles(_resultParticles, false);
            PlayClip(_revealClip);
        }
    }

    private void ShowResultPanel(SkillBookResult result, Color tierColor)
    {
        if (_resultRoot != null)
        {
            _resultRoot.SetActive(true);
        }

        if (_resultCanvasGroup != null)
        {
            _resultCanvasGroup.alpha = 0f;
        }

        if (_resultRect != null)
        {
            _resultRect.localScale = Vector3.one * 0.92f;
        }

        if (_resultTitleText != null)
        {
            _resultTitleText.text = result.IsDuplicate
                ? "이미 알고 있는 지식입니다"
                : "새로운 지식을 습득했습니다";
        }

        if (_resultSkillIcon != null)
        {
            _resultSkillIcon.sprite = result.RolledSkill.SkillIcon;
            _resultSkillIcon.enabled = result.RolledSkill.SkillIcon != null;
            _resultSkillIcon.color = Color.white;
        }

        if (_resultSkillNameText != null)
        {
            _resultSkillNameText.text = result.RolledSkill.SkillName;
            _resultSkillNameText.color = result.IsDuplicate
                ? new Color(1f, 0.80f, 0.35f)
                : tierColor;
        }

        if (_resultTierText != null)
        {
            _resultTierText.text = $"Tier {result.RolledTier}";
            _resultTierText.color = tierColor;
        }

        if (_resultDescriptionText != null)
        {
            _resultDescriptionText.text = result.RolledSkill.Description;
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
        }

        if (_closeGuideObject != null)
        {
            _closeGuideObject.SetActive(true);
        }
    }

    private void EnableClose()
    {
        IsPlaying = false;
        _canClose = true;

        if (_dimmedCloseButton != null)
        {
            _dimmedCloseButton.interactable = true;
        }
    }

    private void ResetVisuals(SkillBookResult result)
    {
        _sequence?.Kill();
        KillParticleTweens(_gatherParticles);
        KillParticleTweens(_resultParticles);
        KillParticleTweens(_goldParticles);

        if (_dimmedCloseButton != null) _dimmedCloseButton.interactable = false;

        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.interactable = true;
            _overlayCanvasGroup.blocksRaycasts = true;
        }

        if (_animationCanvasGroup != null) _animationCanvasGroup.alpha = 1f;
        if (_bookImage != null)
        {
            _bookImage.sprite = _bookClosedSprite;
            _bookImage.color = WithAlpha(Color.white, 0f);
        }
        if (_bookRect != null)
        {
            _bookRect.localScale = Vector3.one * 0.8f;
            _bookRect.localRotation = Quaternion.Euler(0f, 0f, -4f);
        }

        ResetImage(_magicCircleBack, 0f);
        ResetImage(_magicCircleFront, 0f);
        ResetImage(_glowBook, 0f);
        ResetImage(_gatherRays, 0f);
        ResetImage(_revealBurst, 0f);
        ResetImage(_flashImage, 0f);

        if (_magicCircleBackRect != null)
        {
            _magicCircleBackRect.localScale = Vector3.one * 0.8f;
            _magicCircleBackRect.localRotation = Quaternion.identity;
        }
        if (_magicCircleFrontRect != null) _magicCircleFrontRect.localRotation = Quaternion.identity;
        if (_glowBookRect != null) _glowBookRect.localScale = Vector3.one * 0.7f;
        if (_gatherRaysRect != null) _gatherRaysRect.localScale = Vector3.one * 1.5f;
        if (_revealBurstRect != null) _revealBurstRect.localScale = Vector3.one * 0.2f;

        if (_skillRevealRoot != null) _skillRevealRoot.SetActive(false);
        if (_resultRoot != null) _resultRoot.SetActive(false);
        if (_duplicateRewardRoot != null) _duplicateRewardRoot.SetActive(false);
        if (_closeGuideObject != null) _closeGuideObject.SetActive(false);

        ResetParticleList(_gatherParticles);
        ResetParticleList(_resultParticles);
        ResetParticleList(_goldParticles);
    }

    private void PlayGatherParticles()
    {
        for (int i = 0; i < _gatherParticles.Count; i++)
        {
            Image image = _gatherParticles[i];
            if (image == null) continue;

            RectTransform rect = image.rectTransform;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = UnityEngine.Random.Range(270f, 430f);
            rect.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            rect.localScale = Vector3.one * UnityEngine.Random.Range(0.45f, 1.15f);
            image.gameObject.SetActive(true);
            image.color = WithAlpha(Color.white, 0f);

            float delay = UnityEngine.Random.Range(0f, 0.18f);
            float duration = UnityEngine.Random.Range(0.38f, 0.65f);
            Sequence particle = DOTween.Sequence().SetDelay(delay).SetUpdate(true);
            particle.Join(image.DOFade(UnityEngine.Random.Range(0.55f, 1f), 0.12f));
            particle.Join(rect.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.InQuad));
            particle.Insert(delay + Mathf.Max(0f, duration - 0.13f), image.DOFade(0f, 0.13f));
        }
    }

    private void PlayBurstParticles(List<Image> particles, bool gold)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Image image = particles[i];
            if (image == null) continue;

            RectTransform rect = image.rectTransform;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = UnityEngine.Random.Range(gold ? 190f : 160f, gold ? 390f : 340f);
            Vector2 target = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one * UnityEngine.Random.Range(0.35f, 0.9f);
            rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            image.gameObject.SetActive(true);
            image.color = WithAlpha(Color.white, 0f);

            float delay = UnityEngine.Random.Range(0f, 0.16f);
            float duration = UnityEngine.Random.Range(0.55f, 0.95f);
            Sequence particle = DOTween.Sequence().SetDelay(delay).SetUpdate(true);
            particle.Join(image.DOFade(1f, 0.10f));
            particle.Join(rect.DOAnchorPos(target, duration).SetEase(Ease.OutCubic));
            particle.Join(rect.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(120f, 300f)), duration, RotateMode.FastBeyond360));
            particle.Insert(delay + duration * 0.55f, image.DOFade(0f, duration * 0.45f));
        }
    }

    private static void ResetImage(Image image, float alpha)
    {
        if (image == null) return;
        image.DOKill();
        image.color = WithAlpha(Color.white, alpha);
    }

    private static void ResetParticleList(List<Image> particles)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Image image = particles[i];
            if (image == null) continue;
            image.gameObject.SetActive(false);
        }
    }

    private static void KillParticleTweens(List<Image> particles)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Image image = particles[i];
            if (image == null) continue;
            image.DOKill();
            image.rectTransform.DOKill();
        }
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

    private void PlayClip(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip, _sfxVolume);
        }
    }
}