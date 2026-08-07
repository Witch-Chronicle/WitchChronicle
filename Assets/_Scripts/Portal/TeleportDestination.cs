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
/// </summary>
public class TeleportDestination : MonoBehaviour
{
    [Header("표시 이름")]
    [Tooltip("빠른 이동 목록에 표시될 이름. 비우면 오브젝트 이름을 쓴다.")]
    [SerializeField] private string _displayName;

    private static readonly List<TeleportDestination> _all = new List<TeleportDestination>();

    /// <summary>
    /// 현재 씬에 있는 모든 목적지 (등록 순서).
    /// </summary>
    public static IReadOnlyList<TeleportDestination> All => _all;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

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
    /// 씬 뷰에서 도착 지점과 방향을 눈으로 확인하기 위한 기즈모.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}
