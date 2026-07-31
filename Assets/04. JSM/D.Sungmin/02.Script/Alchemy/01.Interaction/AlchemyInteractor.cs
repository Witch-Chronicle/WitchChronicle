using UnityEngine;

namespace WitchChronicle.Alchemy
{
    [RequireComponent(typeof(Collider))]
    public class AlchemyInteractor : MonoBehaviour
    {
        [Header("상호작용")]
        [SerializeField] private KeyCode _interactKey = KeyCode.F;
        [SerializeField] private GameObject _interactPrompt;

        [Header("전환 대상")]
        [SerializeField] private AlchemyCameraController _cameraController;
        [SerializeField] private AlchemyPanel _alchemyPanel;

        [Header("플레이어 배치")]
        [SerializeField] private Transform _playerStandPoint;

        [Header("초기 모드")]
        [SerializeField] private AlchemyMode _defaultMode = AlchemyMode.Cooking;

        private bool _isPlayerNear;
        private bool _isUsing;
        private GameObject _playerRef;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            if (_interactPrompt != null) _interactPrompt.SetActive(false);
        }

        private void Update()
        {
            if (_isUsing) return;
            if (!_isPlayerNear) return;

            if (Input.GetKeyDown(_interactKey))
            {
                OpenAlchemy();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _isPlayerNear = true;
            _playerRef = other.gameObject;
            if (_interactPrompt != null && !_isUsing)
                _interactPrompt.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _isPlayerNear = false;
            if (_interactPrompt != null) _interactPrompt.SetActive(false);
        }

        private void OpenAlchemy()
        {
            _isUsing = true;

            MovePlayerToStand();
            SetCharacterControllerEnabled(false);

            if (_interactPrompt != null) _interactPrompt.SetActive(false);

            if (_cameraController != null)
                _cameraController.SwitchToAlchemyView();

            if (_alchemyPanel != null)
                _alchemyPanel.Open(_defaultMode, OnPanelClosed);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void MovePlayerToStand()
        {
            if (_playerRef == null || _playerStandPoint == null) return;

            var playerTf = _playerRef.transform;

            var cc = _playerRef.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTf.position = _playerStandPoint.position;
            playerTf.rotation = _playerStandPoint.rotation;

            if (cc != null) cc.enabled = true;
        }

        private void SetCharacterControllerEnabled(bool enable)
        {
            if (_playerRef == null) return;
            var cc = _playerRef.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = enable;
        }

        private void OnPanelClosed()
        {
            _isUsing = false;

            SetCharacterControllerEnabled(true);

            if (_cameraController != null)
                _cameraController.SwitchToPlayerView();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (_isPlayerNear && _interactPrompt != null)
                _interactPrompt.SetActive(true);
        }
    }
}