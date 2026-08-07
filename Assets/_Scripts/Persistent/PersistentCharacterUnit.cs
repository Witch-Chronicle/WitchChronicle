using UnityEngine;

/// <summary>
/// 씬 전환 유지 캐릭터 데이터 단위
/// </summary>
public class PersistentCharacterUnit : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool _isRecruited;

    [Header("References")]
    [SerializeField] private CharacterStats _characterStats;
    [SerializeField] private CharacterVitals _characterVitals;
    [SerializeField] private CharacterEquipment _characterEquipment;
    [SerializeField] private StatController _statController;
    [SerializeField] private PlayerSkillLoadout _playerSkillLoadout;

    [Header("Scene Prefabs")]
    [SerializeField] private BattleActor _battleActorPrefab;
    [SerializeField] private GameObject _fieldActorPrefab;

    public bool IsRecruited => _isRecruited;

    public CharacterStats CharacterStats => _characterStats;
    public CharacterVitals CharacterVitals => _characterVitals;
    public CharacterEquipment CharacterEquipment => _characterEquipment;
    public StatController StatController => _statController;
    public PlayerSkillLoadout PlayerSkillLoadout => _playerSkillLoadout;

    public BattleActor BattleActorPrefab => _battleActorPrefab;
    public GameObject FieldActorPrefab => _fieldActorPrefab;

    public string CharacterId => _characterStats != null ? _characterStats.CharacterId : string.Empty;
    public string CharacterName => _characterStats != null ? _characterStats.CharacterName : name;

    /// <summary>
    /// 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        ResolveReferences();
    }

    /// <summary>
    /// 같은 오브젝트의 캐릭터 컴포넌트 연결
    /// </summary>
    public void ResolveReferences()
    {
        if (_characterStats == null)
        {
            _characterStats = GetComponent<CharacterStats>();
        }

        if (_characterVitals == null)
        {
            _characterVitals = GetComponent<CharacterVitals>();
        }

        if (_characterEquipment == null)
        {
            _characterEquipment = GetComponent<CharacterEquipment>();
        }

        if (_statController == null)
        {
            _statController = GetComponent<StatController>();
        }

        if (_playerSkillLoadout == null)
        {
            _playerSkillLoadout = GetComponent<PlayerSkillLoadout>();
        }
    }

    /// <summary>
    /// 영입 상태 설정
    /// </summary>
    /// <param name="isRecruited">영입 여부</param>
    public void SetRecruited(bool isRecruited)
    {
        _isRecruited = isRecruited;
    }

    /// <summary>
    /// 현재 HP/MP 반영
    /// </summary>
    /// <param name="currentHp">현재 HP</param>
    /// <param name="currentMp">현재 MP</param>
    public void ApplyVitals(int currentHp, int currentMp)
    {
        if (_characterVitals == null)
        {
            return;
        }

        _characterVitals.SetCurrentVitals(currentHp, currentMp);
    }

    /// <summary>
    /// 완전 회복
    /// </summary>
    [ContextMenu("Debug Restore Fully")]
    public void RestoreFully()
    {
        if (_characterVitals == null)
        {
            return;
        }

        _characterVitals.RestoreFully();
    }

    /// <summary>
    /// 전투 프리팹 존재 여부 반환
    /// </summary>
    /// <returns>전투 프리팹 존재 여부</returns>
    public bool HasBattleActorPrefab()
    {
        return _battleActorPrefab != null;
    }

    /// <summary>
    /// 필드 프리팹 존재 여부 반환
    /// </summary>
    /// <returns>필드 프리팹 존재 여부</returns>
    public bool HasFieldActorPrefab()
    {
        return _fieldActorPrefab != null;
    }
}