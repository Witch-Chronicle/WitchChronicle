using UnityEngine;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _escapeBtn;
    [SerializeField] private Button _closeBtn;

    [Header("Return Scene")]
    [SerializeField] private SceneId _returnScene = SceneId.Main;

    private void OnEnable()
    {
        if (_escapeBtn != null)
        {
            _escapeBtn.onClick.AddListener(HandleEscapeClicked);
        }

        if (_closeBtn != null)
        {
            _closeBtn.onClick.AddListener(HandleCloseClicked);
        }
    }

    private void OnDisable()
    {
        if (_escapeBtn != null)
        {
            _escapeBtn.onClick.RemoveListener(HandleEscapeClicked);
        }

        if (_closeBtn != null)
        {
            _closeBtn.onClick.RemoveListener(HandleCloseClicked);
        }
    }

    private void HandleEscapeClicked()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[PauseController] SceneTransitionManager.Instance가 없습니다.");
            return;
        }

        if (PlayerUIInputReader.Instance != null)
        {
            PlayerUIInputReader.Instance.TogglePausePanel();
        }

        SceneTransitionManager.Instance.LoadScene(_returnScene);
    }

    private void HandleCloseClicked()
    {
        if (PlayerUIInputReader.Instance != null)
        {
            PlayerUIInputReader.Instance.TogglePausePanel();
        }
    }

}