using UnityEngine;

// 캐릭터마다 정보는 SO로 관리 (주인공 및 동료)
[CreateAssetMenu(menuName = "Witch Chronicle/Character Base Stats")]
public class CharacterBaseStats : ScriptableObject
{
    [SerializeField] private string _characterId;
    [SerializeField] private string _characterName;
    [SerializeField] private int _startLevel = 1;

    [Header("Base Stats")]
    [SerializeField] private StatBlock _baseStats;

    // 레벨 오르면 받는 스탯 포인트
    [Header("Growth")]
    [SerializeField] private int _statPointPerLevel = 3;

    public string CharacterId => _characterId;
    public string CharacterName => _characterName;
    public int StartLevel => _startLevel;

    public StatBlock BaseStats => _baseStats;

    public int StatPointPerLevel => _statPointPerLevel;
}