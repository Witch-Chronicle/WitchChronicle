using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 데이터를 받아 텍스처로 변환하고 RawImage에 출력한다.
/// </summary>
public class MinimapRenderer : MonoBehaviour
{
    [SerializeField] private RawImage _minimapRawImage;
    [SerializeField] private MinimapIconManager _iconManager;

    private MinimapTextureBuilder _textureBuilder = new MinimapTextureBuilder();
    private MinimapData _currentData;
    private Texture2D _currentTexture;

    /// <summary>
    /// 미니맵 데이터를 기반으로 텍스처를 생성하고 RawImage에 반영한다.
    /// </summary>
    public void Render(MinimapData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[MinimapRenderer] 렌더링할 미니맵 데이터가 null입니다.");
            return;
        }

        _currentData = data;

        if (_currentTexture != null)
        {
            Destroy(_currentTexture);
        }

        var iconMap = _iconManager != null ? _iconManager.GetIconMap() : null;
        var unvisitedIcon = _iconManager != null ? _iconManager.GetUnvisitedTexture() : null;
        
        _currentTexture = _textureBuilder.Build(_currentData, iconMap, unvisitedIcon);

        if (_minimapRawImage != null)
        {
            _minimapRawImage.texture = _currentTexture;
            _minimapRawImage.uvRect = new Rect(0f, 0f, 0.25f, 0.25f);
        }

        Debug.Log("[MinimapRenderer] 미니맵 렌더링 완료");
    }

    /// <summary>
    /// 방 상태 변화 등이 일어났을 때 미니맵 텍스처를 다시 그린다.
    /// </summary>
    public void Refresh()
    {
        if (_currentData == null)
        {
            return;
        }

        if (_currentTexture != null)
        {
            Destroy(_currentTexture);
        }

        var iconMap = _iconManager != null ? _iconManager.GetIconMap() : null;
        var unvisitedIcon = _iconManager != null ? _iconManager.GetUnvisitedTexture() : null;
        
        _currentTexture = _textureBuilder.Build(_currentData, iconMap, unvisitedIcon);

        if (_minimapRawImage != null)
        {
            _minimapRawImage.texture = _currentTexture;
        }

        Debug.Log("[MinimapRenderer] 미니맵 텍스처 갱신 완료");
    }

    private void OnDestroy()
    {
        if (_currentTexture != null)
        {
            Destroy(_currentTexture);
        }
    }
}