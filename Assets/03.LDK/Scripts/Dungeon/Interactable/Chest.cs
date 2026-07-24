using UnityEngine;

public class Chest : MonoBehaviour, ITFInteractable
{
    [SerializeField] private ChestRewardData _rewardData;
    private bool _isOpened;
    public string Prompt => _isOpened?  "이미 열었다..." : "[F] 보물상자 열기";
    public void Interact(GameObject interactor)
    {
        if (_isOpened)
        {
            return;
        }

        _isOpened = true;

        GiveReward();
    }

    /// <summary>
    /// 랜덤 보상 지급
    /// </summary>
    private void GiveReward()
    {
        ChestReward reward = GetRandomReward();

        if (reward == null)
        {
            Debug.Log("상자 보상 없음");

            return;
        }

        PlayerInventory.Instance.AddItem(reward.item, 1);

        Debug.Log($"상자 보상 : {reward.item.itemName}");

        ShowMessageManager.Instance.ShowMessage($"상자 보상 : {reward.item.itemName}");
    }



    /// <summary>
    /// 확률 기반 랜덤 선택
    /// </summary>
    private ChestReward GetRandomReward()
    {
        float randomValue = Random.Range(0f, 100f);

        float current = 0f;

        foreach (ChestReward reward in _rewardData.rewards)
        {
            current += reward.chance;

            if (randomValue <= current)
            {
                return reward;
            }
        }

        return null;
    }
}