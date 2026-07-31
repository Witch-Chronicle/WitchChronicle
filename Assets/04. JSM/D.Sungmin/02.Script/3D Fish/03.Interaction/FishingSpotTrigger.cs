using UnityEngine;

/// <summary>
/// 자식 오브젝트의 트리거 이벤트를 부모의 FishingSpot으로 전달.
/// Collider와 스크립트가 다른 GameObject에 있을 때 필요.
/// </summary>
public class FishingSpotTrigger : MonoBehaviour
{
    private FishingSpot spot;

    private void Awake()
    {
        spot = GetComponentInParent<FishingSpot>();
        if (spot == null)
            Debug.LogError("[FishingSpotTrigger] 부모에 FishingSpot 컴포넌트 없음!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (spot != null) spot.HandlePlayerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (spot != null) spot.HandlePlayerExit(other);
    }
}