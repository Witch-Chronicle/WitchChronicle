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
            if (!_isPlayerNear)
            {
                ApplyVisibility(false);
                return;
            }

            if (_worldTarget == null || _mainCamera == null)
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

        /// <summary>
        /// FarmZoneTrigger가 호출: 플레이어가 팜 존 안에 있는지
        /// </summary>
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

        // 씨앗 이름 (첫 줄) — "감자"
        if (_growingSeedName != null)
            _growingSeedName.text = seed.seedName;
    }

    // 상태 + 타이머 (두번째 줄) — "자라는 중\n02:34"
    if (_timerText != null)
    {
        int total = Mathf.CeilToInt(remainingSeconds);
        int m = total / 60;
        int s = total % 60;
        _timerText.text = $"자라는 중\n{m:D2}:{s:D2}";
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

        // 씨앗 이름 (첫 줄) — "감자"
        if (_readySeedName != null)
            _readySeedName.text = seed.seedName;
    }

    // 상태 + 개수 (두번째 줄) — "수확 가능\n×15"
    if (_readyCountText != null)
        _readyCountText.text = $"수확 가능\n×{pendingCount}";
}
        private void HideAll()
        {
            if (_growingRoot != null) _growingRoot.SetActive(false);
            if (_readyRoot != null) _readyRoot.SetActive(false);
        }
    }
}