using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 단일 RawImage의 uvRect를 조작하여 미니맵의 줌, 팬, 그리고 플레이어 중심 실시간 추적을 제어한다.
/// </summary>
public class MinimapUIController : MonoBehaviour, IDragHandler
{
    [Header("UI Targets")]
    [SerializeField] private RawImage _minimapRawImage;
    [SerializeField] private GameObject _minimapRootObject;

    [Header("Axis Settings")]
    [Tooltip("3D 탑뷰 게임이라서 바닥 세로축이 Z축인 경우 체크하세요.")]
    [SerializeField] private bool _useZForVertical = false;

    [Header("Initial Settings")]
    [SerializeField] private bool _startOpen = false;
    [SerializeField] private float _defaultZoomSize = 0.25f;

    [Header("Toggle Settings")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.M;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 0.1f;
    [SerializeField] private float _minZoomSize = 0.1f;
    [SerializeField] private float _maxZoomSize = 1.0f;

    [Header("Pan Settings")]
    [SerializeField] private float _panSensibility = 1.0f;

    private Transform _playerTransform;
    private int _minX;
    private int _minY;
    private int _maxX;
    private int _maxY;
    private float _tileSize = 1f;

    private Rect _currentUvRect = new Rect(0f, 0f, 1f, 1f);
    private bool _isMinimapActive;
    private bool _isInitialized = false;

    private void Start()
    {
        _isMinimapActive = _startOpen;

        if (_minimapRootObject != null)
        {
            _minimapRootObject.SetActive(_isMinimapActive);
        }

        if (_minimapRawImage != null)
        {
            _currentUvRect = _minimapRawImage.uvRect;
        }
    }

    private void Update()
    {
        HandleBattleSceneForceClose();
        HandleToggleInput();

        if (!_isMinimapActive || !_isInitialized)
        {
            return;
        }

        HandleZoomInput();
        UpdatePlayerTracking();
    }

    /// <summary>
    /// 미니맵 UI 컨트롤러를 초기화하고 플레이어 추적 및 초기 위치를 설정한다.
    /// </summary>
    public void Initialize(
        Transform playerTransform,
        int minX,
        int minY,
        int maxX,
        int maxY,
        float tileSize)
    {
        _playerTransform = playerTransform;
        _minX = minX;
        _minY = minY;
        _maxX = maxX;
        _maxY = maxY;
        _tileSize = tileSize > 0f ? tileSize : 1f;
        _isInitialized = true;

        _currentUvRect.width = _defaultZoomSize;
        _currentUvRect.height = _defaultZoomSize;

        if (_playerTransform != null)
        {
            CenterOnPosition(_playerTransform.position);
        }
        else
        {
            _currentUvRect.x = 0f;
            _currentUvRect.y = 0f;
            ApplyUvRect();
        }

        Debug.Log($"[MinimapUIController] 미니맵 UI 및 플레이어 추적 초기화 완료 (기본 줌: {_defaultZoomSize})");
    }

    /// <summary>
    /// 특정 월드 좌표가 미니맵 정중앙에 오도록 뷰를 이동시킨다.
    /// </summary>
    public void CenterOnPosition(Vector3 worldPosition)
    {
        int totalWidth = _maxX - _minX + 1;
        int totalHeight = _maxY - _minY + 1;

        if (totalWidth <= 0 || totalHeight <= 0)
        {
            return;
        }

        float playerWorldX = worldPosition.x;
        float playerWorldY = _useZForVertical ? worldPosition.z : worldPosition.y;

        float gridX = playerWorldX / _tileSize;
        float gridY = playerWorldY / _tileSize;

        float normX = (gridX - _minX + 0.5f) / totalWidth;
        float normY = (gridY - _minY + 0.5f) / totalHeight;

        float currentSize = _currentUvRect.width;

        _currentUvRect.x = Mathf.Clamp(normX - (currentSize * 0.5f), 0f, 1f - currentSize);
        _currentUvRect.y = Mathf.Clamp(normY - (currentSize * 0.5f), 0f, 1f - currentSize);

        ApplyUvRect();
    }

    /// <summary>
    /// 플레이어의 현재 위치를 매 프레임 추적하여 미니맵 중앙에 고정한다.
    /// </summary>
    private void UpdatePlayerTracking()
    {
        if (_playerTransform == null)
        {
            return;
        }

        CenterOnPosition(_playerTransform.position);
    }

    /// <summary>
    /// 마우스 드래그 입력을 감지하여 미니맵의 uvRect 위치를 이동시킨다.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_isMinimapActive || _minimapRawImage == null)
        {
            return;
        }

        float scaleFactorX = _currentUvRect.width / _minimapRawImage.rectTransform.rect.width;
        float scaleFactorY = _currentUvRect.height / _minimapRawImage.rectTransform.rect.height;

        _currentUvRect.x -= eventData.delta.x * scaleFactorX * _panSensibility;
        _currentUvRect.y -= eventData.delta.y * scaleFactorY * _panSensibility;

        _currentUvRect.x = Mathf.Clamp(_currentUvRect.x, 0f, 1f - _currentUvRect.width);
        _currentUvRect.y = Mathf.Clamp(_currentUvRect.y, 0f, 1f - _currentUvRect.height);

        ApplyUvRect();
    }

    /// <summary>
    /// 마우스 스크롤 입력을 감지하여 뷰 크기(줌)를 조절한다.
    /// </summary>
    private void HandleZoomInput()
    {
        float scrollDelta = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scrollDelta) < 0.01f)
        {
            return;
        }

        if (_minimapRawImage == null)
        {
            return;
        }

        float previousWidth = _currentUvRect.width;
        float newSize = previousWidth - (scrollDelta * _zoomSpeed);
        newSize = Mathf.Clamp(newSize, _minZoomSize, _maxZoomSize);

        if (Mathf.Approximately(newSize, 1.0f))
        {
            _currentUvRect = new Rect(0f, 0f, 1f, 1f);
        }
        else
        {
            float centerX = _currentUvRect.x + previousWidth * 0.5f;
            float centerY = _currentUvRect.y + previousWidth * 0.5f;

            _currentUvRect.width = newSize;
            _currentUvRect.height = newSize;
            _currentUvRect.x = Mathf.Clamp(centerX - newSize * 0.5f, 0f, 1f - newSize);
            _currentUvRect.y = Mathf.Clamp(centerY - newSize * 0.5f, 0f, 1f - newSize);
        }

        ApplyUvRect();
    }

    /// <summary>
    /// 변경된 uvRect를 RawImage에 적용하고 방 아이콘들의 위치를 갱신한다.
    /// </summary>
    private void ApplyUvRect()
    {
        if (_minimapRawImage != null)
        {
            _minimapRawImage.uvRect = _currentUvRect;
        }

        if (MinimapIconManager.Instance != null && _minimapRawImage != null)
        {
            MinimapIconManager.Instance.UpdateAllIconViews(
                _currentUvRect,
                _minX,
                _minY,
                _maxX,
                _maxY,
                _minimapRawImage.rectTransform);
        }
    }

    /// <summary>
    /// 미니맵 토글 키 입력을 처리한다. Battle 씬이 로드되어 있으면 무시.
    /// </summary>
    private void HandleToggleInput()
    {
        if (!Input.GetKeyDown(_toggleKey))
        {
            return;
        }

        if (_minimapRootObject == null)
        {
            return;
        }

        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsInBattleScene())
        {
            return;
        }

        _isMinimapActive = !_isMinimapActive;
        _minimapRootObject.SetActive(_isMinimapActive);

        Debug.Log($"[MinimapUIController] 미니맵 표시 상태 변경: {(_isMinimapActive ? "켜짐" : "꺼짐")}");
    }

    /// <summary>
    /// Battle 씬이 로드되어 있으면 미니맵을 강제로 닫음 (열려있던 상태였어도 즉시 비활성화).
    /// </summary>
    private void HandleBattleSceneForceClose()
    {
        if (_isMinimapActive == false) return;
        if (_minimapRootObject == null) return;

        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsInBattleScene())
        {
            _isMinimapActive = false;
            _minimapRootObject.SetActive(false);

            Debug.Log("[MinimapUIController] Battle 씬 감지 - 미니맵 강제 비활성화");
        }
    }
}