using UnityEngine;

/// <summary>
/// 미니맵 카메라가 플레이어 위치를 따라간다.
/// </summary>
public class MinimapCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _cameraHeight = 30f;

    [SerializeField] private GameObject _minimapPanel;


    private void Start()
    {
        if (_playerTransform == null)
        {
            PlayerInteractor player = FindAnyObjectByType<PlayerInteractor>();

            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (_playerTransform == null)
        {
            return;
        }

        transform.position = new Vector3(_playerTransform.position.x, _cameraHeight,  _playerTransform.position.z);

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