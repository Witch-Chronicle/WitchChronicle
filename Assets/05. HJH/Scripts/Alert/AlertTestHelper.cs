using UnityEngine;

/// <summary>
/// Alert UI 테스트용 스크립트입니다.
/// </summary>
public class AlertTestHelper : MonoBehaviour
{
    private int _itemCount;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            _itemCount++;

            AlertManager.Instance?.Enqueue(
                AlertType.ItemAcquired,
                $"테스트 포션 {_itemCount}",
                _itemCount
            );
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            AlertManager.Instance?.Enqueue(
                AlertType.GoldAcquired,
                Random.Range(100, 1001)
            );
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            AlertManager.Instance?.Enqueue(
                AlertType.InventoryFull
            );
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            AlertManager.Instance?.EnqueueRaw(
                "직접 입력한 테스트 Alert입니다.",
                3f
            );
        }
    }
}