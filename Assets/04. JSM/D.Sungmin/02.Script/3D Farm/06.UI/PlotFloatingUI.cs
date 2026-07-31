using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 밭 슬롯 위에 떠있는 월드스페이스 UI
    /// 플레이어가 근접했을 때만 표시 (상태별 정보: 씨앗 종류 / 남은 시간 / 대기 개수)
    /// 항상 카메라 향해 회전
    /// </summary>
    public class PlotFloatingUI : MonoBehaviour
    {
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

        [Header("옵션")]
        [SerializeField] private bool _billboardToCamera = true;

        private Camera _mainCamera;

        // 근접 여부 및 마지막 상태 캐시
        private bool _isPlayerNear;
        private PlotState _cachedState = PlotState.Empty;
        private SeedData _cachedSeed;
        private float _cachedRemaining;
        private int _cachedPending;
        private bool _hasCache;

        private void Awake()
        {
            _mainCamera = Camera.main;
            HideAll();
        }

        private void LateUpdate()
        {
            if (_billboardToCamera && _mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    transform.position - _mainCamera.transform.position);
            }
        }

        /// <summary>
        /// PlotInteractor에서 호출. 근접/이탈 시 표시 여부 갱신.
        /// </summary>
        public void SetPlayerNear(bool near)
        {
            if (_isPlayerNear == near) return;
            _isPlayerNear = near;

            if (!_isPlayerNear)
            {
                HideAll();
                return;
            }

            // 다시 근접했을 때 마지막 상태로 복원
            if (_hasCache)
                ApplyState(_cachedState, _cachedSeed, _cachedRemaining, _cachedPending);
        }

        public void Refresh(PlotState state, SeedData seed, float remainingSeconds, int pendingCount)
        {
            // 값 캐싱
            _cachedState = state;
            _cachedSeed = seed;
            _cachedRemaining = remainingSeconds;
            _cachedPending = pendingCount;
            _hasCache = true;

            // 근접 상태일 때만 실제 UI 갱신
            if (_isPlayerNear)
                ApplyState(state, seed, remainingSeconds, pendingCount);
            else
                HideAll();
        }

        private void ApplyState(PlotState state, SeedData seed, float remainingSeconds, int pendingCount)
        {
            switch (state)
            {
                case PlotState.Locked:
                case PlotState.Empty:
                    HideAll();
                    break;

                case PlotState.Growing:
    if (seed == null) { HideAll(); return; }
    _growingRoot.SetActive(true);
    _readyRoot.SetActive(false);

    if (_growingSeedIcon != null && seed.seedSprite != null)
        _growingSeedIcon.sprite = seed.seedSprite;
    if (_growingSeedName != null)
        _growingSeedName.text = $"{seed.harvestName} \n자라는 중...";  // ← 여기
    if (_timerText != null)
        _timerText.text = FormatTime(remainingSeconds);
    break;

case PlotState.ReadyToHarvest:
    if (seed == null) { HideAll(); return; }
    _growingRoot.SetActive(false);
    _readyRoot.SetActive(true);

    if (_readySeedIcon != null && seed.harvestSprite != null)
        _readySeedIcon.sprite = seed.harvestSprite;
    if (_readySeedName != null)
        _readySeedName.text = $"{seed.harvestName} \n수확 가능";  // ← 여기
    if (_readyCountText != null)
        _readyCountText.text = $"x {pendingCount}";
    break;
            }
        }

        private void HideAll()
        {
            if (_growingRoot != null) _growingRoot.SetActive(false);
            if (_readyRoot != null) _readyRoot.SetActive(false);
        }

        private string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int mm = Mathf.FloorToInt(seconds / 60f);
            int ss = Mathf.FloorToInt(seconds % 60f);
            return $"{mm:D2}:{ss:D2}";
        }
    }
}