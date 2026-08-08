using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 버튼에 마우스 호버 시 SelectedOrnament(Image)를 활성화하고
/// Color의 alpha를 0->1 로 부드럽게 페이드인, 벗어나면 1->0 페이드아웃.
/// 각 버튼(StartBtn, SettingBtn, ExitBtn) 오브젝트에 부착.
/// </summary>
public class ButtonHoverOrnament : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Ornament")]
    [Tooltip("호버 시 활성화/페이드될 이미지 (SelectedOrnament)")]
    [SerializeField] private Image _ornamentImage;

    [Header("Fade")]
    [SerializeField] private float _fadeInDuration = 0.15f;
    [SerializeField] private float _fadeOutDuration = 0.15f;
    [SerializeField] private Ease _fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease _fadeOutEase = Ease.OutQuad;

    private void Awake()
    {
        HideImmediate();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        FadeOrnament(1f, _fadeInDuration, _fadeInEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        FadeOrnament(0f, _fadeOutDuration, _fadeOutEase);
    }

    private void FadeOrnament(float targetAlpha, float duration, Ease ease)
    {
        if (_ornamentImage == null) return;

        if (targetAlpha > 0f && _ornamentImage.gameObject.activeSelf == false)
        {
            _ornamentImage.gameObject.SetActive(true);
        }

        _ornamentImage.DOKill();
        _ornamentImage
            .DOFade(targetAlpha, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                if (targetAlpha <= 0f && _ornamentImage != null)
                {
                    _ornamentImage.gameObject.SetActive(false);
                }
            });
    }

    private void HideImmediate()
    {
        if (_ornamentImage == null) return;

        _ornamentImage.DOKill();

        Color c = _ornamentImage.color;
        c.a = 0f;
        _ornamentImage.color = c;

        _ornamentImage.gameObject.SetActive(false);
    }
}