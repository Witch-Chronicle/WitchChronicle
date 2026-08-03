using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WitchChronicle.Alchemy
{
    /// <summary>
    /// 요리/포션 제작 성공 팝업.
    /// </summary>
    public class AlchemySuccessPopup : MonoBehaviour
    {
        [Header("UI 루트")]
        [SerializeField] private GameObject _popupRoot;

        [Header("표시 요소")]
        [SerializeField] private TextMeshProUGUI _headerText;
        [SerializeField] private Image _resultImage;
        [SerializeField] private Button _confirmButton;

        [Header("모드별 헤더 텍스트")]
        [SerializeField] private string _cookingSuccessText = "요리 성공!";
        [SerializeField] private string _potionSuccessText = "포션 제조 성공!";

        private Action _onClosedCallback;

        private void Awake()
        {
            if (_popupRoot != null) _popupRoot.SetActive(false);

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        public void Show(Sprite resultSprite, AlchemyMode mode, Action onClosed)
        {
            _onClosedCallback = onClosed;

            if (_popupRoot != null) _popupRoot.SetActive(true);

            if (_headerText != null)
                _headerText.text = (mode == AlchemyMode.Cooking) 
                    ? _cookingSuccessText 
                    : _potionSuccessText;

            if (_resultImage != null && resultSprite != null)
            {
                _resultImage.sprite = resultSprite;
                _resultImage.preserveAspect = true;
                _resultImage.enabled = true;
            }

            Debug.Log($"[AlchemySuccessPopup] 표시 (모드: {mode})");
        }

        public void Close()
        {
            if (_popupRoot != null) _popupRoot.SetActive(false);

            _onClosedCallback?.Invoke();
            _onClosedCallback = null;
        }

        private void OnConfirmClicked()
        {
            Close();
        }
    }
}