using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.IdleFarming
{
    /// <summary>
    /// 밭 슬롯 위에 떠있는 월드스페이스 UI
    /// 상태별 정보 표시 (씨앗 종류 / 남은 시간 / 대기 개수)
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

        public void Refresh(PlotState state, SeedData seed, float remainingSeconds, int pendingCount)
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
                        _growingSeedName.text = seed.seedName;
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
                        _readySeedName.text = seed.harvestName;
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