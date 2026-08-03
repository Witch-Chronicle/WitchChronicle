using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Title 씬 버튼(StartBtn/SettingBtn/ExitBtn)에 부착. 마우스 Hover 시
/// Image/TMP 색상 변경 + 스케일업 애니메이션을 재생하고, Hover가 끝나면 원래대로 복귀.
/// </summary>
[RequireComponent(typeof(Button))]
public class TitleButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("대상")]
    [SerializeField] private Image _btnImage;
    [SerializeField] private TMP_Text _btnText;

    [Header("Hover 색상")]
    [SerializeField] private Color _hoverImageColor = Color.white;
    [SerializeField] private Color _hoverTextColor = Color.white;

    [Header("Hover 스케일")]
    [SerializeField] private float _hoverScale = 1.1f;
    [SerializeField] private float _duration = 0.15f;
    [SerializeField] private Ease _ease = Ease.OutQuad;

    private RectTransform _rectTransform;
    private Color _normalImageColor;
    private Color _normalTextColor;
    private Vector3 _normalScale;

    private Tween _scaleTween;
    private Tween _imageColorTween;
    private Tween _textColorTween;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;

        if (_btnImage != null) _normalImageColor = _btnImage.color;
        if (_btnText != null) _normalTextColor = _btnText.color;
        if (_rectTransform != null) _normalScale = _rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateTo(_hoverImageColor, _hoverTextColor, _normalScale * _hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(_normalImageColor, _normalTextColor, _normalScale);
    }

    private void AnimateTo(Color imageColor, Color textColor, Vector3 scale)
    {
        if (_rectTransform != null)
        {
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(scale, _duration).SetEase(_ease);
        }

        if (_btnImage != null)
        {
            _imageColorTween?.Kill();
            _imageColorTween = _btnImage.DOColor(imageColor, _duration).SetEase(_ease);
        }

        if (_btnText != null)
        {
            _textColorTween?.Kill();
            _textColorTween = _btnText.DOColor(textColor, _duration).SetEase(_ease);
        }
    }

    private void OnDisable()
    {
        _scaleTween?.Kill();
        _imageColorTween?.Kill();
        _textColorTween?.Kill();

        if (_rectTransform != null) _rectTransform.localScale = _normalScale;
        if (_btnImage != null) _btnImage.color = _normalImageColor;
        if (_btnText != null) _btnText.color = _normalTextColor;
    }
}