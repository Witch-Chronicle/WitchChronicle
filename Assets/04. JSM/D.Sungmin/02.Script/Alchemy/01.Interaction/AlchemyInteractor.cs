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
            SetPlayerLocked(true);

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

        private void SetPlayerLocked(bool locked)
        {
            if (_playerRef == null) return;

            // 이동 스크립트 잠금 (Move 호출 자체를 막음)
            // 우리 캐릭터(PlayerController)와 StarterAssets 양쪽 모두 대응
            string[] moverTypeNames = { "PlayerController", "ThirdPersonController", "StarterAssetsInputs" };

            for (int i = 0; i < moverTypeNames.Length; i++)
            {
                var mover = _playerRef.GetComponent(moverTypeNames[i]) as MonoBehaviour;
                if (mover != null) mover.enabled = !locked;
            }

            // CharacterController도 잠금 (물리 이동 봉인)
            var cc = _playerRef.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = !locked;

            // 애니메이션 잠금 (걷기 애니 재생 방지)
            var animator = _playerRef.GetComponentInChildren<Animator>();
            if (animator != null) animator.enabled = !locked;
        }

        private void OnPanelClosed()
        {
            _isUsing = false;

            SetPlayerLocked(false);

            if (_cameraController != null)
                _cameraController.SwitchToPlayerView();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (_isPlayerNear && _interactPrompt != null)
                _interactPrompt.SetActive(true);
        }
    }
}