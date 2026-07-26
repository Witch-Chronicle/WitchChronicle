using UnityEngine;

public class ShopKeeper : MonoBehaviour, ITFInteractable
{
    public string Prompt => "[F] 상점 열기";

    public void Interact(GameObject interactor)
    {
        Debug.Log("상점 NPC 상호작용: 상점 UI 오픈.");
        ShopNPC.Instance.ToggleShop();
    }

}
