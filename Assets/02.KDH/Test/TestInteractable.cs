using UnityEngine;

public class TestInteractable : MonoBehaviour, ITFInteractable
{
    public string Prompt => "Test";

    public void Interact(GameObject interactor)
    {
        Debug.Log($"{name} Interact");
    }
}
