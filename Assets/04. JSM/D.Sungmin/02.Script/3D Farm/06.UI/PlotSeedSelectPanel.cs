using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.IdleFarming
{
    public class PlotSeedSelectPanel : MonoBehaviour
    {
        public enum Category { Crop, Herb }

        [Header("UI - 루트")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _closeButton;

        [Header("UI - 카테고리 탭")]
        [SerializeField] private Button _cropTabButton;
        [SerializeField] private Button _herbTabButton;

        [Header("UI - 카드 그리드")]
        [SerializeField] private Transform _cardParent;
        [SerializeField] private SeedCardUI _cardPrefab;

        [Header("UI - 상세 정보")]
        [SerializeField] private GameObject _detailRoot;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailCycleInfo;
        [SerializeField] private Button _plantButton;

        private PlotSlot _targetSlot;
        private Category _currentCategory = Category.Crop;
        private SeedData _selectedSeed;
        private readonly List<SeedCardUI> _spawnedCards = new List<SeedCardUI>();
        private bool _isOpen;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _cropTabButton.onClick.AddListener(() => SwitchCategory(Category.Crop));
            _herbTabButton.onClick.AddListener(() => SwitchCategory(Category.Herb));
            _plantButton.onClick.AddListener(OnPlantClicked);
            if (_root != null) _root.SetActive(false);
        }

        public void Open(PlotSlot slot)
        {
            _targetSlot = slot;
            _selectedSeed = null;
            _root.SetActive(true);
            SwitchCategory(Category.Crop);
            UpdateDetail();

            if (!_isOpen)
            {
                _isOpen = true;
                if (PlotManager.Instance != null) PlotManager.Instance.NotifyPanelOpened();
            }
        }

        public void Close()
        {
            _root.SetActive(false);
            _targetSlot = null;
            _selectedSeed = null;

            if (_isOpen)
            {
                _isOpen = false;
                if (PlotManager.Instance != null) PlotManager.Instance.NotifyPanelClosed();
            }
        }

        private void SwitchCategory(Category cat)
        {
            _currentCategory = cat;
            _selectedSeed = null;
            RebuildCards();
            UpdateDetail();
        }

        private void RebuildCards()
        {
            // 기존 카드 제거
            foreach (var c in _spawnedCards)
                if (c != null) Destroy(c.gameObject);
            _spawnedCards.Clear();

            // 카테고리로 필터링해서 생성
            for (int i = 0; i < PlotManager.Instance.AllSeedsCount; i++)
            {
                var seed = PlotManager.Instance.GetSeedAt(i);
                if (seed == null) continue;
                if (!MatchesCategory(seed, _currentCategory)) continue;

                int owned = GetOwnedCount(seed);
                var card = Instantiate(_cardPrefab, _cardParent);
                card.Setup(seed, owned, OnCardClicked);
                _spawnedCards.Add(card);
            }
        }

        private bool MatchesCategory(SeedData seed, Category cat)
        {
            switch (cat)
            {
                case Category.Crop: return seed.category == SeedCategory.Jagmul;
                case Category.Herb: return seed.category == SeedCategory.Yakcho;
                default: return false;
            }
        }

        private int GetOwnedCount(SeedData seed)
        {
            if (seed == null || seed.seedItem == null)
                return 0;

            return PlayerInventory.Instance.GetTotalQuantity(seed.seedItem);
        }

        private void OnCardClicked(SeedData seed)
        {
            _selectedSeed = seed;
            foreach (var c in _spawnedCards)
                c.SetSelected(false);
            foreach (var c in _spawnedCards)
            {
                // 선택된 카드 표시 (Setup에서 seed 저장해두면 더 깔끔하지만 지금은 단순화)
            }
            UpdateDetail();
        }

        private void UpdateDetail()
        {
            if (_selectedSeed == null)
            {
                _detailRoot.SetActive(false);
                _plantButton.interactable = false;
                return;
            }

            _detailRoot.SetActive(true);
            if (_detailIcon != null && _selectedSeed.seedSprite != null)
                _detailIcon.sprite = _selectedSeed.seedSprite;
            if (_detailName != null)
                _detailName.text = _selectedSeed.seedName;
            if (_detailDescription != null)
                _detailDescription.text = _selectedSeed.description;

            int minutes = Mathf.RoundToInt(_selectedSeed.growthTime / 60f);
            if (_detailCycleInfo != null)
                _detailCycleInfo.text = $"{minutes}분마다 {_selectedSeed.harvestAmount}개 최대 5개";

            _plantButton.interactable = GetOwnedCount(_selectedSeed) > 0;
        }

        private void OnPlantClicked()
        {
            if (_selectedSeed == null || _targetSlot == null)
                return;

            if (!PlayerInventory.Instance.TryConsumeItem(_selectedSeed.seedItem, 1))
            {
                Debug.Log("씨앗이 부족합니다.");
                return;
            }

            if (_targetSlot.PlantSeed(_selectedSeed))
            {
                PlayerInventory.Instance.RaiseInventoryChanged();
                Close();
            }
            else
            {
                // 심기 실패 시 씨앗 반환
                PlayerInventory.Instance.AddItem(_selectedSeed.seedItem, 1);
                PlayerInventory.Instance.RaiseInventoryChanged();
            }
        }
    }
}