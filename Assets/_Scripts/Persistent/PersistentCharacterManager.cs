using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 전환 유지 캐릭터 목록, 영입 상태, 현재 파티 관리
/// </summary>
public class PersistentCharacterManager : MonoBehaviour
{
    public static PersistentCharacterManager Instance { get; private set; }

    [Header("Characters")]
    [SerializeField]
    private List<PersistentCharacterUnit> _allCharacters =
        new List<PersistentCharacterUnit>();

    [Header("Active Party")]
    [SerializeField]
    private List<string> _activePartyCharacterIds =
        new List<string>();

    [SerializeField] private int _maxActivePartyCount = 4;

    private readonly Dictionary<string, PersistentCharacterUnit> _characterById =
        new Dictionary<string, PersistentCharacterUnit>();

    public IReadOnlyList<PersistentCharacterUnit> AllCharacters => _allCharacters;
    public IReadOnlyList<string> ActivePartyCharacterIds => _activePartyCharacterIds;
    public int MaxActivePartyCount => _maxActivePartyCount;

    /// <summary>
    /// 싱글톤 등록 및 목록 초기화
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }

        Instance = this;

        RefreshCharacters();
        ValidateActiveParty();
    }

    /// <summary>
    /// 싱글톤 해제
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 전체 캐릭터 목록 갱신
    /// </summary>
    public void RefreshCharacters()
    {
        if (_allCharacters.Count == 0)
        {
            GetComponentsInChildren(true, _allCharacters);
        }

        _characterById.Clear();

        for (int i = 0; i < _allCharacters.Count; i++)
        {
            PersistentCharacterUnit character = _allCharacters[i];

            if (character == null)
            {
                continue;
            }

            character.ResolveReferences();

            string characterId = character.CharacterId;

            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogWarning($"{character.name}의 CharacterId 비어 있음");
                continue;
            }

            if (_characterById.ContainsKey(characterId))
            {
                Debug.LogWarning($"중복 CharacterId 감지: {characterId}");
                continue;
            }

            _characterById.Add(characterId, character);
        }
    }

    /// <summary>
    /// 전체 캐릭터 목록 복사
    /// </summary>
    /// <param name="result">복사 대상 목록</param>
    public void GetAllCharacters(List<PersistentCharacterUnit> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        for (int i = 0; i < _allCharacters.Count; i++)
        {
            PersistentCharacterUnit character = _allCharacters[i];

            if (character == null)
            {
                continue;
            }

            result.Add(character);
        }
    }

    /// <summary>
    /// 영입된 캐릭터 목록 복사
    /// </summary>
    /// <param name="result">복사 대상 목록</param>
    public void GetRecruitedCharacters(List<PersistentCharacterUnit> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        for (int i = 0; i < _allCharacters.Count; i++)
        {
            PersistentCharacterUnit character = _allCharacters[i];

            if (character == null || character.IsRecruited == false)
            {
                continue;
            }

            result.Add(character);
        }
    }

    /// <summary>
    /// 현재 파티 캐릭터 목록 복사
    /// </summary>
    /// <param name="result">복사 대상 목록</param>
    public void GetActivePartyMembers(List<PersistentCharacterUnit> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        for (int i = 0; i < _activePartyCharacterIds.Count; i++)
        {
            string characterId = _activePartyCharacterIds[i];

            if (TryGetCharacter(characterId, out PersistentCharacterUnit character) == false)
            {
                continue;
            }

            if (character.IsRecruited == false)
            {
                continue;
            }

            result.Add(character);
        }
    }

    /// <summary>
    /// CharacterId 기준 캐릭터 검색
    /// </summary>
    /// <param name="characterId">캐릭터 ID</param>
    /// <param name="character">검색 캐릭터</param>
    /// <returns>검색 성공 여부</returns>
    public bool TryGetCharacter(string characterId, out PersistentCharacterUnit character)
    {
        character = null;

        if (string.IsNullOrEmpty(characterId))
        {
            return false;
        }

        if (_characterById.TryGetValue(characterId, out character))
        {
            return true;
        }

        RefreshCharacters();

        return _characterById.TryGetValue(characterId, out character);
    }

    /// <summary>
    /// 캐릭터 영입
    /// </summary>
    /// <param name="characterId">캐릭터 ID</param>
    /// <param name="addToActiveParty">현재 파티 자동 추가 여부</param>
    /// <param name="restoreFully">영입 시 완전 회복 여부</param>
    /// <returns>영입 성공 여부</returns>
    public bool RecruitCharacter(
        string characterId,
        bool addToActiveParty = true,
        bool restoreFully = true)
    {
        if (TryGetCharacter(characterId, out PersistentCharacterUnit character) == false)
        {
            Debug.LogWarning($"영입 실패. 캐릭터 없음: {characterId}");
            return false;
        }

        character.SetRecruited(true);

        if (restoreFully)
        {
            character.RestoreFully();
        }

        if (addToActiveParty)
        {
            TryAddToActiveParty(characterId);
        }

        SaveManager.RequestSave();

        return true;
    }

    /// <summary>
    /// 캐릭터 영입 해제
    /// </summary>
    /// <param name="characterId">캐릭터 ID</param>
    /// <returns>해제 성공 여부</returns>
    public bool UnrecruitCharacter(string characterId)
    {
        if (TryGetCharacter(characterId, out PersistentCharacterUnit character) == false)
        {
            return false;
        }

        character.SetRecruited(false);
        RemoveFromActiveParty(characterId);

        return true;
    }

    /// <summary>
    /// 현재 파티에 캐릭터 추가
    /// </summary>
    /// <param name="characterId">캐릭터 ID</param>
    /// <returns>추가 성공 여부</returns>
    public bool TryAddToActiveParty(string characterId)
    {
        if (_activePartyCharacterIds.Contains(characterId))
        {
            return true;
        }

        if (_activePartyCharacterIds.Count >= _maxActivePartyCount)
        {
            return false;
        }

        if (TryGetCharacter(characterId, out PersistentCharacterUnit character) == false)
        {
            return false;
        }

        if (character.IsRecruited == false)
        {
            return false;
        }

        _activePartyCharacterIds.Add(characterId);
        return true;
    }

    /// <summary>
    /// 현재 파티에서 캐릭터 제거
    /// </summary>
    /// <param name="characterId">캐릭터 ID</param>
    public void RemoveFromActiveParty(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            return;
        }

        _activePartyCharacterIds.Remove(characterId);
    }

    /// <summary>
    /// 현재 파티 순서 설정
    /// </summary>
    /// <param name="characterIds">캐릭터 ID 목록</param>
    public void SetActivePartyOrder(IReadOnlyList<string> characterIds)
    {
        _activePartyCharacterIds.Clear();

        if (characterIds == null)
        {
            return;
        }

        for (int i = 0; i < characterIds.Count; i++)
        {
            if (_activePartyCharacterIds.Count >= _maxActivePartyCount)
            {
                break;
            }

            TryAddToActiveParty(characterIds[i]);
        }
    }

    /// <summary>
    /// 현재 파티 유효성 보정
    /// </summary>
    private void ValidateActiveParty()
    {
        for (int i = _activePartyCharacterIds.Count - 1; i >= 0; i--)
        {
            string characterId = _activePartyCharacterIds[i];

            if (TryGetCharacter(characterId, out PersistentCharacterUnit character) == false)
            {
                _activePartyCharacterIds.RemoveAt(i);
                continue;
            }

            if (character.IsRecruited == false)
            {
                _activePartyCharacterIds.RemoveAt(i);
                continue;
            }

            if (IsDuplicateActivePartyId(characterId, i))
            {
                _activePartyCharacterIds.RemoveAt(i);
            }
        }

        while (_activePartyCharacterIds.Count > _maxActivePartyCount)
        {
            _activePartyCharacterIds.RemoveAt(_activePartyCharacterIds.Count - 1);
        }
    }

    /// <summary>
    /// 현재 파티 중복 ID 여부 확인
    /// </summary>
    /// <param name="characterId">캐릭터 ID</param>
    /// <param name="currentIndex">현재 인덱스</param>
    /// <returns>중복 여부</returns>
    private bool IsDuplicateActivePartyId(string characterId, int currentIndex)
    {
        for (int i = 0; i < _activePartyCharacterIds.Count; i++)
        {
            if (i == currentIndex)
            {
                continue;
            }

            if (_activePartyCharacterIds[i] == characterId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 현재 파티 전원에게 경험치 지급
    /// </summary>
    /// <param name="amount">지급 경험치</param>
    public void AddExpToActiveParty(int amount)
    {
        Debug.Log("AddExpToActiveParty 호출됨");
        if (amount <= 0)
        {
            return;
        }

        List<PersistentCharacterUnit> activePartyMembers = new List<PersistentCharacterUnit>();
        GetActivePartyMembers(activePartyMembers);

        for (int i = 0; i < activePartyMembers.Count; i++)
        {
            PersistentCharacterUnit character = activePartyMembers[i];

            if (character == null || character.StatController == null)
            {
                continue;
            }

            character.StatController.AddExp(amount);
            Debug.Log($"[PersistentCharacterManager] {character.CharacterName} 경험치 +{amount} (현재 Exp: {character.StatController.Exp})");
        }

        SaveManager.RequestSave();
    }
}