using UnityEngine;

public class FarmInteractable : MonoBehaviour, ITFInteractable
{
    public GameObject farmUIPanel;

    public string Prompt => "[F] 농사 짓기";

    void Start()
    {
        if (farmUIPanel != null) farmUIPanel.SetActive(false);
    }

    void ToggleFarmUI()
    {
        if (farmUIPanel == null) return;
        bool isActive = farmUIPanel.activeSelf;
        farmUIPanel.SetActive(!isActive);

        if (isActive)
        {
            CursorLocker.Instance.ExitUIMode();
        }
        else
        {
            CursorLocker.Instance.EnterUIMode();
        }
    }

    public void Interact(GameObject interactor)
    {
        ToggleFarmUI();
    }
}