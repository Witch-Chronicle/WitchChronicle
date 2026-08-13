using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.IdleFarming
{
    public class PlotFloatingUI : MonoBehaviour
    {
        [Header("추적 대상")]
        [SerializeField] private Transform _worldTarget;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.5f, 0f);

        [Header("루트")]
        [SerializeField] private GameObject _growingRoot;
        [SerializeField] private GameObject _readyRoot;

        [Header("Growing 표시")]
        [SerializeField] private Image _growingSeedIcon;
        [SerializeField] private TextMeshProUGUI _growingSeedName;
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("ReadyToHarvest 표시")]
        [SerializeField] private Image _readySeedIcon;
        [SerializeField] private TextMeshProUGUI _readySeedName;
        [SerializeField] private TextMeshProUGUI _readyCountText;

        [Header("표시 제어")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private bool _startHidden = true;

        private Camera _mainCamera;
        private RectTransform _rect;
        private bool _isPlayerNear;

        // 일시정지 패널 캐시 (씬 로드 시 1회 탐색)
        private PauseController _pauseController;
        private bool _pauseControllerSearched;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _rect = GetComponent<RectTransform>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            _isPlayerNear = !_startHidden;
            ApplyVisibility(false);
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // 일시정지 패널이 켜져 있으면 무조건 숨김
            if (IsPausePanelOpen())
            {
                ApplyVisibility(false);
                return;
            }

            if (!_isPlayerNear)
            {
                ApplyVisibility(false);
                return;
            }

            if (_worldTarget == null)
            {
                ApplyVisibility(false);
                return;
            }

            Vector3 worldPos = _worldTarget.position + _worldOffset;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0f)
            {
                ApplyVisibility(false);
                return;
            }

            ApplyVisibility(true);
            _rect.position = screenPos;
        }

        private bool IsPausePanelOpen()
        {
            // 처음 한 번만 씬에서 찾아서 캐시 (비활성 오브젝트 포함해서 검색)
            if (!_pauseControllerSearched)
            {
                _pauseController = FindAnyPauseController();
                _pauseControllerSearched = true;
            }

            if (_pauseController == null) return false;
            return _pauseController.gameObject.activeInHierarchy;
        }

        private PauseController FindAnyPauseController()
        {
            // 비활성 오브젝트에 붙어있어도 찾을 수 있게 Resources 방식 사용
            var all = Resources.FindObjectsOfTypeAll<PauseController>();
            foreach (var pc in all)
            {
                if (pc.gameObject.scene.IsValid()) return pc;
            }
            return null;
        }

        private void ApplyVisibility(bool visible)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        // ====== 외부 API ======

        public void SetTarget(Transform target)
        {
            _worldTarget = target;
        }

        public void SetPlayerNear(bool near)
        {
            _isPlayerNear = near;
        }

        public void Refresh(PlotState state, SeedData seed, float remainingSeconds, int pendingCount)
        {
            switch (state)
            {
                case PlotState.Growing:
                    ShowGrowing(seed, remainingSeconds);
                    break;
                case PlotState.ReadyToHarvest:
                    ShowReady(seed, pendingCount);
                    break;
                default:
                    HideAll();
                    break;
            }
        }

        private void ShowGrowing(SeedData seed, float remainingSeconds)
        {
            if (_growingRoot != null) _growingRoot.SetActive(true);
            if (_readyRoot != null) _readyRoot.SetActive(false);

            if (seed != null)
            {
                if (_growingSeedIcon != null && seed.seedSprite != null)
                    _growingSeedIcon.sprite = seed.seedSprite;

                if (_growingSeedName != null)
                    _growingSeedName.text = seed.harvestName;
            }

            if (_timerText != null)
            {
                int total = Mathf.CeilToInt(remainingSeconds);
                int m = total / 60;
                int s = total % 60;
                _timerText.text = $"{m:D2}:{s:D2}";
            }
        }

        private void ShowReady(SeedData seed, int pendingCount)
        {
            if (_growingRoot != null) _growingRoot.SetActive(false);
            if (_readyRoot != null) _readyRoot.SetActive(true);

            if (seed != null)
            {
                if (_readySeedIcon != null && seed.harvestSprite != null)
                    _readySeedIcon.sprite = seed.harvestSprite;

                if (_readySeedName != null)
                    _readySeedName.text = seed.harvestName;
            }

            if (_readyCountText != null)
    _readyCountText.text = $"{pendingCount}개"; 
        }

        private void HideAll()
        {
            if (_growingRoot != null) _growingRoot.SetActive(false);
            if (_readyRoot != null) _readyRoot.SetActive(false);
        }
    }
}