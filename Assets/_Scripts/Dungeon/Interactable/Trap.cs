using UnityEngine;

public class Trap : MonoBehaviour, ITFInteractable
{
    public string Prompt => "[F] 조사하기";

    public void Interact(GameObject interactor)
    {
        TrapActivated();
    }

    private void TrapActivated()
    {
        // player 인벤토리 무작위 삭제(포션) 
        // player 피 깍기
        // 디버프 효과,

        ShowMessageManager.Instance.ShowMessage("함정 발동 됨");
    }
}
