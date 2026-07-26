using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform; // 추적할 플레이어 트랜스폼
    [SerializeField] private float _cameraHeight = 30f;   // 미니맵 카메라의 고정 높이

    [SerializeField] private GameObject _minimapPanel;

    private void LateUpdate()
    {
        if (_playerTransform == null) 
        {
            _playerTransform = FindAnyObjectByType<PlayerInteractor>().transform;
        }

        // 플레이어의 X, Z 좌표만 따라가고 Y축 높이는 고정하여 위에서 아래로 내려다보는 시점을 유지함.
        Vector3 targetPosition = new Vector3(_playerTransform.position.x, _cameraHeight, _playerTransform.position.z);
        transform.position = targetPosition;

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool shouldHide = SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsInBattleScene();

        if (_minimapPanel != null)
        {
            _minimapPanel.SetActive(!shouldHide);
        }
    }
}