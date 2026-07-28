using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.IdleFarming
{
    public class SeedCardUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _ownedCountText;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _selectedFrame;

        private SeedData _seed;
        private Action<SeedData> _onClick;

        private void Awake()
        {
            _button.onClick.AddListener(HandleClick);
        }

        public void Setup(SeedData seed, int ownedCount, Action<SeedData> onClick)
        {
            _seed = seed;
            _onClick = onClick;

            if (_iconImage != null && seed.seedSprite != null)
                _iconImage.sprite = seed.seedSprite;
            if (_nameText != null)
                _nameText.text = seed.seedName;
            if (_ownedCountText != null)
                _ownedCountText.text = $"x{ownedCount}";

            _button.interactable = ownedCount > 0;
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectedFrame != null) _selectedFrame.SetActive(selected);
        }

        private void HandleClick()
        {
            _onClick?.Invoke(_seed);
        }
    }
}