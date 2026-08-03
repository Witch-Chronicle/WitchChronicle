using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.Alchemy
{
    /// <summary>
    /// 알케미 재료 슬롯.
    /// 인벤토리 클릭으로 담기, 슬롯 클릭으로 비우기.
    /// </summary>
    public class AlchemyIngredientSlot : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private GameObject _emptyIndicator;
        [SerializeField] private Button _clearButton;

        private MaterialItemData _currentMaterial;
        private int _currentCount;

        public MaterialItemData CurrentMaterial => _currentMaterial;
        public int CurrentCount => _currentCount;
        public bool IsEmpty => _currentMaterial == null;

        public event Action OnSlotChanged;

        private void Awake()
        {
            if (_clearButton != null)
                _clearButton.onClick.AddListener(ClearSlot);

            Refresh();
        }

        public void SetMaterial(MaterialItemData material, int count)
        {
            _currentMaterial = material;
            _currentCount = count;
            Refresh();
            OnSlotChanged?.Invoke();
        }

        public void ClearSlot()
        {
            _currentMaterial = null;
            _currentCount = 0;
            Refresh();
            OnSlotChanged?.Invoke();
        }

        private void Refresh()
        {
            bool hasItem = !IsEmpty;

            if (_iconImage != null)
            {
                _iconImage.enabled = hasItem;
                _iconImage.preserveAspect = true;
                if (hasItem && _currentMaterial.icon != null)
                    _iconImage.sprite = _currentMaterial.icon;
            }

            if (_countText != null)
            {
                _countText.gameObject.SetActive(hasItem);
                _countText.text = $"x{_currentCount}";
            }

            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(hasItem);
                if (hasItem) _nameText.text = _currentMaterial.itemName;
            }

            if (_emptyIndicator != null)
                _emptyIndicator.SetActive(!hasItem);
        }
    }
}