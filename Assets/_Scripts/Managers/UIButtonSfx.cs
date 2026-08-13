using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특정 버튼에 다른 SFX를 넣고 싶을 때만 붙임.
/// 이게 붙어있으면 SoundManager의 자동 등록은 스킵됨.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonSfx : MonoBehaviour
{
    [SerializeField] private SfxType sfxType = SfxType.ButtonClick;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(PlaySound);
    }

    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(PlaySound);
    }

    private void PlaySound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(sfxType);
    }
}