using UnityEngine;

/// <summary>
/// Singleton Pattern으로 전역 사운드 재생을 담당한다.
/// - BGM: 배경음 재생/정지
/// - SFX: 효과음 1회 재생(PlayOneShot). 캐릭터 사운드(CharacterAudio) 등도 전부 이곳을 거쳐서 재생됨.
/// - 마스터/BGM/SFX 볼륨 + Mute 여부를 SoundManager가 유일한 원본(single source of truth)으로 관리.
///   Title/Pause 등 여러 곳의 VolumeControl UI가 전부 이 값을 그대로 반영/조작하므로 항상 동기화됨.
/// - Mute 상태에서는 슬라이더 값(볼륨) 자체는 그대로 유지한 채 실제 재생 볼륨만 0으로 처리됨.
/// </summary>
[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("AudioSource (Optional)")]
    [Tooltip("비어 있으면 Awake에서 자동 생성한다. BGM 전용 AudioSource.")]
    [SerializeField] private AudioSource bgmSource;
    [Tooltip("비어 있으면 Awake에서 자동 생성한다. SFX 전용 AudioSource.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("볼륨 (0~1, 설정 UI가 이 값을 저장/복원)")]
    [Range(0f, 1f)][SerializeField] private float _masterVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float _bgmVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float _sfxVolume = 1f;

    [Header("Mute 여부 (볼륨 값과 별개로 관리됨)")]
    [SerializeField] private bool _isMasterMuted;
    [SerializeField] private bool _isBgmMuted;
    [SerializeField] private bool _isSfxMuted;

    /// <summary>슬라이더에 표시할 값 (Mute 여부와 무관하게 마지막으로 설정한 값 그대로 유지)</summary>
    public float MasterVolume => _masterVolume;
    public float BgmVolume => _bgmVolume;
    public float SfxVolume => _sfxVolume;

    public bool IsMasterMuted => _isMasterMuted;
    public bool IsBgmMuted => _isBgmMuted;
    public bool IsSfxMuted => _isSfxMuted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        ApplyBgmVolume();
    }

    /// <summary>
    /// BGM을 재생한다. 기존 BGM이 있으면 clip을 교체해 다시 재생한다.
    /// volume은 이 BGM 고유의 배율(0~1)이며, 최종 볼륨은 여기에 마스터/BGM 볼륨(+Mute)이 곱해져 적용된다.
    /// </summary>
    public void PlayBgm(AudioClip bgmClip, float volume = 1f, bool loop = true)
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("[SoundManager] PlayBgm 실패: bgmClip이 null입니다.");
            return;
        }

        if (bgmSource == null)
        {
            Debug.LogWarning("[SoundManager] PlayBgm 실패: bgmSource가 없습니다.");
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.loop = loop;
        bgmSource.volume = Mathf.Clamp01(volume) * GetEffectiveBgmFactor();
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("[SoundManager] StopBgm 실패: bgmSource가 없습니다.");
            return;
        }

        bgmSource.Stop();
    }

    /// <summary>
    /// 효과음을 1회 재생한다. (AudioSource.PlayOneShot)
    /// * 캐릭터 효과음(CharacterAudio 등)도 전부 이 메서드를 거쳐서 재생해야 마스터/SFX 볼륨/Mute가 반영됨.
    /// </summary>
    public void PlaySfxOneShot(AudioClip sfxClip, float volumeScale = 1f)
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("[SoundManager] PlaySfxOneShot 실패: sfxClip이 null입니다.");
            return;
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("[SoundManager] PlaySfxOneShot 실패: sfxSource가 없습니다.");
            return;
        }

        float finalVolume = Mathf.Clamp01(volumeScale) * GetEffectiveSfxFactor();
        sfxSource.PlayOneShot(sfxClip, finalVolume);
    }

    // ===================== 볼륨 / Mute 조절 (설정 UI에서 호출) =====================

    /// <summary>
    /// 마스터 볼륨 설정 (0~1). Mute 상태와 무관하게 슬라이더 값 자체를 저장.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        ApplyBgmVolume();
    }

    public void SetBgmVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyBgmVolume();
    }

    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 마스터 Mute 여부 설정. true면 마스터가 걸린 모든 소리(BGM+SFX)가 실질적으로 0볼륨이 됨.
    /// </summary>
    public void SetMasterMuted(bool muted)
    {
        _isMasterMuted = muted;
        ApplyBgmVolume();
    }

    public void SetBgmMuted(bool muted)
    {
        _isBgmMuted = muted;
        ApplyBgmVolume();
    }

    public void SetSfxMuted(bool muted)
    {
        _isSfxMuted = muted;
    }

    /// <summary>
    /// 마스터를 뺀 BGM 자체의 유효 배율 (Mute면 0, 아니면 설정된 볼륨).
    /// </summary>
    private float GetEffectiveBgmFactor()
    {
        float bgmFactor = _isBgmMuted ? 0f : _bgmVolume;
        float masterFactor = _isMasterMuted ? 0f : _masterVolume;
        return bgmFactor * masterFactor;
    }

    private float GetEffectiveSfxFactor()
    {
        float sfxFactor = _isSfxMuted ? 0f : _sfxVolume;
        float masterFactor = _isMasterMuted ? 0f : _masterVolume;
        return sfxFactor * masterFactor;
    }

    /// <summary>
    /// 지금 재생 중인 bgmSource의 실제 볼륨을 최신 마스터/BGM 볼륨+Mute 상태로 갱신.
    /// </summary>
    private void ApplyBgmVolume()
    {
        if (bgmSource == null) return;

        bgmSource.volume = GetEffectiveBgmFactor();
    }

    private void EnsureAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }
}