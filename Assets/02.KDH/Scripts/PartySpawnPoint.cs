using UnityEngine;

/// 파티 스폰 지점 — 씬마다 하나 배치.
/// 씬이 로드되면 Party가 이 위치·방향으로 파티를 이동시킨다.
/// 씬 뷰에서 하늘색 구 + 화살표(바라볼 방향)로 표시됨.
public class PartySpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}
