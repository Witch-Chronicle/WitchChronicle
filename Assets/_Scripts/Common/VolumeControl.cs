using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SettingPanel의 볼륨 조절 한 줄(Master/Bgm/Sfx 공통)에 부착.
/// - 상태(볼륨 값/Mute 여부)의 원본은 SoundManager. 이 컴포넌트는 그 값을 그대로 반영/조작만 함.
/// - Title/Pause 등 여러 씬/패널에 배치된 VolumeControl이 전부 SoundManager를 보고 있어서 항상 동기화됨.
/// - Slider: 값이 바뀌면 SoundManager의 해당 볼륨에 반영 (Mute 상태에서도 값 자체는 저장됨, 실제 재생만 0)
/// - MuteToggleBtn: 클릭할 때마다 SoundManager의 Mute 여부를 토글
/// </summary>
public class VolumeControl : MonoBehaviour
{
    public enum VolumeType
    {
        Master,
        Bgm,
        Sfx
    }

    [Header("어느 볼륨을 조절할지")]
    [SerializeField] private VolumeType _volumeType;

    [Header("UI 연결")]
    [SerializeField] private Slider _slider;
    [SerializeField] private Button _muteToggleBtn;
    [SerializeField] private Image _muteIconImage;
    [SerializeField] private Sprite _notMuteSprite;
    [SerializeField] private Sprite _muteSprite;

    [Header("Mute 상태 색상")]
    [SerializeField] private Color _notMuteColor = Color.white;
    [SerializeField] private Color _muteColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private void OnEnable()
    {
        RefreshFromSoundManager();

        if (_slider != null) _slider.onValueChanged.AddListener(OnSliderChanged);
        if (_muteToggleBtn != null) _muteToggleBtn.onClick.AddListener(OnClickMuteToggle);
    }

    private void OnDisable()
    {
        if (_slider != null) _slider.onValueChanged.RemoveListener(OnSliderChanged);
        if (_muteToggleBtn != null) _muteToggleBtn.onClick.RemoveListener(OnClickMuteToggle);
    }

    /// <summary>
    /// 패널이 열릴 때마다 SoundManager에 저장된 실제 상태(볼륨 값 + Mute 여부)로 UI를 동기화.
    /// </summary>
    private void RefreshFromSoundManager()
    {
        if (SoundManager.Instance == null) return;

        float volume = GetVolumeFromManager();
        bool isMuted = GetMutedFromManager();

        if (_slider != null)
        {
            _slider.SetValueWithoutNotify(volume);
        }

        UpdateMuteIcon(isMuted);
    }

    private void OnSliderChanged(float value)
    {
        if (SoundManager.Instance == null) return;

        SetVolumeToManager(value);
        // Mute 상태에서도 슬라이더 값 자체는 저장됨 (SoundManager 내부에서 실제 재생 볼륨만 0으로 유지)
    }

    private void OnClickMuteToggle()
    {
        if (SoundManager.Instance == null) return;

        bool newMuted = !GetMutedFromManager();
        SetMutedToManager(newMuted);
        UpdateMuteIcon(newMuted);
    }

    private void UpdateMuteIcon(bool isMuted)
    {
        if (_muteIconImage == null) return;

        _muteIconImage.sprite = isMuted ? _muteSprite : _notMuteSprite;
        _muteIconImage.color = isMuted ? _muteColor : _notMuteColor;
    }

    private float GetVolumeFromManager()
    {
        switch (_volumeType)
        {
            case VolumeType.Master: return SoundManager.Instance.MasterVolume;
            case VolumeType.Bgm: return SoundManager.Instance.BgmVolume;
            case VolumeType.Sfx: return SoundManager.Instance.SfxVolume;
            default: return 1f;
        }
    }

    private bool GetMutedFromManager()
    {
        switch (_volumeType)
        {
            case VolumeType.Master: return SoundManager.Instance.IsMasterMuted;
            case VolumeType.Bgm: return SoundManager.Instance.IsBgmMuted;
            case VolumeType.Sfx: return SoundManager.Instance.IsSfxMuted;
            default: return false;
        }
    }

    private void SetVolumeToManager(float value)
    {
        switch (_volumeType)
        {
            case VolumeType.Master:
                SoundManager.Instance.SetMasterVolume(value);
                break;
            case VolumeType.Bgm:
                SoundManager.Instance.SetBgmVolume(value);
                break;
            case VolumeType.Sfx:
                SoundManager.Instance.SetSfxVolume(value);
                break;
        }
    }

    private void SetMutedToManager(bool muted)
    {
        switch (_volumeType)
        {
            case VolumeType.Master:
                SoundManager.Instance.SetMasterMuted(muted);
                break;
            case VolumeType.Bgm:
                SoundManager.Instance.SetBgmMuted(muted);
                break;
            case VolumeType.Sfx:
                SoundManager.Instance.SetSfxMuted(muted);
                break;
        }
    }
}