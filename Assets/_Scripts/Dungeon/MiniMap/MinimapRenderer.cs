using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 데이터를 받아 텍스처로 변환하고 RawImage에 출력한다.
/// - 바닥/벽 타일은 던전 구조가 바뀌지 않는 한 한 번만 그려서 _baseTexture/_basePixels에 캐싱.
/// - 방 발견 등으로 갱신이 필요할 땐 캐싱해둔 _basePixels를 복제해서 아이콘만 다시 그림 (Refresh).
///   (_baseTexture.GetPixels()를 매번 다시 호출하지 않도록 배열 자체를 들고 있음 - GC 할당 절감)
/// </summary>
public class MinimapRenderer : MonoBehaviour
{
    [SerializeField] private RawImage _minimapRawImage;
    [SerializeField] private MinimapIconManager _iconManager;

    private MinimapTextureBuilder _textureBuilder = new MinimapTextureBuilder();
    private MinimapData _currentData;
    private Texture2D _baseTexture;
    private Texture2D _displayTexture;
    private Color[] _basePixels;

    /// <summary>
    /// 미니맵 데이터를 기반으로 베이스 텍스처를 새로 생성하고 RawImage에 반영한다.
    /// </summary>
    public void Render(MinimapData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[MinimapRenderer] 렌더링할 미니맵 데이터가 null입니다.");
            return;
        }

        _currentData = data;

        if (_baseTexture != null)
        {
            Destroy(_baseTexture);
        }

        if (_displayTexture != null)
        {
            Destroy(_displayTexture);
        }

        _baseTexture = _textureBuilder.BuildBase(_currentData);
        _basePixels = _baseTexture.GetPixels(); // 여기서 딱 한 번만 뽑아서 배열로 캐싱

        RebuildDisplayTexture();

        Debug.Log("[MinimapRenderer] 미니맵 렌더링 완료");
    }

    /// <summary>
    /// 방 상태 변화 등이 일어났을 때, 베이스(바닥/벽)는 그대로 두고 아이콘만 다시 그린다.
    /// </summary>
    public void Refresh()
    {
        if (_currentData == null || _basePixels == null)
        {
            return;
        }

        RebuildDisplayTexture();
    }

    /// <summary>
    /// 캐싱된 베이스 픽셀 배열을 복제한 뒤 그 위에 아이콘을 그려서 화면에 표시할 텍스처를 갱신한다.
    /// </summary>
    private void RebuildDisplayTexture()
    {
        if (_displayTexture != null)
        {
            Destroy(_displayTexture);
        }

        _displayTexture = new Texture2D(_baseTexture.width, _baseTexture.height, TextureFormat.RGBA32, false);
        _displayTexture.filterMode = FilterMode.Point;
        _displayTexture.wrapMode = TextureWrapMode.Clamp;

        var iconMap = _iconManager != null ? _iconManager.GetIconMap() : null;
        var unvisitedIcon = _iconManager != null ? _iconManager.GetUnvisitedTexture() : null;

        _textureBuilder.DrawIconsOntoPixels(
            _displayTexture,
            _basePixels,
            _currentData.Rooms,
            _currentData.MinX,
            _currentData.MinY,
            iconMap,
            unvisitedIcon);

        if (_minimapRawImage != null)
        {
            _minimapRawImage.texture = _displayTexture;
        }

        Debug.Log("[MinimapRenderer] 미니맵 텍스처 갱신 완료");
    }

    private void OnDestroy()
    {
        if (_baseTexture != null)
        {
            Destroy(_baseTexture);
        }

        if (_displayTexture != null)
        {
            Destroy(_displayTexture);
        }
    }
}