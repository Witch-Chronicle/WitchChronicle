using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("대상")]
    [SerializeField] private TMP_Text _labelTxt;

    [Header("Hover 색상")]
    [SerializeField] private Color _hoverTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Hover 스케일")]
    [SerializeField] private float _hoverScale = 1.08f;
    [SerializeField] private float _duration = 0.15f;
    [SerializeField] private Ease _ease = Ease.OutQuad;

    private RectTransform _rectTransform;
    private Color _normalTextColor;
    private Vector3 _normalScale = Vector3.one;

    private Tween _scaleTween;
    private Tween _textColorTween;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;

        if (_labelTxt == null) _labelTxt = GetComponentInChildren<TMP_Text>();

        if (_labelTxt != null) _normalTextColor = _labelTxt.color;
    }

    private void OnEnable()
    {
        // 패널이 열리는 애니메이션(스케일 트윈) 도중에 값이 잘못 캡처되는 걸 방지하기 위해,
        // 활성화될 때마다 그 시점의 스케일을 "정상 크기" 기준으로 다시 잡음.
        if (_rectTransform != null)
        {
            _normalScale = _rectTransform.localScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateTo(_hoverTextColor, _normalScale * _hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(_normalTextColor, _normalScale);
    }

    private void AnimateTo(Color textColor, Vector3 scale)
    {
        if (_rectTransform != null)
        {
            _scaleTween?.Kill();
            _scaleTween = _rectTransform.DOScale(scale, _duration).SetEase(_ease).SetUpdate(true);
        }

        if (_labelTxt != null)
        {
            _textColorTween?.Kill();
            _textColorTween = _labelTxt.DOColor(textColor, _duration).SetEase(_ease).SetUpdate(true);
        }
    }

    private void OnDisable()
    {
        _scaleTween?.Kill();
        _textColorTween?.Kill();

        if (_rectTransform != null) _rectTransform.localScale = _normalScale;
        if (_labelTxt != null) _labelTxt.color = _normalTextColor;
    }
}