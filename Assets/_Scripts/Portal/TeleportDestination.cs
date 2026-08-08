using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 빠른 이동 목적지 표식.
///
/// 도착시키고 싶은 위치에 빈 오브젝트를 두고 이 컴포넌트를 붙인다.
/// 오브젝트의 위치와 회전이 그대로 도착 지점이 되므로,
/// 캐릭터가 바라볼 방향까지 고려해서 회전을 맞춰두면 좋다.
///
/// 활성화될 때 스스로 목록에 등록되므로 별도 등록 작업이 필요 없다.
/// TeleportPanel의 고정 버튼과는 Id로 매칭된다.
/// </summary>
public class TeleportDestination : MonoBehaviour
{
    [Header("식별자")]
    [Tooltip("TeleportPanel의 고정 버튼과 매칭할 식별자.")]
    [SerializeField] private TeleportPointId _id;

    private static readonly List<TeleportDestination> _all = new List<TeleportDestination>();

    /// <summary>
    /// 현재 씬에 있는 모든 목적지 (등록 순서).
    /// </summary>
    public static IReadOnlyList<TeleportDestination> All => _all;

    public TeleportPointId Id => _id;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    private void OnEnable()
    {
        if (_all.Contains(this) == false)
        {
            _all.Add(this);
        }
    }

    private void OnDisable()
    {
        _all.Remove(this);
    }

    /// <summary>
    /// Id로 등록된 목적지를 찾는다. 없으면 null.
    /// </summary>
    public static TeleportDestination FindById(TeleportPointId id)
    {
        for (int i = 0; i < _all.Count; i++)
        {
            if (_all[i] != null && _all[i].Id == id)
            {
                return _all[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 씬 뷰에서 도착 지점과 방향을 눈으로 확인하기 위한 기즈모.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}