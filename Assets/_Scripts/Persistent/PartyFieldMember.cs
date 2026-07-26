using UnityEngine;

/// <summary>
/// 필드 파티 멤버와 유지 캐릭터 데이터 연결 관리
/// </summary>
public class PartyFieldMember : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string _characterId;

    [Header("Persistent Source")]
    [SerializeField] private PersistentCharacterUnit _persistentCharacterUnit;

    [Header("References")]
    [SerializeField] private StatController _statController;

    public string CharacterId => _characterId;
    public PersistentCharacterUnit PersistentCharacterUnit => _persistentCharacterUnit;
    public StatController StatController => _statController;

    /// <summary>
    /// 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        ResolveReferences();
    }

    /// <summary>
    /// 같은 오브젝트의 필드 멤버 참조 연결
    /// </summary>
    public void ResolveReferences()
    {
        if (_statController == null || _statController.gameObject != gameObject)
        {
            _statController = GetComponent<StatController>();
        }
    }

    /// <summary>
    /// 유지 캐릭터 데이터 연결
    /// </summary>
    /// <param name="persistentCharacterUnit">연결할 유지 캐릭터 데이터</param>
    public void Bind(PersistentCharacterUnit persistentCharacterUnit)
    {
        _persistentCharacterUnit = persistentCharacterUnit;

        if (_persistentCharacterUnit != null)
        {
            _characterId = _persistentCharacterUnit.CharacterId;
        }

        ResolveReferences();
    }

    /// <summary>
    /// 연결 캐릭터 존재 여부 반환
    /// </summary>
    /// <returns>연결 캐릭터 존재 여부</returns>
    public bool HasPersistentCharacter()
    {
        return _persistentCharacterUnit != null;
    }
}