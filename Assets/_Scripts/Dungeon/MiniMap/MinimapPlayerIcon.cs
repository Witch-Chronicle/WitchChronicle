using UnityEngine;

/// <summary>
/// 미니맵 상에서 플레이어 아이콘의 위치와 회전을 실시간으로 갱신한다.
/// </summary>
public class MinimapPlayerIcon : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _useZForVertical = false;
    [Tooltip("플레이어의 회전 값을 가져올 때 사용할 축 (3D 탑뷰는 Y, 2D/탑뷰 스프라이트는 Z 등)")]
    [SerializeField] private RotationAxis _rotationAxis = RotationAxis.Y;
    [Tooltip("스프라이트의 기본 방향에 따라 추가 회전 오프셋을 조절합니다 (예: 0, 90, 180 등).")]
    [SerializeField] private float _rotationOffset = 0f;

    public enum RotationAxis
    {
        Y,
        Z
    }

    private Transform _playerTransform;
    private RectTransform _rectTransform;
    private float _tileSize = 1f;
    private bool _isInitialized;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
    }

    /// <summary>
    /// 플레이어 트랜스폼과 맵 정보를 초기화한다.
    /// </summary>
    public void Initialize(Transform playerTransform, float tileSize)
    {
        _playerTransform = playerTransform;
        _tileSize = tileSize > 0f ? tileSize : 1f;
        _isInitialized = true;

        Debug.Log($"[MinimapPlayerIcon] 플레이어 아이콘 초기화 완료. 대상: {(_playerTransform != null ? _playerTransform.name : "Null")}");
    }

    /// <summary>
    /// 현재 미니맵 뷰(uvRect) 비율에 맞춰 플레이어 아이콘의 위치와 회전을 갱신한다.
    /// 전체 맵 상태일 경우 아이콘을 숨긴다.
    /// </summary>
    public void UpdateView(
        Rect uvRect,
        int minX,
        int minY,
        int maxX,
        int maxY,
        RectTransform parentRect)
    {
        if (!_isInitialized || _playerTransform == null || _rectTransform == null || parentRect == null)
        {
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        int totalWidth = maxX - minX + 1;
        int totalHeight = maxY - minY + 1;

        if (totalWidth <= 0 || totalHeight <= 0)
        {
            return;
        }

        float playerWorldX = _playerTransform.position.x;
        float playerWorldY = _useZForVertical ? _playerTransform.position.z : _playerTransform.position.y;

        float playerGridX = playerWorldX / _tileSize;
        float playerGridY = playerWorldY / _tileSize;

        float normX = (playerGridX - minX + 0.5f) / totalWidth;
        float normY = (playerGridY - minY + 0.5f) / totalHeight;

        float viewNormX = (normX - uvRect.x) / uvRect.width;
        float viewNormY = (normY - uvRect.y) / uvRect.height;

        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        float localX = (viewNormX - 0.5f) * parentWidth;
        float localY = (viewNormY - 0.5f) * parentHeight;

        _rectTransform.anchoredPosition = new Vector2(localX, localY);

        // 플레이어의 회전 방향을 미니맵 UI 회전(Z축)에 반영
        float playerAngle = 0f;
        if (_rotationAxis == RotationAxis.Y)
        {
            playerAngle = _playerTransform.eulerAngles.y;
        }
        else
        {
            playerAngle = _playerTransform.eulerAngles.z;
        }

        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, -playerAngle + _rotationOffset);
    }
}