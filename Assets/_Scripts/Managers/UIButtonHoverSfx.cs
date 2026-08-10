using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼에 마우스 커서가 들어오면 hover 사운드 재생.
/// SoundManager가 자동으로 부착. Button이 비활성이면 소리 안 남.
/// </summary>
[DisallowMultipleComponent]
public class UIButtonHoverSfx : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private SfxType sfxType = SfxType.ButtonHover;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 버튼이 비활성/미상호작용이면 소리 안 재생
        if (_button != null && (!_button.interactable || !_button.IsActive())) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(sfxType);
    }
}