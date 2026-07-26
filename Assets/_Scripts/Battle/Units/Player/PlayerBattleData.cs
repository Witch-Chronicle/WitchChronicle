using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 캐릭터의 전투 프로필 데이터를 관리하는 ScriptableObject입니다.
/// 실제 전투 스탯은 CharacterStats에서 계산하고, 이 데이터는 캐릭터 기본 정보와 전투 스킬 목록을 제공합니다.
/// </summary>
[CreateAssetMenu(menuName = "Witch Chronicle/PlayerBattleData")]
public class PlayerBattleData : ScriptableObject
{
    [Header("Player Info")]
    [SerializeField] private CharacterBaseStats _characterBaseStats;

    [Header("Player Skill List")]
    [SerializeField] private SkillData[] _skillList;

    public CharacterBaseStats CharacterBaseStats => _characterBaseStats;

    public string PlayerId => _characterBaseStats != null ? _characterBaseStats.CharacterId : string.Empty;
    public string PlayerName => _characterBaseStats != null ? _characterBaseStats.CharacterName : string.Empty;

    public IReadOnlyList<SkillData> SkillList => _skillList;
}