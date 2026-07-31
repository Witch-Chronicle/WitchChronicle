using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.Alchemy
{
    /// <summary>
    /// 가마솥 UI 패널 (요리/포션 겸용).
    /// 모드 탭 클릭 시 3D 가마솥 + UI 요소가 함께 스왑됨.
    /// </summary>
    public class AlchemyPanel : MonoBehaviour
    {
        [Header("UI 루트")]
        [SerializeField] private GameObject _panelRoot;

        [Header("모드 전환 - 3D 가마솥")]
        [SerializeField] private GameObject _cookingCauldron;
        [SerializeField] private GameObject _potionCauldron;

        [Header("모드 탭 버튼")]
        [SerializeField] private Button _cookingTabButton;
        [SerializeField] private Button _potionTabButton;

        [Header("모드 탭 시각 상태")]
        [SerializeField] private Color _tabActiveColor = new Color(1f, 0.6f, 0.3f);
        [SerializeField] private Color _tabInactiveColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("등급 탭")]
        [SerializeField] private Button _commonTabButton;
        [SerializeField] private Button _rareTabButton;
        [SerializeField] private Button _legendaryTabButton;

        [Header("영역 참조")]
        [SerializeField] private RectTransform _ingredientSlotContainer;
        [SerializeField] private TextMeshProUGUI _startButtonText;
        [SerializeField] private Button _startButton;

        [Header("재료 슬롯")]
        [SerializeField] private GameObject[] _ingredientSlots; // 최대 6개 미리 배치

        private Action _onClosedCallback;
        private AlchemyMode _currentMode;

        private void Awake()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_cookingTabButton != null)
                _cookingTabButton.onClick.AddListener(() => SwitchMode(AlchemyMode.Cooking));
            if (_potionTabButton != null)
                _potionTabButton.onClick.AddListener(() => SwitchMode(AlchemyMode.Potion));

            if (_commonTabButton != null)
                _commonTabButton.onClick.AddListener(() => OnGradeTabClicked(0));
            if (_rareTabButton != null)
                _rareTabButton.onClick.AddListener(() => OnGradeTabClicked(1));
            if (_legendaryTabButton != null)
                _legendaryTabButton.onClick.AddListener(() => OnGradeTabClicked(2));
        }

        private void Update()
        {
            if (_panelRoot == null || !_panelRoot.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Open(AlchemyMode mode, Action onClosed)
        {
            _onClosedCallback = onClosed;
            if (_panelRoot != null) _panelRoot.SetActive(true);

            SwitchMode(mode);
            Debug.Log($"[AlchemyPanel] 열림 (모드: {mode})");
        }

        public void Close()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_cookingCauldron != null) _cookingCauldron.SetActive(false);
            if (_potionCauldron != null) _potionCauldron.SetActive(false);

            _onClosedCallback?.Invoke();
            _onClosedCallback = null;
        }

        private void SwitchMode(AlchemyMode mode)
        {
            _currentMode = mode;

            // 3D 가마솥 스왑
            if (_cookingCauldron != null)
                _cookingCauldron.SetActive(mode == AlchemyMode.Cooking);
            if (_potionCauldron != null)
                _potionCauldron.SetActive(mode == AlchemyMode.Potion);

            // 모드 탭 색상 업데이트
            UpdateModeTabVisual();

            // 재료 슬롯 개수 스왑 (요리 5, 포션 6)
            UpdateIngredientSlotCount(mode);

            // 시작 버튼 텍스트 스왑
            UpdateStartButtonText(mode);

            // 등급 탭 스왑 (포션은 Legendary 없음)
            UpdateGradeTabVisibility(mode);

            // TODO: 레시피 리스트 리로드 (Phase 2)
            // TODO: 재료 인벤토리 필터 변경 (Phase 3)

            Debug.Log($"[AlchemyPanel] 모드 전환: {mode}");
        }

        private void UpdateModeTabVisual()
        {
            SetButtonColor(_cookingTabButton, _currentMode == AlchemyMode.Cooking ? _tabActiveColor : _tabInactiveColor);
            SetButtonColor(_potionTabButton, _currentMode == AlchemyMode.Potion ? _tabActiveColor : _tabInactiveColor);
        }

        private void SetButtonColor(Button btn, Color color)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        private void UpdateIngredientSlotCount(AlchemyMode mode)
        {
            if (_ingredientSlots == null) return;

            int visibleCount = (mode == AlchemyMode.Cooking) ? 5 : 6;

            for (int i = 0; i < _ingredientSlots.Length; i++)
            {
                if (_ingredientSlots[i] != null)
                    _ingredientSlots[i].SetActive(i < visibleCount);
            }
        }

        private void UpdateStartButtonText(AlchemyMode mode)
        {
            if (_startButtonText == null) return;
            _startButtonText.text = (mode == AlchemyMode.Cooking) ? " 요리 시작" : " 제조 시작";
        }

        private void UpdateGradeTabVisibility(AlchemyMode mode)
        {
            // 포션은 Legendary 없으니 숨김
            if (_legendaryTabButton != null)
                _legendaryTabButton.gameObject.SetActive(mode == AlchemyMode.Cooking);
        }

        private void OnGradeTabClicked(int gradeIndex)
        {
            // TODO: 레시피 리스트 필터링 (Phase 2)
            Debug.Log($"[AlchemyPanel] 등급 탭 클릭: {gradeIndex}");
        }
    }
}