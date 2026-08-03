using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 씬에서 공통으로 사용하는 UI 배경 Blur 관리자.
///
/// 동작 순서:
/// 1. 현재 씬의 Camera.main 탐색
/// 2. QuestListUI 패널 비활성화
/// 3. 현재 화면 캡처
/// 4. Horizontal / Vertical Gaussian Blur 적용
/// 5. Blur RawImage와 DimBackground 표시
///
/// Blur 요청이 모두 해제되면:
/// 1. Blur RawImage와 DimBackground 숨김
/// 2. QuestListUI 패널 다시 활성화
///
/// 사용 방법:
/// UIBackgroundBlurManager.Instance?.Show();
/// UIBackgroundBlurManager.Instance?.Hide();
///
/// 씬 전환 시:
/// UIBackgroundBlurManager.Instance?.ForceHide();
/// </summary>
public sealed class UIBackgroundBlurManager : MonoBehaviour
{
    public static UIBackgroundBlurManager Instance { get; private set; }

    private static readonly int BlurRadiusId =
        Shader.PropertyToID("_BlurRadius");

    [Header("Capture Camera")]
    [Tooltip("Blur 화면을 캡처할 전용 카메라입니다.")]
    [SerializeField]
    private Camera _captureCamera;

    [Header("Blur Output UI")]
    [Tooltip("Blur 결과를 화면 전체에 표시할 RawImage입니다.")]
    [SerializeField]
    private RawImage _blurredBackground;

    [Tooltip("Blur 위에 표시할 반투명 어두운 배경입니다.")]
    [SerializeField]
    private GameObject _dimBackground;

    [Header("Blur Material")]
    [Tooltip("Horizontal/Vertical 두 개의 Pass가 포함된 Gaussian Blur Material입니다.")]
    [SerializeField]
    private Material _blurMaterial;

    [Header("Blur Quality")]
    [Tooltip("화면 해상도를 나누어 Blur를 처리합니다. 2를 권장합니다.")]
    [SerializeField, Range(1, 8)]
    private int _downsample = 2;

    [Tooltip("Horizontal/Vertical Blur 반복 횟수입니다.")]
    [SerializeField, Range(1, 4)]
    private int _iterations = 2;

    [Tooltip("기본 Blur 반경입니다.")]
    [SerializeField, Range(0.1f, 5f)]
    private float _blurRadius = 1.2f;

    private RenderTexture _captureTexture;
    private RenderTexture _blurTextureA;
    private RenderTexture _blurTextureB;

    private int _cachedWidth;
    private int _cachedHeight;

    // 여러 UI가 Blur를 동시에 요청할 경우를 위한 요청 횟수
    private int _requestCount;

    private bool _isVisible;

    public bool IsVisible => _isVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeCaptureCamera();
        InitializeOutputUI();
    }

    /// <summary>
    /// 캡처 카메라를 비활성 상태로 초기화합니다.
    /// 캡처 시 Camera.Render()로만 직접 렌더링합니다.
    /// </summary>
    private void InitializeCaptureCamera()
    {
        if (_captureCamera == null)
        {
            return;
        }

        _captureCamera.enabled = false;
        _captureCamera.targetTexture = null;
    }

    /// <summary>
    /// Blur 출력 UI를 숨긴 상태로 초기화합니다.
    /// </summary>
    private void InitializeOutputUI()
    {
        if (_blurredBackground != null)
        {
            _blurredBackground.texture = null;
            _blurredBackground.gameObject.SetActive(false);
        }

        if (_dimBackground != null)
        {
            _dimBackground.SetActive(false);
        }

        _isVisible = false;
        _requestCount = 0;
    }

    /// <summary>
    /// Blur 표시를 요청합니다.
    ///
    /// 첫 번째 요청일 때만 화면을 캡처하고 Blur를 생성합니다.
    /// 이미 Blur가 표시 중이면 요청 횟수만 증가합니다.
    /// </summary>
    public void Show()
    {
        if (!ValidateReferences())
        {
            return;
        }

        Camera sourceCamera = FindSourceCamera();

        if (sourceCamera == null)
        {
            Debug.LogWarning(
                "[UIBackgroundBlurManager] " +
                "현재 씬에서 MainCamera 태그가 지정된 활성 카메라를 찾지 못했습니다.",
                this
            );

            return;
        }

        _requestCount++;

        // 이미 Blur가 켜져 있다면 다시 캡처하지 않습니다.
        if (_isVisible)
        {
            return;
        }

        /*
         * QuestList를 먼저 숨겨야 캡처 화면에 포함되지 않습니다.
         * QuestListUI에 이미 존재하는 Close()를 사용합니다.
         */
        HideQuestList();

        CaptureAndBlur(sourceCamera);
        SetBlurVisible(true);
    }

    /// <summary>
    /// Blur 요청을 하나 해제합니다.
    ///
    /// 모든 요청이 해제되어 요청 횟수가 0이 되었을 때만
    /// 실제 Blur를 숨기고 QuestList를 다시 표시합니다.
    /// </summary>
    public void Hide()
    {
        if (_requestCount <= 0)
        {
            _requestCount = 0;
            return;
        }

        _requestCount--;

        if (_requestCount > 0)
        {
            return;
        }

        SetBlurVisible(false);
        ShowQuestList();
    }

    /// <summary>
    /// 현재 카메라 화면을 다시 캡처하여 Blur 이미지를 갱신합니다.
    ///
    /// Blur가 켜진 상태에서 카메라 화면이 바뀌었을 때 사용할 수 있습니다.
    /// 요청 횟수는 변경하지 않습니다.
    /// </summary>
    public void Refresh()
    {
        if (!ValidateReferences())
        {
            return;
        }

        Camera sourceCamera = FindSourceCamera();

        if (sourceCamera == null)
        {
            Debug.LogWarning(
                "[UIBackgroundBlurManager] " +
                "Blur를 갱신할 Main Camera를 찾지 못했습니다.",
                this
            );

            return;
        }

        HideQuestList();

        CaptureAndBlur(sourceCamera);
        SetBlurVisible(true);
    }

    /// <summary>
    /// 요청 횟수와 관계없이 Blur를 즉시 종료합니다.
    ///
    /// 씬 전환, UI 강제 초기화 등의 상황에서 사용합니다.
    /// </summary>
    public void ForceHide()
    {
        _requestCount = 0;

        SetBlurVisible(false);
        ShowQuestList();
    }

    /// <summary>
    /// 호출 시점의 현재 Main Camera를 반환합니다.
    /// DontDestroyOnLoad 구조이므로 매번 새로 검색합니다.
    /// </summary>
    private Camera FindSourceCamera()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return null;
        }

        if (!mainCamera.isActiveAndEnabled)
        {
            return null;
        }

        if (mainCamera == _captureCamera)
        {
            return null;
        }

        return mainCamera;
    }

    /// <summary>
    /// 현재 카메라 화면을 RenderTexture에 캡처하고
    /// Horizontal/Vertical Gaussian Blur를 적용합니다.
    /// </summary>
    private void CaptureAndBlur(Camera sourceCamera)
    {
        CreateRenderTexturesIfNeeded();
        CopyCameraSettings(sourceCamera);

        // 현재 월드 화면을 Capture Texture에 한 번 렌더링합니다.
        _captureCamera.targetTexture = _captureTexture;
        _captureCamera.Render();
        _captureCamera.targetTexture = null;

        // 원본 캡처 이미지를 Blur 작업용 Texture로 복사합니다.
        Graphics.Blit(
            _captureTexture,
            _blurTextureA
        );

        for (int i = 0; i < _iterations; i++)
        {
            float currentRadius =
                _blurRadius + i * 0.5f;

            _blurMaterial.SetFloat(
                BlurRadiusId,
                currentRadius
            );

            // Pass 0: Horizontal Blur
            Graphics.Blit(
                _blurTextureA,
                _blurTextureB,
                _blurMaterial,
                0
            );

            // Pass 1: Vertical Blur
            Graphics.Blit(
                _blurTextureB,
                _blurTextureA,
                _blurMaterial,
                1
            );
        }

        _blurredBackground.texture = _blurTextureA;
    }

    /// <summary>
    /// 현재 Main Camera의 설정과 Transform을 캡처 카메라에 복사합니다.
    /// </summary>
    private void CopyCameraSettings(Camera sourceCamera)
    {
        _captureCamera.CopyFrom(sourceCamera);

        /*
         * CopyFrom 이후 캡처 카메라가 일반 카메라처럼 동작하지 않도록
         * 필요한 값을 다시 정리합니다.
         */
        _captureCamera.enabled = false;
        _captureCamera.targetTexture = null;

        _captureCamera.transform.SetPositionAndRotation(
            sourceCamera.transform.position,
            sourceCamera.transform.rotation
        );
    }

    /// <summary>
    /// Blur 출력 RawImage와 DimBackground를 표시하거나 숨깁니다.
    /// </summary>
    private void SetBlurVisible(bool visible)
    {
        _isVisible = visible;

        if (_blurredBackground != null)
        {
            if (visible)
            {
                _blurredBackground.texture = _blurTextureA;
                _blurredBackground.gameObject.SetActive(true);
            }
            else
            {
                _blurredBackground.texture = null;
                _blurredBackground.gameObject.SetActive(false);
            }
        }

        if (_dimBackground != null)
        {
            _dimBackground.SetActive(visible);
        }
    }

    /// <summary>
    /// Blur가 표시되기 전에 QuestList 패널을 숨깁니다.
    /// </summary>
    private void HideQuestList()
    {
        if (QuestListUI.Instance != null)
        {
            QuestListUI.Instance.Close();
        }

        ShowMessageManager.Instance?.BlockByUI();
    }

    private void ShowQuestList()
    {
        if (QuestListUI.Instance != null)
        {
            QuestListUI.Instance.Open();
        }

        ShowMessageManager.Instance?.UnblockByUI();
    }

    /// <summary>
    /// 필수 참조가 정상적으로 연결됐는지 확인합니다.
    /// </summary>
    private bool ValidateReferences()
    {
        if (_captureCamera == null)
        {
            Debug.LogError(
                "[UIBackgroundBlurManager] Capture Camera가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (_blurredBackground == null)
        {
            Debug.LogError(
                "[UIBackgroundBlurManager] Blurred Background RawImage가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (_blurMaterial == null)
        {
            Debug.LogError(
                "[UIBackgroundBlurManager] Blur Material이 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (_blurMaterial.passCount < 2)
        {
            Debug.LogError(
                "[UIBackgroundBlurManager] Blur Material에 " +
                "Horizontal/Vertical 두 개의 Pass가 필요합니다.",
                this
            );

            return false;
        }

        return true;
    }

    /// <summary>
    /// 현재 화면 크기와 Downsample 값에 맞는 RenderTexture를 생성합니다.
    /// 화면 크기가 변하지 않았다면 기존 Texture를 재사용합니다.
    /// </summary>
    private void CreateRenderTexturesIfNeeded()
    {
        int width = Mathf.Max(
            1,
            Screen.width / _downsample
        );

        int height = Mathf.Max(
            1,
            Screen.height / _downsample
        );

        bool textureSizeUnchanged =
            _captureTexture != null &&
            _blurTextureA != null &&
            _blurTextureB != null &&
            _cachedWidth == width &&
            _cachedHeight == height;

        if (textureSizeUnchanged)
        {
            return;
        }

        ReleaseRenderTextures();

        _cachedWidth = width;
        _cachedHeight = height;

        RenderTextureDescriptor captureDescriptor =
            new RenderTextureDescriptor(
                width,
                height,
                RenderTextureFormat.Default,
                24
            );

        captureDescriptor.msaaSamples = 1;
        captureDescriptor.useMipMap = false;
        captureDescriptor.autoGenerateMips = false;

        _captureTexture = CreateRenderTexture(
            captureDescriptor,
            "RT_UIBlur_Capture"
        );

        RenderTextureDescriptor blurDescriptor =
            captureDescriptor;

        // Blur 처리용 Texture에는 Depth Buffer가 필요하지 않습니다.
        blurDescriptor.depthBufferBits = 0;

        _blurTextureA = CreateRenderTexture(
            blurDescriptor,
            "RT_UIBlur_A"
        );

        _blurTextureB = CreateRenderTexture(
            blurDescriptor,
            "RT_UIBlur_B"
        );
    }

    private static RenderTexture CreateRenderTexture(
        RenderTextureDescriptor descriptor,
        string textureName
    )
    {
        RenderTexture renderTexture =
            new RenderTexture(descriptor)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

        renderTexture.Create();

        return renderTexture;
    }

    /// <summary>
    /// 생성한 모든 RenderTexture를 해제합니다.
    /// </summary>
    private void ReleaseRenderTextures()
    {
        ReleaseRenderTexture(ref _captureTexture);
        ReleaseRenderTexture(ref _blurTextureA);
        ReleaseRenderTexture(ref _blurTextureB);

        _cachedWidth = 0;
        _cachedHeight = 0;
    }

    private static void ReleaseRenderTexture(
        ref RenderTexture renderTexture
    )
    {
        if (renderTexture == null)
        {
            return;
        }

        if (renderTexture.IsCreated())
        {
            renderTexture.Release();
        }

        Destroy(renderTexture);
        renderTexture = null;
    }

    private void OnDestroy()
    {
        /*
         * 중복 생성되어 제거되는 Manager가
         * 실제 Instance를 초기화하지 않도록 검사합니다.
         */
        if (Instance == this)
        {
            Instance = null;
        }

        ReleaseRenderTextures();
    }
}