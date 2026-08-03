using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.Alchemy
{
    /// <summary>
    /// 재료 인벤토리 그리드 슬롯 하나.
    /// 재료 아이콘 + 이름 + 개수 표시.
    /// </summary>
    public class MaterialSlot : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private Button _button;

        private MaterialItemData _materialData;
        private int _count;
        private Action<MaterialItemData> _onClickCallback;

        public MaterialItemData MaterialData => _materialData;
        public int Count => _count;

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(OnSlotClicked);
        }

        public void Setup(MaterialItemData material, int count, Action<MaterialItemData> onClick)
        {
            _materialData = material;
            _count = count;
            _onClickCallback = onClick;

            if (material == null) return;

            if (_iconImage != null)
            {
                if (material.icon != null)
                {
                    _iconImage.sprite = material.icon;
                    _iconImage.enabled = true;
                }
                else
                {
                    _iconImage.enabled = false;
                }
            }

            if (_nameText != null)
                _nameText.text = material.itemName;

            if (_countText != null)
                _countText.text = count.ToString();
        }

        private void OnSlotClicked()
        {
            _onClickCallback?.Invoke(_materialData);
        }
    }
}