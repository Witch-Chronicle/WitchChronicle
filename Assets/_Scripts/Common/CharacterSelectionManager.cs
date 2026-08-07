using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "지금 선택된 캐릭터가 누구인지"만 아는 매니저.
/// 장비/스탯/스킬 등 캐릭터별 화면들이 전부 이걸 통해 "누구 기준으로 보여줄지" 판단.
/// 캐릭터별 아이콘(장비 UI용/스탯 UI용)도 여기서 함께 관리한다.
/// * 장비 정보, 스탯 정보는 전혀 모름 - 순수하게 선택 상태와 UI용 아이콘 매핑만 관리.
/// </summary>
public class CharacterSelectionManager : MonoBehaviour
{
    /// <summary>
    /// 캐릭터별로 화면마다 다르게 쓰이는 아이콘 세트.
    /// CharacterBaseStats의 기본 Icon과는 별개로, UI 용도에 맞게 따로 지정한다.
    /// </summary>
    [Serializable]
    public class CharacterIconEntry
    {
        public CharacterType character;
        [Tooltip("장비(Equip) 화면에서 사용할 아이콘")]
        public Sprite characterEquipIcon;
        [Tooltip("스탯(Stat) 화면에서 사용할 아이콘")]
        public Sprite characterStatIcon;
    }

    public static CharacterSelectionManager Instance { get; private set; }

    [Header("시작 시 기본 선택 캐릭터")]
    [SerializeField] private CharacterType _defaultCharacter = CharacterType.ariel;

    [Header("캐릭터별 UI 아이콘 매핑")]
    [SerializeField] private List<CharacterIconEntry> _characterIcons = new List<CharacterIconEntry>();

    private Dictionary<CharacterType, CharacterIconEntry> _iconLookup;

    private CharacterType _selectedCharacter;

    /// <summary>
    /// 선택된 캐릭터가 바뀔 때마다 호출됨. 장비/스탯 UI가 구독해서 화면 전환하는 용도.
    /// </summary>
    public event Action<CharacterType> OnSelectionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _selectedCharacter = _defaultCharacter;

        BuildIconLookup();
    }

    private void BuildIconLookup()
    {
        _iconLookup = new Dictionary<CharacterType, CharacterIconEntry>();

        for (int i = 0; i < _characterIcons.Count; i++)
        {
            CharacterIconEntry entry = _characterIcons[i];

            if (entry == null)
            {
                continue;
            }

            if (_iconLookup.ContainsKey(entry.character))
            {
                Debug.LogWarning($"[CharacterSelectionManager] 중복된 CharacterType 아이콘 항목: {entry.character}", this);
                continue;
            }

            _iconLookup.Add(entry.character, entry);
        }
    }

    public CharacterType GetSelected()
    {
        return _selectedCharacter;
    }

    /// <summary>
    /// 캐릭터 탭 버튼 클릭 시 호출. 같은 캐릭터를 다시 선택하면 이벤트 발행 안 함.
    /// </summary>
    public void SetSelected(CharacterType character)
    {
        if (_selectedCharacter == character) return;

        _selectedCharacter = character;
        OnSelectionChanged?.Invoke(_selectedCharacter);
    }

    /// <summary>
    /// 선택 상태를 기본 캐릭터로 되돌림. UI 패널이 닫힐 때 호출해서,
    /// 다른 UI를 다시 열었을 때 이전에 선택했던 캐릭터가 아니라 항상 기본값부터 보이게 함.
    /// </summary>
    public void ResetToDefault()
    {
        if (_selectedCharacter == _defaultCharacter) return;

        _selectedCharacter = _defaultCharacter;
        OnSelectionChanged?.Invoke(_selectedCharacter);
    }

    /// <summary>
    /// 지정한 캐릭터의 장비 UI용 아이콘. 매핑이 없으면 null.
    /// </summary>
    public Sprite GetEquipIcon(CharacterType character)
    {
        return TryGetEntry(character, out CharacterIconEntry entry) ? entry.characterEquipIcon : null;
    }

    /// <summary>
    /// 지정한 캐릭터의 스탯 UI용 아이콘. 매핑이 없으면 null.
    /// </summary>
    public Sprite GetStatIcon(CharacterType character)
    {
        return TryGetEntry(character, out CharacterIconEntry entry) ? entry.characterStatIcon : null;
    }

    private bool TryGetEntry(CharacterType character, out CharacterIconEntry entry)
    {
        if (_iconLookup == null)
        {
            BuildIconLookup();
        }

        return _iconLookup.TryGetValue(character, out entry);
    }
}