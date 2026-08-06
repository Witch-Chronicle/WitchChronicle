using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼 위에 마우스를 올리면 지정된 Image들(BG, Frame 등)의 색상을 바꾼다.
/// EquipBtn/UnEquipBtn처럼 텍스트 없이 배경/테두리 색만 전환하는 경우에 사용.
/// </summary>
public class SkillEquipButtonColorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Hover 시 색상을 바꿀 대상들 (BG, Frame 등)")]
    [SerializeField] private Image[] _targets;

    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _hoverColor = new Color(0.62f, 0.62f, 0.62f); // 9F9F9F

    private void OnEnable()
    {
        SetColor(_normalColor);
    }

    private void OnDisable()
    {
        SetColor(_normalColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetColor(_hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetColor(_normalColor);
    }

    private void SetColor(Color color)
    {
        if (_targets == null) return;

        for (int i = 0; i < _targets.Length; i++)
        {
            if (_targets[i] != null)
            {
                _targets[i].color = color;
            }
        }
    }
}