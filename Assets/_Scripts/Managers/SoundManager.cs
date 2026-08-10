using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class SceneBgmEntry
    {
        public string sceneName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumeScale = 1f;
        public bool loop = true;
    }

    [System.Serializable]
    public class SfxEntry
    {
        public SfxType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumeScale = 1f;
    }

    [Header("AudioSource (Optional)")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("루프 재생용 SFX (낚시 캐스트 같은 지속음)")]
    [SerializeField] private AudioSource loopSfxSource;

    [Header("씬별 BGM 매핑")]
    [SerializeField] private List<SceneBgmEntry> sceneBgmList = new();

    [Header("SFX 매핑 (enum → 클립)")]
    [SerializeField] private List<SfxEntry> sfxList = new();

    [Header("자동 버튼 클릭 사운드")]
    [SerializeField] private bool autoRegisterButtons = true;

    [Header("크로스페이드 설정")]
    [SerializeField] private float crossfadeDuration = 1.5f;

    [Header("볼륨 (0~1)")]
    [Range(0f, 1f)][SerializeField] private float _masterVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float _bgmVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float _sfxVolume = 1f;

    [Header("Mute 여부")]
    [SerializeField] private bool _isMasterMuted;
    [SerializeField] private bool _isBgmMuted;
    [SerializeField] private bool _isSfxMuted;

    public float MasterVolume => _masterVolume;
    public float BgmVolume => _bgmVolume;
    public float SfxVolume => _sfxVolume;

    public bool IsMasterMuted => _isMasterMuted;
    public bool IsBgmMuted => _isBgmMuted;
    public bool IsSfxMuted => _isSfxMuted;

    private AudioSource _currentBgmSource;
    private AudioSource _nextBgmSource;
    private float _currentBgmScale = 1f;
    private Coroutine _fadeCo;

    private Dictionary<string, SceneBgmEntry> _sceneBgmMap;
    private Dictionary<SfxType, SfxEntry> _sfxMap;

    // 현재 루프 재생 중인 SFX
    private SfxType? _currentLoopSfx;
    private float _currentLoopScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        BuildSceneBgmMap();
        BuildSfxMap();

        _currentBgmSource = bgmSourceA;
        _nextBgmSource = bgmSourceB;

        ApplyBgmVolume();
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start()
    {
        TryPlaySceneBgm(SceneManager.GetActiveScene().name);
        if (autoRegisterButtons) AutoRegisterAllButtons();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryPlaySceneBgm(scene.name);
        if (autoRegisterButtons) AutoRegisterAllButtons();

        // 씬 전환 시 루프 SFX 자동 정지 (다른 씬으로 넘어가는데 낚시 소리 계속 나면 이상함)
        StopSfxLoop();
    }

    // ===================== BGM =====================

    public void PlayBgm(AudioClip bgmClip, float volume = 1f, bool loop = true)
    {
        if (bgmClip == null || _currentBgmSource == null) return;

        if (_currentBgmSource.clip == bgmClip && _currentBgmSource.isPlaying)
        {
            _currentBgmScale = Mathf.Clamp01(volume);
            ApplyBgmVolume();
            return;
        }

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CrossfadeTo(bgmClip, Mathf.Clamp01(volume), loop));
    }

    public void TryPlaySceneBgm(string sceneName)
    {
        if (_sceneBgmMap == null) BuildSceneBgmMap();

        if (_sceneBgmMap.TryGetValue(sceneName, out var entry) && entry.clip != null)
            PlayBgm(entry.clip, entry.volumeScale, entry.loop);
        else
            StopBgm();
    }

    public void StopBgm()
    {
        if (_currentBgmSource == null) return;
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeOutCurrent());
    }

    private IEnumerator CrossfadeTo(AudioClip newClip, float newScale, bool loop)
    {
        _nextBgmSource.clip = newClip;
        _nextBgmSource.loop = loop;
        _nextBgmSource.volume = 0f;
        _nextBgmSource.Play();

        float startCurrentVol = _currentBgmSource.volume;
        float t = 0f;
        float dur = Mathf.Max(0.01f, crossfadeDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            _currentBgmSource.volume = Mathf.Lerp(startCurrentVol, 0f, k);
            _nextBgmSource.volume = Mathf.Lerp(0f, newScale * GetEffectiveBgmFactor(), k);
            yield return null;
        }

        _currentBgmSource.Stop();
        _currentBgmSource.clip = null;
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
    /// enum SFX를 1회 재생 (PlayOneShot).
    /// </summary>
    public void PlaySfx(SfxType type, float extraVolumeScale = 1f)
    {
        if (_sfxMap == null) BuildSfxMap();

        if (!_sfxMap.TryGetValue(type, out var entry) || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 미등록/클립 없음: {type}");
            return;
        }

        float finalVolume = Mathf.Clamp01(entry.volumeScale * extraVolumeScale) * GetEffectiveSfxFactor();
        sfxSource.PlayOneShot(entry.clip, finalVolume);
    }

    public void PlaySfxOneShot(AudioClip sfxClip, float volumeScale = 1f)
    {
        if (sfxClip == null || sfxSource == null) return;
        float finalVolume = Mathf.Clamp01(volumeScale) * GetEffectiveSfxFactor();
        sfxSource.PlayOneShot(sfxClip, finalVolume);
    }

    // ===================== 루프 SFX (지속음) =====================

    /// <summary>
    /// SFX를 루프 재생 시작. StopSfxLoop() 부를 때까지 계속 재생.
    /// 이미 다른 루프 SFX 재생 중이면 교체됨.
    /// </summary>
    public void PlaySfxLoop(SfxType type, float extraVolumeScale = 1f)
    {
        if (_sfxMap == null) BuildSfxMap();

        if (!_sfxMap.TryGetValue(type, out var entry) || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] 루프 SFX 미등록/클립 없음: {type}");
            return;
        }

        if (loopSfxSource == null)
        {
            Debug.LogWarning("[SoundManager] loopSfxSource가 없음");
            return;
        }

        // 이미 같은 SFX 재생 중이면 무시
        if (_currentLoopSfx == type && loopSfxSource.isPlaying)
            return;

        loopSfxSource.clip = entry.clip;
        loopSfxSource.loop = true;
        _currentLoopScale = entry.volumeScale * extraVolumeScale;
        loopSfxSource.volume = Mathf.Clamp01(_currentLoopScale) * GetEffectiveSfxFactor();
        loopSfxSource.Play();

        _currentLoopSfx = type;
    }

    /// <summary>
    /// 루프 재생 중인 SFX 정지.
    /// </summary>
    public void StopSfxLoop()
    {
        if (loopSfxSource == null) return;

        loopSfxSource.Stop();
        loopSfxSource.clip = null;
        _currentLoopSfx = null;
    }

    /// <summary>
    /// 지정한 SFX 타입이 루프 재생 중인지 확인.
    /// </summary>
    public bool IsLoopSfxPlaying(SfxType type)
    {
        return _currentLoopSfx == type && loopSfxSource != null && loopSfxSource.isPlaying;
    }

    // ===================== 자동 버튼 등록 =====================

    public void AutoRegisterAllButtons()
    {
        var buttons = FindObjectsOfType<Button>(true);
        int registered = 0;
        foreach (var btn in buttons)
        {
            if (RegisterButton(btn)) registered++;
        }
        if (registered > 0)
            Debug.Log($"[SoundManager] 자동 등록된 버튼: {registered}개");
    }

    public void RegisterButtonsInHierarchy(GameObject root)
    {
        if (root == null) return;
        var buttons = root.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            RegisterButton(btn);
        }
    }

    public bool RegisterButton(Button btn)
    {
        if (btn == null) return false;

        bool alreadyCustomClick = btn.GetComponent<UIButtonSfx>() != null;
        bool alreadyAutoClick = btn.GetComponent<UIButtonSfxTag>() != null;

        if (!alreadyCustomClick && !alreadyAutoClick)
        {
            btn.gameObject.AddComponent<UIButtonSfxTag>();
            btn.onClick.AddListener(() => PlaySfx(SfxType.ButtonClick));
        }

        if (btn.GetComponent<UIButtonHoverSfx>() == null)
        {
            btn.gameObject.AddComponent<UIButtonHoverSfx>();
        }

        return !alreadyCustomClick && !alreadyAutoClick;
    }

    // ===================== 볼륨 / Mute =====================

    public void SetMasterVolume(float volume) { _masterVolume = Mathf.Clamp01(volume); ApplyBgmVolume(); ApplyLoopSfxVolume(); }
    public void SetBgmVolume(float volume)    { _bgmVolume = Mathf.Clamp01(volume); ApplyBgmVolume(); }
    public void SetSfxVolume(float volume)    { _sfxVolume = Mathf.Clamp01(volume); ApplyLoopSfxVolume(); }

    public void SetMasterMuted(bool muted) { _isMasterMuted = muted; ApplyBgmVolume(); ApplyLoopSfxVolume(); }
    public void SetBgmMuted(bool muted)    { _isBgmMuted = muted; ApplyBgmVolume(); }
    public void SetSfxMuted(bool muted)    { _isSfxMuted = muted; ApplyLoopSfxVolume(); }

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

    private void ApplyBgmVolume()
    {
        if (_currentBgmSource == null) return;
        if (_fadeCo != null) return;
        _currentBgmSource.volume = _currentBgmScale * GetEffectiveBgmFactor();
    }

    /// <summary>
    /// 루프 재생 중인 SFX 볼륨을 최신 SFX/Master 볼륨으로 갱신.
    /// </summary>
    private void ApplyLoopSfxVolume()
    {
        if (loopSfxSource == null || !loopSfxSource.isPlaying) return;
        loopSfxSource.volume = Mathf.Clamp01(_currentLoopScale) * GetEffectiveSfxFactor();
    }

    private void EnsureAudioSources()
    {
        if (bgmSourceA == null) bgmSourceA = gameObject.AddComponent<AudioSource>();
        if (bgmSourceB == null) bgmSourceB = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (loopSfxSource == null) loopSfxSource = gameObject.AddComponent<AudioSource>();

        foreach (var src in new[] { bgmSourceA, bgmSourceB })
        {
            src.playOnAwake = false;
            src.loop = true;
            src.volume = 0f;
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        loopSfxSource.playOnAwake = false;
        loopSfxSource.loop = true;
        loopSfxSource.volume = 0f;
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

    private void BuildSfxMap()
    {
        _sfxMap = new Dictionary<SfxType, SfxEntry>();
        foreach (var entry in sfxList)
        {
            if (entry == null || entry.clip == null) continue;
            _sfxMap[entry.type] = entry;
        }
    }
}