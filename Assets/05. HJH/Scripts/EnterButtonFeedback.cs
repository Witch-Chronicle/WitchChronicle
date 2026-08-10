using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// EnterBtn의 Hover / Press 시 BG, Frame 색상을 함께 변경해
/// 버튼이 눌리는 듯한 피드백을 준다.
/// Button의 기본 Color Tint 대신 사용 (타겟이 여러 개라서).
/// </summary>
[RequireComponent(typeof(Button))]
public class EnterButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("색상을 바꿀 대상")]
    [SerializeField] private Image _bgImage;
    [SerializeField] private Image _frameImage;
    [Header("Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _hoverColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color _pressedColor = new Color(0.75f, 0.75f, 0.75f);
    [Header("Tween")]
    [SerializeField, Min(0.01f)] private float _duration = 0.1f;
    private Button _button;
    private bool _isPointerInside;
    private bool _isPointerDown;
    private void Awake()
    {
        _button = GetComponent<Button>();
        ApplyColorImmediate(_normalColor);
    }
    private void OnDisable()
    {
        _isPointerInside = false;
        _isPointerDown = false;
        _bgImage?.DOKill();
        _frameImage?.DOKill();
        ApplyColorImmediate(_normalColor);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;
        RefreshVisual();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
        _isPointerDown = false;
        RefreshVisual();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
        RefreshVisual();
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        RefreshVisual();
    }
    private void RefreshVisual()
    {
        if (_button != null && _button.interactable == false)
        {
            ApplyColor(_normalColor);
            return;
        }
        if (_isPointerDown)
        {
            ApplyColor(_pressedColor);
        }
        else if (_isPointerInside)
        {
            ApplyColor(_hoverColor);
        }
        else
        {
            ApplyColor(_normalColor);
        }
    }
    private void ApplyColor(Color color)
    {
        if (_bgImage != null)
        {
            _bgImage.DOKill();
            _bgImage.DOColor(color, _duration);
        }
        if (_frameImage != null)
        {
            _frameImage.DOKill();
            _frameImage.DOColor(color, _duration);
        }
    }
    private void ApplyColorImmediate(Color color)
    {
        if (_bgImage != null)
        {
            _bgImage.DOKill();
            _bgImage.color = color;
        }
        if (_frameImage != null)
        {
            _frameImage.DOKill();
            _frameImage.color = color;
        }
    }
}