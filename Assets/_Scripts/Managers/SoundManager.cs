using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton Pattern으로 전역 사운드 재생을 담당한다.
/// - BGM: 배경음 재생/정지 + 씬 전환 시 크로스페이드 자동 전환
/// - SFX: 효과음 1회 재생(PlayOneShot). 캐릭터 사운드(CharacterAudio) 등도 전부 이곳을 거쳐서 재생됨.
/// - 마스터/BGM/SFX 볼륨 + Mute 여부를 SoundManager가 유일한 원본(single source of truth)으로 관리.
///   Title/Pause 등 여러 곳의 VolumeControl UI가 전부 이 값을 그대로 반영/조작하므로 항상 동기화됨.
/// - Mute 상태에서는 슬라이더 값(볼륨) 자체는 그대로 유지한 채 실제 재생 볼륨만 0으로 처리됨.
/// - BGM은 크로스페이드 전환을 위해 AudioSource 2개(bgmSourceA/B)를 번갈아 사용한다.
/// </summary>
[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class SceneBgmEntry
    {
        [Tooltip("씬 이름 (Build Settings의 이름과 정확히 일치해야 함)")]
        public string sceneName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumeScale = 1f;
        public bool loop = true;
    }

    [Header("AudioSource (Optional)")]
    [Tooltip("비어 있으면 Awake에서 자동 생성한다. BGM 크로스페이드용 A.")]
    [SerializeField] private AudioSource bgmSourceA;
    [Tooltip("비어 있으면 Awake에서 자동 생성한다. BGM 크로스페이드용 B.")]
    [SerializeField] private AudioSource bgmSourceB;
    [Tooltip("비어 있으면 Awake에서 자동 생성한다. SFX 전용 AudioSource.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("씬별 BGM 매핑")]
    [Tooltip("씬 이름 → BGM 클립. 씬 로드 시 자동으로 크로스페이드 전환됨. 매핑 없는 씬은 자동 정지.")]
    [SerializeField] private List<SceneBgmEntry> sceneBgmList = new();

    [Header("크로스페이드 설정")]
    [Tooltip("BGM 전환 페이드 시간(초).")]
    [SerializeField] private float crossfadeDuration = 1.5f;

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

    // 크로스페이드 상태
    private AudioSource _currentBgmSource;   // 지금 재생 중(또는 페이드인 완료 예정) 소스
    private AudioSource _nextBgmSource;      // 다음 페이드인 대기 소스
    private float _currentBgmScale = 1f;     // 현재 BGM 고유 배율 (PlayBgm의 volume 파라미터)
    private Coroutine _fadeCo;

    // 씬 이름 → 엔트리 빠른 조회용
    private Dictionary<string, SceneBgmEntry> _sceneBgmMap;

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
        BuildSceneBgmMap();

        _currentBgmSource = bgmSourceA;
        _nextBgmSource = bgmSourceB;

        ApplyBgmVolume();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Boot 씬에서 SoundManager가 생성된 직후에도 BGM이 재생되도록
        TryPlaySceneBgm(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryPlaySceneBgm(scene.name);
    }

    // ===================== BGM 재생 API =====================

    /// <summary>
    /// BGM을 재생한다. 기존 BGM이 재생 중이면 크로스페이드로 전환한다.
    /// volume은 이 BGM 고유의 배율(0~1)이며, 최종 볼륨은 여기에 마스터/BGM 볼륨(+Mute)이 곱해져 적용된다.
    /// </summary>
    public void PlayBgm(AudioClip bgmClip, float volume = 1f, bool loop = true)
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("[SoundManager] PlayBgm 실패: bgmClip이 null입니다.");
            return;
        }

        if (_currentBgmSource == null)
        {
            Debug.LogWarning("[SoundManager] PlayBgm 실패: bgmSource가 없습니다.");
            return;
        }

        // 이미 같은 클립이 재생 중이면 무시 (씬 재로드 등에서 끊김 방지)
        if (_currentBgmSource.clip == bgmClip && _currentBgmSource.isPlaying)
        {
            _currentBgmScale = Mathf.Clamp01(volume);
            ApplyBgmVolume();
            return;
        }

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CrossfadeTo(bgmClip, Mathf.Clamp01(volume), loop));
    }

    /// <summary>
    /// 씬 이름으로 매핑된 BGM을 재생. 매핑이 없으면 페이드아웃하여 정지.
    /// </summary>
    public void TryPlaySceneBgm(string sceneName)
    {
        if (_sceneBgmMap == null) BuildSceneBgmMap();

        if (_sceneBgmMap.TryGetValue(sceneName, out var entry) && entry.clip != null)
        {
            PlayBgm(entry.clip, entry.volumeScale, entry.loop);
        }
        else
        {
            StopBgm();
        }
    }

    public void StopBgm()
    {
        if (_currentBgmSource == null)
        {
            Debug.LogWarning("[SoundManager] StopBgm 실패: bgmSource가 없습니다.");
            return;
        }

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeOutCurrent());
    }

    private IEnumerator CrossfadeTo(AudioClip newClip, float newScale, bool loop)
    {
        // 새 소스에 새 클립 재생 준비
        _nextBgmSource.clip = newClip;
        _nextBgmSource.loop = loop;
        _nextBgmSource.volume = 0f;
        _nextBgmSource.Play();

        float startCurrentVol = _currentBgmSource.volume;
        float targetNextVol = newScale * GetEffectiveBgmFactor();

        float t = 0f;
        float dur = Mathf.Max(0.01f, crossfadeDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            _currentBgmSource.volume = Mathf.Lerp(startCurrentVol, 0f, k);
            // 진행 중에 마스터/BGM 볼륨이 변할 수 있으니 매 프레임 factor 재계산
            _nextBgmSource.volume = Mathf.Lerp(0f, newScale * GetEffectiveBgmFactor(), k);
            yield return null;
        }

        _currentBgmSource.Stop();
        _currentBgmSource.clip = null;

        // 스왑
        (_currentBgmSource, _nextBgmSource) = (_nextBgmSource, _currentBgmSource);
        _currentBgmScale = newScale;

        ApplyBgmVolume();
        _fadeCo = null;
    }

    private IEnumerator FadeOutCurrent()
    {
        float startVol = _currentBgmSource.volume;
        float t = 0f;
        float dur = Mathf.Max(0.01f, crossfadeDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            _currentBgmSource.volume = Mathf.Lerp(startVol, 0f, t / dur);
            yield return null;
        }
        _currentBgmSource.Stop();
        _currentBgmSource.clip = null;
        _currentBgmScale = 1f;
        _fadeCo = null;
    }

    // ===================== SFX =====================

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
    /// 지금 재생 중인 현재 BGM 소스의 실제 볼륨을 최신 마스터/BGM 볼륨+Mute 상태로 갱신.
    /// 크로스페이드 중에는 코루틴이 매 프레임 볼륨을 덮어쓰므로 여기선 무시.
    /// </summary>
    private void ApplyBgmVolume()
    {
        if (_currentBgmSource == null) return;
        if (_fadeCo != null) return; // 페이드 중엔 코루틴이 관리

        _currentBgmSource.volume = _currentBgmScale * GetEffectiveBgmFactor();
    }

    private void EnsureAudioSources()
    {
        if (bgmSourceA == null) bgmSourceA = gameObject.AddComponent<AudioSource>();
        if (bgmSourceB == null) bgmSourceB = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        foreach (var src in new[] { bgmSourceA, bgmSourceB })
        {
            src.playOnAwake = false;
            src.loop = true;
            src.volume = 0f;
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    private void BuildSceneBgmMap()
    {
        _sceneBgmMap = new Dictionary<string, SceneBgmEntry>();
        foreach (var entry in sceneBgmList)
        {
            if (entry == null || string.IsNullOrEmpty(entry.sceneName)) continue;
            _sceneBgmMap[entry.sceneName] = entry;
        }
    }
}